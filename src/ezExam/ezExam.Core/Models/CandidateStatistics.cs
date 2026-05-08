using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Core.Models
{
    /// <summary>
    /// Kết quả thống kê thí sinh sau khi import.
    /// Được trả về bởi IStatisticsService.GetStatisticsAsync()
    /// </summary>
    public class CandidateStatistics
    {
        /// <summary>Tổng số thí sinh của kỳ thi</summary>
        public int TotalCandidates { get; set; }

        /// <summary>
        /// Số thí sinh mỗi môn thi.
        /// Key = tên môn, Value = số lượng thí sinh.
        /// Đã sắp xếp theo số lượng giảm dần.
        /// Ví dụ: { "Ngữ văn": 181, "Toán": 181, "Lịch sử": 95, "KTPL": 86 }
        /// </summary>
        public Dictionary<string, int> CandidatesBySubject { get; set; } = new();

        /// <summary>
        /// Số thí sinh theo cặp môn tự chọn.
        /// Key = "MônA + MônB", Value = số lượng thí sinh.
        /// Đã sắp xếp theo số lượng giảm dần.
        /// Ví dụ: { "Lịch sử + KTPL": 72, "Địa lý + Vật lý": 45 }
        /// </summary>
        public Dictionary<string, int> CandidatesBySubjectPair { get; set; } = new();
    }
}
