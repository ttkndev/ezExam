using ezExam.Core.Models;
using ezExam.Core.Services;
using ezExam.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.App.Services
{
    /// <summary>
    /// Service quản lý thí sinh:
    /// - Lấy danh sách
    /// - Sắp xếp theo tên/họ đệm và đánh SBD
    /// </summary>
    public class CandidateService : ICandidateService
    {
        private readonly CandidateRepository _candidateRepo;
        private readonly ImportService _importService;
        private readonly StatisticsService _statisticsService;

        public CandidateService(
            CandidateRepository candidateRepo,
            ImportService importService,
            StatisticsService statisticsService)
        {
            _candidateRepo = candidateRepo;
            _importService = importService;
            _statisticsService = statisticsService;
        }

        public Task<List<Candidate>> GetBySessionAsync(int sessionId)
            => _candidateRepo.GetBySessionAsync(sessionId);

        public Task ImportFromExcelAsync(string filePath, int sessionId)
            => _importService.ImportFromExcelAsync(filePath, sessionId);

        public Task<CandidateStatistics> GetStatisticsAsync(int sessionId)
            => _statisticsService.GetStatisticsAsync(sessionId);

        /// <summary>
        /// Sắp xếp thí sinh theo tên (A-Z), nếu trùng tên thì xét họ đệm,
        /// sau đó đánh số báo danh liên tục bắt đầu từ số cho trước.
        /// </summary>
        public async Task SortAndAssignNumbersAsync(int sessionId, int startNumber = 1)
        {
            var candidates = await _candidateRepo.GetBySessionAsync(sessionId);

            // Sắp xếp: ưu tiên Tên → rồi mới đến Họ đệm (đúng quy tắc tiếng Việt)
            var sorted = candidates
                .OrderBy(c => RemoveDiacritics(c.FirstName))
                .ThenBy(c => RemoveDiacritics(c.LastName))
                .ToList();

            // Đánh SBD liên tục, format 6 chữ số: 000001, 000002, ...
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].CandidateNumber = (startNumber + i).ToString("D6");
                await _candidateRepo.UpdateAsync(sorted[i]);
            }

            await _candidateRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Loại bỏ dấu tiếng Việt để so sánh chuỗi chính xác hơn.
        /// Ví dụ: "Ngân" → "Ngan", "Ánh" → "Anh"
        /// </summary>
        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var chars = normalized
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                            != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray();
            return new string(chars).Normalize(System.Text.NormalizationForm.FormC);
        }
    }
}
