using ezExam.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Core.Services
{
    /// <summary>
    /// Interface quản lý thí sinh
    /// </summary>
    public interface ICandidateService
    {
        /// <summary>Lấy danh sách thí sinh theo kỳ thi, sắp xếp theo SBD</summary>
        Task<List<Candidate>> GetBySessionAsync(int sessionId);

        /// <summary>Import danh sách thí sinh từ file Excel</summary>
        Task ImportFromExcelAsync(string filePath, int sessionId);

        /// <summary>
        /// Sắp xếp thí sinh theo tên (A-Z), sau đó đánh số báo danh liên tục.
        /// </summary>
        /// <param name="sessionId">Id kỳ thi</param>
        /// <param name="startNumber">Số SBD bắt đầu, mặc định là 1</param>
        Task SortAndAssignNumbersAsync(int sessionId, int startNumber = 1, string? numberFormat = null);

        /// <summary>Lấy thống kê thí sinh sau khi import</summary>
        Task<CandidateStatistics> GetStatisticsAsync(int sessionId);

        /// <summary>Xóa toàn bộ danh sách thí sinh của kỳ thi</summary>
        Task DeleteBySessionAsync(int sessionId);

        /// <summary>Xóa kết quả xếp ca (ca 1/ca 2) của toàn bộ thí sinh</summary>
        Task ClearShiftAssignmentsAsync(int sessionId);
    }
}
