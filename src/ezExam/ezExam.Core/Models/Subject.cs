using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Core.Models
{
    /// <summary>Loại môn thi</summary>
    public enum SubjectType
    {
        /// <summary>Môn bắt buộc (Văn, Toán)</summary>
        Required,
        /// <summary>Môn tự chọn (thi Ca 1 hoặc Ca 2)</summary>
        Optional
    }

    /// <summary>Đại diện cho một môn thi trong hệ thống</summary>
    public class Subject
    {
        public int Id { get; set; }

        /// <summary>Tên môn thi, ví dụ: "Ngữ văn", "Toán", "Lịch sử"</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Loại môn: bắt buộc hay tự chọn</summary>
        public SubjectType Type { get; set; }

        /// <summary>Ca thi được phân (chỉ áp dụng cho môn tự chọn)</summary>
        public int? AssignedShift { get; set; } // 1 hoặc 2, null nếu chưa xếp
    }
}
