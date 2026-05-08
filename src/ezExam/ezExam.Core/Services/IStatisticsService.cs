using ezExam.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Core.Services
{
    /// <summary>
    /// Interface thống kê thí sinh sau khi import
    /// </summary>
    public interface IStatisticsService
    {
        /// <summary>
        /// Thống kê toàn bộ thí sinh của kỳ thi:
        /// - Tổng số thí sinh
        /// - Số thí sinh mỗi môn
        /// - Số thí sinh theo cặp môn tự chọn
        /// </summary>
        Task<CandidateStatistics> GetStatisticsAsync(int sessionId);
    }
}
