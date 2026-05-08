using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Core.Models
{
    /// <summary>Đại diện cho một thí sinh</summary>
    public class Candidate
    {
        public int Id { get; set; }

        /// <summary>Số báo danh (được đánh sau khi sắp xếp)</summary>
        public string CandidateNumber { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;   // Họ đệm (để sắp xếp)
        public string FirstName { get; set; } = string.Empty;  // Tên (để sắp xếp)
        public DateTime DateOfBirth { get; set; }
        public string ClassName { get; set; } = string.Empty;

        /// <summary>Danh sách tên môn thi (lưu dạng chuỗi phân cách bởi dấu phẩy)</summary>
        public string SubjectNames { get; set; } = string.Empty;

        /// <summary>Môn được xếp vào ca 1</summary>
        public string Shift1SubjectName { get; set; } = string.Empty;

        /// <summary>Môn được xếp vào ca 2</summary>
        public string Shift2SubjectName { get; set; } = string.Empty;

        /// <summary>Kỳ thi mà thí sinh này thuộc về</summary>
        public int ExamSessionId { get; set; }
        public ExamSession? ExamSession { get; set; }

        /// <summary>Lấy danh sách môn thi dạng List</summary>
        public List<string> GetSubjectList() =>
            SubjectNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();
    }
}
