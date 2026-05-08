using ezExam.Core.Models;
using ezExam.Core.Services;
using ezExam.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.App.Services
{
    /// <summary>
    /// Thuật toán xếp phòng thi cho cả 4 loại: Văn, Toán, Ca 1, Ca 2.
    ///
    /// Chiến lược:
    ///   - Văn / Toán: cắt tuần tự từ trên xuống, mỗi phòng 24 HS
    ///   - Ca 1 / Ca 2: Bin Packing (First Fit Decreasing)
    ///     + Môn >= 24 HS: lấp đầy phòng nguyên, phần dư gom vào pool
    ///     + Pool dư + môn nhỏ: ghép First Fit Decreasing cho đến khi hết
    /// </summary>
    public class RoomSchedulerService : IRoomSchedulerService
    {
        private readonly CandidateRepository _candidateRepo;
        private readonly ExamRoomRepository _roomRepo;

        private static readonly HashSet<string> RequiredSubjects =
            new(StringComparer.OrdinalIgnoreCase) { "Ngữ văn", "Toán" };

        public RoomSchedulerService(
            CandidateRepository candidateRepo,
            ExamRoomRepository roomRepo)
        {
            _candidateRepo = candidateRepo;
            _roomRepo = roomRepo;
        }

        /// <summary>
        /// Xếp phòng toàn bộ kỳ thi. Gọi sau khi đã xếp ca xong.
        /// Xóa phòng cũ và tạo lại từ đầu.
        /// </summary>
        public async Task<List<ExamRoom>> ScheduleRoomsAsync(
            int sessionId,
            Dictionary<string, int> subjectShiftMap, // Kết quả từ ShiftScheduler
            int capacity = 24)
        {
            // Xóa toàn bộ phòng cũ của kỳ thi
            await _roomRepo.DeleteBySessionAsync(sessionId);

            var candidates = await _candidateRepo.GetBySessionAsync(sessionId);
            var allRooms = new List<ExamRoom>();
            int roomNumber = 1; // Số phòng thi tăng dần xuyên suốt

            // --- 1. Xếp phòng Ngữ văn ---
            var vanCandidates = candidates
                .Where(c => c.GetSubjectList()
                    .Any(s => s.Equals("Ngữ văn", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(c => c.CandidateNumber)
                .ToList();

            allRooms.AddRange(AssignSequential(
                vanCandidates, "Ngữ văn",
                RoomType.Literature, sessionId,
                capacity, ref roomNumber));

            // --- 2. Xếp phòng Toán ---
            var toanCandidates = candidates
                .Where(c => c.GetSubjectList()
                    .Any(s => s.Equals("Toán", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(c => c.CandidateNumber)
                .ToList();

            allRooms.AddRange(AssignSequential(
                toanCandidates, "Toán",
                RoomType.Math, sessionId,
                capacity, ref roomNumber));

            // --- 3. Xếp phòng Ca 1 ---
            var shift1Subjects = subjectShiftMap
                .Where(kv => kv.Value == 1)
                .Select(kv => kv.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var shift1Groups = BuildSubjectGroups(candidates, shift1Subjects);
            allRooms.AddRange(AssignBinPacking(
                shift1Groups, RoomType.Shift1,
                sessionId, capacity, ref roomNumber));

            // --- 4. Xếp phòng Ca 2 ---
            var shift2Subjects = subjectShiftMap
                .Where(kv => kv.Value == 2)
                .Select(kv => kv.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var shift2Groups = BuildSubjectGroups(candidates, shift2Subjects);
            allRooms.AddRange(AssignBinPacking(
                shift2Groups, RoomType.Shift2,
                sessionId, capacity, ref roomNumber));

            // Lưu tất cả phòng vào database
            await _roomRepo.AddRangeAsync(allRooms);
            await _roomRepo.SaveChangesAsync();

            return allRooms;
        }

        // =========================================================================
        // XẾP PHÒNG TUẦN TỰ (Văn / Toán)
        // =========================================================================

        /// <summary>
        /// Xếp phòng tuần tự: cắt danh sách từ trên xuống, mỗi phòng đủ capacity.
        /// Dùng cho môn Văn và Toán (bắt buộc).
        /// </summary>
        private static List<ExamRoom> AssignSequential(
            List<Candidate> candidates,
            string subjectName,
            RoomType roomType,
            int sessionId,
            int capacity,
            ref int roomNumber)
        {
            var rooms = new List<ExamRoom>();
            int idx = 0;

            while (idx < candidates.Count)
            {
                // Cắt một nhóm tối đa capacity người
                var batch = candidates.Skip(idx).Take(capacity).ToList();
                idx += capacity;

                var room = new ExamRoom
                {
                    RoomName = $"Phòng {roomNumber:D2}",
                    Type = roomType,
                    SubjectNames = subjectName,
                    ExamSessionId = sessionId,
                    Assignments = batch.Select((c, i) => new RoomAssignment
                    {
                        CandidateId = c.Id,
                        OrderInRoom = i + 1,
                        SubjectName = subjectName
                    }).ToList()
                };

                rooms.Add(room);
                roomNumber++;
            }

            return rooms;
        }

        // =========================================================================
        // XẾP PHÒNG BIN PACKING (Ca 1 / Ca 2)
        // =========================================================================

        /// <summary>
        /// Nhóm thí sinh theo môn tự chọn trong ca.
        /// Trả về: key = tên môn, value = danh sách thí sinh môn đó
        /// </summary>
        private static Dictionary<string, List<Candidate>> BuildSubjectGroups(
            List<Candidate> candidates,
            HashSet<string> shiftSubjects)
        {
            var groups = new Dictionary<string, List<Candidate>>(StringComparer.OrdinalIgnoreCase);

            foreach (var subject in shiftSubjects)
                groups[subject] = new List<Candidate>();

            foreach (var c in candidates)
            {
                var optionals = c.GetSubjectList()
                    .Where(s => shiftSubjects.Contains(s))
                    .ToList();

                // Mỗi HS chỉ có 1 môn trong ca (đã đảm bảo bởi ShiftScheduler)
                foreach (var subject in optionals)
                    groups[subject].Add(c);
            }

            // Sắp xếp HS trong mỗi môn theo SBD
            foreach (var key in groups.Keys.ToList())
                groups[key] = groups[key].OrderBy(c => c.CandidateNumber).ToList();

            return groups;
        }

        /// <summary>
        /// Bin Packing — First Fit Decreasing:
        ///   1. Sắp xếp môn theo số HS giảm dần
        ///   2. Môn >= capacity: lấp đầy phòng nguyên, phần dư vào pool
        ///   3. Môn < capacity: thẳng vào pool
        ///   4. Ghép pool theo First Fit Decreasing cho đến khi hết
        /// </summary>
        private static List<ExamRoom> AssignBinPacking(
            Dictionary<string, List<Candidate>> subjectGroups,
            RoomType roomType,
            int sessionId,
            int capacity,
            ref int roomNumber)
        {
            var rooms = new List<ExamRoom>();

            // Sắp xếp môn theo số HS giảm dần
            var sortedSubjects = subjectGroups
                .OrderByDescending(kv => kv.Value.Count)
                .ToList();

            // Pool dư: danh sách (tên môn, danh sách HS còn dư)
            var remainderPool = new List<(string subject, List<Candidate> candidates)>();

            // Bước 1: Xử lý môn >= capacity → lấp đầy phòng nguyên
            foreach (var (subject, candidateList) in sortedSubjects)
            {
                int idx = 0;

                // Lấy từng nhóm 24 người → tạo phòng nguyên
                while (idx + capacity <= candidateList.Count)
                {
                    var batch = candidateList.Skip(idx).Take(capacity).ToList();
                    idx += capacity;

                    rooms.Add(CreateRoom(batch, new[] { subject },
                        roomType, sessionId, capacity, ref roomNumber));
                }

                // Phần dư (< capacity) → vào pool
                if (idx < candidateList.Count)
                {
                    var remainder = candidateList.Skip(idx).ToList();
                    remainderPool.Add((subject, remainder));
                }
            }

            // Bước 2: Ghép pool dư theo First Fit Decreasing
            // Sắp xếp pool theo số HS giảm dần
            remainderPool = remainderPool
                .OrderByDescending(x => x.candidates.Count)
                .ToList();

            // Danh sách "thùng" đang mở (chưa đầy)
            // Mỗi thùng: (danh sách môn, danh sách HS, tổng HS hiện tại)
            var bins = new List<(List<string> subjects, List<(string subj, Candidate cand)> items, int count)>();

            foreach (var (subject, candidateList) in remainderPool)
            {
                // Thử nhét vào thùng đang mở (First Fit)
                bool placed = false;

                for (int i = 0; i < bins.Count; i++)
                {
                    var (binSubjects, binItems, binCount) = bins[i];

                    if (binCount + candidateList.Count <= capacity)
                    {
                        // Vừa → nhét vào thùng này
                        binSubjects.Add(subject);
                        binItems.AddRange(candidateList.Select(c => (subject, c)));
                        bins[i] = (binSubjects, binItems, binCount + candidateList.Count);
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    // Không vừa thùng nào → mở thùng mới
                    bins.Add((
                        new List<string> { subject },
                        candidateList.Select(c => (subject, c)).ToList(),
                        candidateList.Count
                    ));
                }
            }

            // Bước 3: Chuyển các thùng thành phòng thi
            foreach (var (binSubjects, binItems, _) in bins)
            {
                if (binItems.Count == 0) continue;

                var assignments = binItems.Select((item, i) => new RoomAssignment
                {
                    CandidateId = item.cand.Id,
                    OrderInRoom = i + 1,
                    SubjectName = item.subj
                }).ToList();

                var room = new ExamRoom
                {
                    RoomName = $"Phòng {roomNumber:D2}",
                    Type = roomType,
                    SubjectNames = string.Join(", ", binSubjects.Distinct()),
                    ExamSessionId = sessionId,
                    Assignments = assignments
                };

                rooms.Add(room);
                roomNumber++;
            }

            return rooms;
        }

        /// <summary>Helper tạo phòng từ 1 nhóm thí sinh cùng môn</summary>
        private static ExamRoom CreateRoom(
            List<Candidate> batch,
            IEnumerable<string> subjects,
            RoomType roomType,
            int sessionId,
            int capacity,
            ref int roomNumber)
        {
            var subjectList = subjects.ToList();
            var room = new ExamRoom
            {
                RoomName = $"Phòng {roomNumber:D2}",
                Type = roomType,
                SubjectNames = string.Join(", ", subjectList),
                ExamSessionId = sessionId,
                Assignments = batch.Select((c, i) => new RoomAssignment
                {
                    CandidateId = c.Id,
                    OrderInRoom = i + 1,
                    SubjectName = subjectList.First()
                }).ToList()
            };
            roomNumber++;
            return room;
        }
    }
}
