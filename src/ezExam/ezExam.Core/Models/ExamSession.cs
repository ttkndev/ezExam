using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Core.Models
{
    /// <summary>Đại diện cho một kỳ thi</summary>
    public class ExamSession
    {
        public int Id { get; set; }

        /// <summary>Tên kỳ thi, ví dụ: "Kỳ thi THPT 2024"</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Thời gian tổ chức kỳ thi</summary>
        public DateTime ExamDate { get; set; }

        /// <summary>Đánh dấu kỳ thi đang hoạt động (mặc định)</summary>
        public bool IsDefault { get; set; }

        // Navigation
        public List<Candidate> Candidates { get; set; } = new();
    }
}
