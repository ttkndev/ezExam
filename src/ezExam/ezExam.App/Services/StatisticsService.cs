using ezExam.Core.Models;
using ezExam.Core.Services;
using ezExam.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.App.Services
{
    /// <summary>
    /// Service thống kê thí sinh sau khi import:
    /// - Tổng số thí sinh
    /// - Số thí sinh mỗi môn
    /// - Số thí sinh theo cặp môn tự chọn
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private readonly CandidateRepository _repo;

        // Tên môn bắt buộc để lọc ra môn tự chọn
        private static readonly HashSet<string> RequiredSubjects = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ngữ văn", "Toán"
    };

        public StatisticsService(CandidateRepository repo)
        {
            _repo = repo;
        }

        public async Task<CandidateStatistics> GetStatisticsAsync(int sessionId)
        {
            var candidates = await _repo.GetBySessionAsync(sessionId);

            var stats = new CandidateStatistics
            {
                TotalCandidates = candidates.Count
            };

            // Thống kê số thí sinh mỗi môn
            foreach (var c in candidates)
            {
                foreach (var subject in c.GetSubjectList())
                {
                    if (!stats.CandidatesBySubject.ContainsKey(subject))
                        stats.CandidatesBySubject[subject] = 0;
                    stats.CandidatesBySubject[subject]++;
                }
            }

            // Thống kê số thí sinh theo cặp môn tự chọn
            foreach (var c in candidates)
            {
                // Lọc ra chỉ các môn tự chọn
                var optionals = c.GetSubjectList()
                    .Where(s => !RequiredSubjects.Contains(s))
                    .OrderBy(s => s)
                    .ToList();

                if (optionals.Count >= 2)
                {
                    // Ghép thành cặp: "Lịch sử + KTPL"
                    var pair = string.Join(" + ", optionals.Take(2));
                    if (!stats.CandidatesBySubjectPair.ContainsKey(pair))
                        stats.CandidatesBySubjectPair[pair] = 0;
                    stats.CandidatesBySubjectPair[pair]++;
                }
                else if (optionals.Count == 1)
                {
                    // Thí sinh chỉ có 1 môn tự chọn (được miễn giảm)
                    var key = optionals[0] + " (1 môn)";
                    if (!stats.CandidatesBySubjectPair.ContainsKey(key))
                        stats.CandidatesBySubjectPair[key] = 0;
                    stats.CandidatesBySubjectPair[key]++;
                }
            }

            // Sắp xếp theo số lượng giảm dần
            stats.CandidatesBySubject = stats.CandidatesBySubject
                .OrderByDescending(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);

            stats.CandidatesBySubjectPair = stats.CandidatesBySubjectPair
                .OrderByDescending(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);

            return stats;
        }
    }
}
