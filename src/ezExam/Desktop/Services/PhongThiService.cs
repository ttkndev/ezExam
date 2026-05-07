using Desktop.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Desktop.Services
{
<<<<<<< HEAD:src/ezExam/Desktop/Services/PhongThiService.cs
    public enum RoomAllocationStrategy
    {
        MaxMerge,
        SequentialSbd
    }

    public enum ExamSession
    {
        MandatoryVan,
        MandatoryToan,
        OptionalCa1,
        OptionalCa2
    }

    public class SubjectCount
    {
        public string Subject { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RoomEntry
    {
        public string Subject { get; set; } = string.Empty;
        public int Count { get; set; }
        public int StartSbd { get; set; }
        public int EndSbd { get; set; }
    }

    public class RoomPlan
    {
        public int RoomNumber { get; set; }
        public List<RoomEntry> Entries { get; set; } = new();
        public int Total => Entries.Sum(e => e.Count);
    }

    public class PhongThiService
=======
    public class PhongThiAllocator
>>>>>>> 2f0b510af1b33b945425ec90da8d69fb7c777048:src/ezExam/Desktop/Services/PhongThiAllocator.cs
    {
        /// <summary>
        /// Hàm tổng quát dùng để xếp phòng cho một danh sách môn đã có số lượng đăng ký.
        /// Có thể dùng cho ca 1, ca 2, hoặc các môn bắt buộc nếu truyền đúng dữ liệu đầu vào.
        /// </summary>
        public List<RoomPlan> Allocate(List<SubjectCount> subjects, int roomCapacity, RoomAllocationStrategy strategy)
        {
            if (subjects == null || subjects.Count == 0) return new List<RoomPlan>();
            if (roomCapacity <= 0) throw new ArgumentException("roomCapacity must be > 0");

            return strategy == RoomAllocationStrategy.MaxMerge
                ? AllocateMaxMerge(subjects, roomCapacity)
                : AllocateSequentialSbd(subjects, roomCapacity);
        }

        /// <summary>
        /// Xếp phòng cho Văn/Toán bắt buộc theo đúng quy tắc "từ trên xuống dưới, mỗi phòng roomCapacity thí sinh".
        /// Mỗi phần tử trả về tương ứng một phòng chỉ chứa đúng 1 môn bắt buộc.
        /// </summary>
        public List<RoomPlan> AllocateMandatorySubject(string subjectName, int totalCandidates, int roomCapacity)
        {
            if (totalCandidates <= 0) return new List<RoomPlan>();

            var rooms = new List<RoomPlan>();
            var roomNo = 1;
            var nextSbd = 1;
            var remaining = totalCandidates;

            while (remaining > 0)
            {
                var take = Math.Min(remaining, roomCapacity);
                rooms.Add(new RoomPlan
                {
                    RoomNumber = roomNo++,
                    Entries = new List<RoomEntry>
                    {
                        new RoomEntry
                        {
                            Subject = subjectName,
                            Count = take,
                            StartSbd = nextSbd,
                            EndSbd = nextSbd + take - 1
                        }
                    }
                });

                nextSbd += take;
                remaining -= take;
            }

            return rooms;
        }

        /// <summary>
        /// Tối ưu chia 2 môn tự chọn của mỗi thí sinh vào Ca 1/Ca 2.
        /// Mục tiêu: giảm số phòng ước tính cho từng ca và tránh lệch tải quá nhiều giữa 2 ca của cùng một môn.
        /// - Nếu thí sinh có 0 môn tự chọn: bỏ qua.
        /// - Nếu có 1 môn: môn đó được gán vào ca đang ít tải hơn.
        /// - Nếu có 2 môn: thử cả 2 cách gán (A->Ca1,B->Ca2) và (B->Ca1,A->Ca2), chọn phương án có "điểm" tốt hơn.
        /// </summary>
        public void OptimizeOptionalSessions(List<ThiSinh> candidates, int roomCapacity)
        {
            var ca1Count = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var ca2Count = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var ts in candidates)
            {
                var optionalSubjects = ExtractOptionalSubjects(ts)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (optionalSubjects.Count == 0)
                {
                    ts.MonCa1 = string.Empty;
                    ts.MonCa2 = string.Empty;
                    continue;
                }

                if (optionalSubjects.Count == 1)
                {
                    var subject = optionalSubjects[0];
                    if (GetCount(ca1Count, subject) <= GetCount(ca2Count, subject))
                    {
                        Assign(ts, subject, string.Empty, ca1Count, ca2Count);
                    }
                    else
                    {
                        Assign(ts, string.Empty, subject, ca1Count, ca2Count);
                    }
                    continue;
                }

                var a = optionalSubjects[0];
                var b = optionalSubjects[1];

                var scoreAB = ScoreAssignment(ca1Count, ca2Count, a, b, roomCapacity);
                var scoreBA = ScoreAssignment(ca1Count, ca2Count, b, a, roomCapacity);

                if (scoreAB <= scoreBA)
                {
                    Assign(ts, a, b, ca1Count, ca2Count);
                }
                else
                {
                    Assign(ts, b, a, ca1Count, ca2Count);
                }
            }
        }

        private static IEnumerable<string> ExtractOptionalSubjects(ThiSinh ts)
        {
            return (ts.CacMonThi ?? string.Empty)
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s) &&
                            !s.Equals("Văn", StringComparison.OrdinalIgnoreCase) &&
                            !s.Equals("Toán", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(2);
        }

        private static double ScoreAssignment(Dictionary<string, int> ca1Count, Dictionary<string, int> ca2Count,
            string ca1Subject, string ca2Subject, int roomCapacity)
        {
            var p1 = RoomsNeeded(GetCount(ca1Count, ca1Subject) + (!string.IsNullOrEmpty(ca1Subject) ? 1 : 0), roomCapacity);
            var p2 = RoomsNeeded(GetCount(ca2Count, ca2Subject) + (!string.IsNullOrEmpty(ca2Subject) ? 1 : 0), roomCapacity);

            var balancePenalty = Math.Abs(GetCount(ca1Count, ca1Subject) - GetCount(ca2Count, ca1Subject))
                               + Math.Abs(GetCount(ca1Count, ca2Subject) - GetCount(ca2Count, ca2Subject));

            return p1 + p2 + balancePenalty * 0.01;
        }

        private static int RoomsNeeded(int count, int roomCapacity) => (count + roomCapacity - 1) / roomCapacity;

        private static void Assign(ThiSinh ts, string ca1Subject, string ca2Subject,
            Dictionary<string, int> ca1Count, Dictionary<string, int> ca2Count)
        {
            ts.MonCa1 = ca1Subject;
            ts.MonCa2 = ca2Subject;

            if (!string.IsNullOrEmpty(ca1Subject)) ca1Count[ca1Subject] = GetCount(ca1Count, ca1Subject) + 1;
            if (!string.IsNullOrEmpty(ca2Subject)) ca2Count[ca2Subject] = GetCount(ca2Count, ca2Subject) + 1;
        }

        private static int GetCount(Dictionary<string, int> dict, string subject)
            => (!string.IsNullOrWhiteSpace(subject) && dict.TryGetValue(subject, out var value)) ? value : 0;

        private static List<RoomPlan> AllocateSequentialSbd(List<SubjectCount> subjects, int roomCapacity)
        {
            var queue = subjects
                .Where(s => s.Count > 0)
                .OrderByDescending(s => s.Count)
                .Select(s => new SubjectCount { Subject = s.Subject, Count = s.Count })
                .ToList();

            var rooms = new List<RoomPlan>();
            var nextSbd = 1;
            var roomNo = 1;

            while (queue.Any(s => s.Count > 0))
            {
                var room = new RoomPlan { RoomNumber = roomNo++ };
                var remaining = roomCapacity;

                foreach (var subject in queue)
                {
                    if (subject.Count <= 0 || remaining <= 0) continue;

                    var take = Math.Min(subject.Count, remaining);
                    room.Entries.Add(new RoomEntry
                    {
                        Subject = subject.Subject,
                        Count = take,
                        StartSbd = nextSbd,
                        EndSbd = nextSbd + take - 1
                    });

                    nextSbd += take;
                    subject.Count -= take;
                    remaining -= take;
                }

                rooms.Add(room);
            }

            return rooms;
        }

        private static List<RoomPlan> AllocateMaxMerge(List<SubjectCount> subjects, int roomCapacity)
        {
            var ordered = subjects
                .Where(s => s.Count > 0)
                .OrderByDescending(s => s.Count)
                .Select(s => new SubjectCount { Subject = s.Subject, Count = s.Count })
                .ToList();

            var rooms = new List<RoomPlan>();
            var nextSbd = 1;
            var roomNo = 1;

            while (ordered.Any(s => s.Count >= roomCapacity))
            {
                var subject = ordered.First(s => s.Count >= roomCapacity);
                var room = new RoomPlan { RoomNumber = roomNo++ };
                room.Entries.Add(new RoomEntry
                {
                    Subject = subject.Subject,
                    Count = roomCapacity,
                    StartSbd = nextSbd,
                    EndSbd = nextSbd + roomCapacity - 1
                });
                nextSbd += roomCapacity;
                subject.Count -= roomCapacity;
                rooms.Add(room);
            }

            while (ordered.Any(s => s.Count > 0))
            {
                var room = new RoomPlan { RoomNumber = roomNo++ };
                var remaining = roomCapacity;

                foreach (var subject in ordered.Where(s => s.Count > 0).OrderBy(s => s.Count))
                {
                    if (remaining == 0) break;
                    var take = Math.Min(subject.Count, remaining);
                    room.Entries.Add(new RoomEntry
                    {
                        Subject = subject.Subject,
                        Count = take,
                        StartSbd = nextSbd,
                        EndSbd = nextSbd + take - 1
                    });
                    nextSbd += take;
                    subject.Count -= take;
                    remaining -= take;
                }

                rooms.Add(room);
            }

            return rooms;
        }
    }
}
