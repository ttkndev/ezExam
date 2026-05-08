using ezExam.Core.Models;
using ezExam.Core.Services;
using ezExam.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.App.Services
{
    /// <summary>
    /// Thuật toán xếp ca thi cho các môn tự chọn.
    ///
    /// Quy tắc nghiệp vụ:
    ///   - Mỗi HS có 2 môn tự chọn → 1 môn Ca1, 1 môn Ca2
    ///   - Một môn CÓ THỂ xuất hiện ở cả Ca1 lẫn Ca2 (nhiều đề)
    ///   - HS chỉ có 1 môn → xếp vào Ca1
    ///
    /// Mục tiêu tối ưu:
    ///   - Tối thiểu số môn phân biệt ở mỗi ca
    ///   - Phòng thi càng ít môn càng tốt
    ///
    /// Thuật toán: Greedy theo độ nặng cặp + Local Search
    /// </summary>
    public class ShiftSchedulerService : IShiftSchedulerService
    {
        private readonly CandidateRepository _repo;

        private static readonly HashSet<string> RequiredSubjects =
            new(StringComparer.OrdinalIgnoreCase) { "Ngữ văn", "Toán" };

        public ShiftSchedulerService(CandidateRepository repo)
        {
            _repo = repo;
        }

        public async Task<ShiftScheduleResult> ScheduleAsync(int sessionId)
        {
            var candidates = await _repo.GetBySessionAsync(sessionId);

            // Lấy danh sách môn tự chọn
            var allSubjects = GetAllOptionalSubjects(candidates);
            if (!allSubjects.Any())
                return new ShiftScheduleResult
                {
                    IsPerfect = true,
                    Message = "Không có môn tự chọn nào."
                };

            // Lấy danh sách cặp môn và số HS mỗi cặp
            var pairs = BuildSubjectPairs(candidates);

            // HS chỉ có 1 môn tự chọn → gán thẳng Ca 1
            var singleSubjects = GetSingleOptionalSubjects(candidates);

            // Chạy thuật toán xếp ca
            var assignment = GreedyWithLocalSearch(allSubjects, pairs, singleSubjects);

            // Tính kết quả
            var ca1Subjects = assignment
                .Where(kv => kv.Value.Contains(1))
                .Select(kv => kv.Key).ToList();
            var ca2Subjects = assignment
                .Where(kv => kv.Value.Contains(2))
                .Select(kv => kv.Key).ToList();

            // Xây dựng SubjectShiftMap cho từng HS
            // (mỗi môn có thể ở cả 2 ca nên map theo HS)
            var shiftMap = BuildShiftMapPerCandidate(candidates, assignment);

            // Ghi trực tiếp kết quả môn ca 1/ca 2 vào từng thí sinh
            ApplyShiftSubjectsToCandidates(candidates, shiftMap);
            foreach (var candidate in candidates)
                await _repo.UpdateAsync(candidate);
            await _repo.SaveChangesAsync();

            return new ShiftScheduleResult
            {
                SubjectShiftMap = shiftMap,
                SubjectCaMap = assignment,
                Ca1Subjects = ca1Subjects,
                Ca2Subjects = ca2Subjects,
                ConflictCount = 0, // Không còn xung đột vì môn có thể lặp
                IsPerfect = true,
                Message = $"Ca 1: {string.Join(", ", ca1Subjects)} | " +
                          $"Ca 2: {string.Join(", ", ca2Subjects)}"
            };
        }

        // =========================================================================
        // GREEDY + LOCAL SEARCH
        // =========================================================================

        /// <summary>
        /// Greedy + Local Search:
        ///   1. Sắp xếp cặp môn theo số HS giảm dần
        ///   2. Với mỗi cặp, chọn hướng gán làm tổng số môn 2 ca nhỏ nhất
        ///   3. Local Search: thử đổi chiều từng cặp để cải thiện
        /// </summary>
        private static Dictionary<string, HashSet<int>> GreedyWithLocalSearch(
            HashSet<string> allSubjects,
            List<SubjectPair> pairs,
            HashSet<string> singleSubjects)
        {
            // assignment[môn] = {1} hoặc {2} hoặc {1,2} (môn xuất hiện ở cả 2 ca)
            var assignment = new Dictionary<string, HashSet<int>>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var s in allSubjects)
                assignment[s] = new HashSet<int>();

            // HS chỉ có 1 môn → Ca 1
            foreach (var s in singleSubjects)
                assignment[s].Add(1);

            // Sắp xếp cặp theo số HS giảm dần
            var sortedPairs = pairs.OrderByDescending(p => p.Count).ToList();

            // --- GREEDY ---
            foreach (var pair in sortedPairs)
            {
                var a = pair.SubjectA;
                var b = pair.SubjectB;

                // Thử hướng 1: A→Ca1, B→Ca2
                int score1 = ScoreAssignment(assignment, a, 1, b, 2);
                // Thử hướng 2: A→Ca2, B→Ca1
                int score2 = ScoreAssignment(assignment, a, 2, b, 1);

                // Chọn hướng có score nhỏ hơn (ít môn mới thêm vào)
                if (score1 <= score2)
                {
                    assignment[a].Add(1);
                    assignment[b].Add(2);
                }
                else
                {
                    assignment[a].Add(2);
                    assignment[b].Add(1);
                }
            }

            // --- LOCAL SEARCH ---
            // Thử đổi chiều từng cặp, nếu cải thiện thì giữ
            bool improved = true;
            int maxIterations = 100; // Giới hạn vòng lặp

            while (improved && maxIterations-- > 0)
            {
                improved = false;
                int currentScore = TotalScore(assignment);

                foreach (var pair in sortedPairs)
                {
                    var a = pair.SubjectA;
                    var b = pair.SubjectB;

                    // Thử đảo chiều cặp này
                    var backup = CloneAssignment(assignment);
                    FlipPair(assignment, a, b);

                    int newScore = TotalScore(assignment);
                    if (newScore < currentScore)
                    {
                        // Cải thiện → giữ lại
                        currentScore = newScore;
                        improved = true;
                    }
                    else
                    {
                        // Không cải thiện → khôi phục
                        RestoreAssignment(assignment, backup, a, b);
                    }
                }
            }

            return assignment;
        }

        /// <summary>
        /// Tính score khi thêm môn A vào caA và môn B vào caB.
        /// Score = số môn mới được thêm vào (chưa có trong ca đó).
        /// Score nhỏ hơn = tốt hơn (ít môn mới = phòng ít hỗn hợp hơn).
        /// </summary>
        private static int ScoreAssignment(
            Dictionary<string, HashSet<int>> assignment,
            string a, int caA,
            string b, int caB)
        {
            int score = 0;
            if (!assignment[a].Contains(caA)) score++;
            if (!assignment[b].Contains(caB)) score++;
            return score;
        }

        /// <summary>
        /// Tính tổng số (môn, ca) → số nhỏ hơn = ít phòng hỗn hợp hơn.
        /// Ví dụ: Ca1={Lý,Hóa,Sinh}, Ca2={Lý,Sinh} → score = 3+2 = 5
        /// </summary>
        private static int TotalScore(Dictionary<string, HashSet<int>> assignment)
        {
            int ca1 = assignment.Count(kv => kv.Value.Contains(1));
            int ca2 = assignment.Count(kv => kv.Value.Contains(2));
            return ca1 + ca2;
        }

        /// <summary>Đảo chiều một cặp môn (Ca1↔Ca2)</summary>
        private static void FlipPair(
            Dictionary<string, HashSet<int>> assignment,
            string a, string b)
        {
            // Đảo Ca1↔Ca2 cho cặp (a,b)
            foreach (var s in new[] { a, b })
            {
                var old = new HashSet<int>(assignment[s]);
                assignment[s].Clear();
                if (old.Contains(1)) assignment[s].Add(2);
                if (old.Contains(2)) assignment[s].Add(1);
            }
        }

        private static Dictionary<string, HashSet<int>> CloneAssignment(
            Dictionary<string, HashSet<int>> assignment)
            => assignment.ToDictionary(
                kv => kv.Key,
                kv => new HashSet<int>(kv.Value),
                StringComparer.OrdinalIgnoreCase);

        private static void RestoreAssignment(
            Dictionary<string, HashSet<int>> assignment,
            Dictionary<string, HashSet<int>> backup,
            string a, string b)
        {
            assignment[a] = new HashSet<int>(backup[a]);
            assignment[b] = new HashSet<int>(backup[b]);
        }

        // =========================================================================
        // BUILD SHIFT MAP PER CANDIDATE
        // =========================================================================

        /// <summary>
        /// Xây dựng map xếp ca cho từng thí sinh cụ thể.
        /// Key = CandidateId_SubjectName, Value = ca (1 hoặc 2)
        /// Dùng để xếp phòng sau này.
        /// </summary>
        private static Dictionary<string, int> BuildShiftMapPerCandidate(
            List<Candidate> candidates,
            Dictionary<string, HashSet<int>> subjectCaMap)
        {
            var map = new Dictionary<string, int>();

            foreach (var c in candidates)
            {
                var optionals = c.GetSubjectList()
                    .Where(s => !RequiredSubjects.Contains(s))
                    .ToList();

                if (optionals.Count == 0) continue;

                if (optionals.Count == 1)
                {
                    // Chỉ 1 môn → Ca 1
                    map[$"{c.Id}_{optionals[0]}"] = 1;
                    continue;
                }

                // 2 môn: gán theo subjectCaMap
                var subA = optionals[0];
                var subB = optionals[1];

                var casA = subjectCaMap.GetValueOrDefault(subA, new HashSet<int> { 1 });
                var casB = subjectCaMap.GetValueOrDefault(subB, new HashSet<int> { 2 });

                // Chọn ca cho A và B sao cho khác nhau
                int caA = casA.Contains(1) ? 1 : 2;
                int caB = caA == 1 ? 2 : 1;

                // Nếu B không thi ở caB → đổi lại
                if (!casB.Contains(caB))
                {
                    caA = caA == 1 ? 2 : 1;
                    caB = caA == 1 ? 2 : 1;
                }

                map[$"{c.Id}_{subA}"] = caA;
                map[$"{c.Id}_{subB}"] = caB;
            }

            return map;
        }


        /// <summary>Gán môn ca 1/ca 2 trực tiếp vào thí sinh để hiển thị nhanh trên UI</summary>
        private static void ApplyShiftSubjectsToCandidates(
            List<Candidate> candidates,
            Dictionary<string, int> shiftMap)
        {
            foreach (var candidate in candidates)
            {
                candidate.Shift1SubjectName = string.Empty;
                candidate.Shift2SubjectName = string.Empty;

                var optionalSubjects = candidate.GetSubjectList()
                    .Where(s => !RequiredSubjects.Contains(s))
                    .ToList();

                foreach (var subject in optionalSubjects)
                {
                    var key = $"{candidate.Id}_{subject}";
                    if (!shiftMap.TryGetValue(key, out var shift)) continue;

                    if (shift == 1) candidate.Shift1SubjectName = subject;
                    else if (shift == 2) candidate.Shift2SubjectName = subject;
                }
            }
        }

        // =========================================================================
        // HELPERS
        // =========================================================================

        /// <summary>Lấy tất cả môn tự chọn phân biệt</summary>
        private static HashSet<string> GetAllOptionalSubjects(List<Candidate> candidates)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in candidates)
                foreach (var s in c.GetSubjectList().Where(s => !RequiredSubjects.Contains(s)))
                    set.Add(s);
            return set;
        }

        /// <summary>Lấy môn tự chọn của HS chỉ có 1 môn (miễn giảm)</summary>
        private static HashSet<string> GetSingleOptionalSubjects(List<Candidate> candidates)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in candidates)
            {
                var opts = c.GetSubjectList()
                    .Where(s => !RequiredSubjects.Contains(s)).ToList();
                if (opts.Count == 1) set.Add(opts[0]);
            }
            return set;
        }

        /// <summary>
        /// Xây dựng danh sách cặp môn tự chọn và số HS mỗi cặp.
        /// Ví dụ: (Lý, Hóa) → 40 HS
        /// </summary>
        private static List<SubjectPair> BuildSubjectPairs(List<Candidate> candidates)
        {
            var dict = new Dictionary<string, SubjectPair>();

            foreach (var c in candidates)
            {
                var opts = c.GetSubjectList()
                    .Where(s => !RequiredSubjects.Contains(s))
                    .OrderBy(s => s)
                    .ToList();

                if (opts.Count < 2) continue;

                var key = $"{opts[0]}|{opts[1]}";
                if (!dict.ContainsKey(key))
                    dict[key] = new SubjectPair
                    {
                        SubjectA = opts[0],
                        SubjectB = opts[1],
                        Count = 0
                    };
                dict[key].Count++;
            }

            return dict.Values.ToList();
        }

        /// <summary>Cặp môn tự chọn và số HS đăng ký</summary>
        private class SubjectPair
        {
            public string SubjectA { get; set; } = "";
            public string SubjectB { get; set; } = "";
            public int Count { get; set; }
        }
    }
}
