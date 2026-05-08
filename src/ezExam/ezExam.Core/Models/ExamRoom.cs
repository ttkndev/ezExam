using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Core.Models
{
    /// <summary>Loại phòng thi</summary>
    public enum RoomType
    {
        Literature,  // Ngữ văn
        Math,        // Toán
        Shift1,      // Ca 1
        Shift2       // Ca 2
    }

    /// <summary>Đại diện cho một phòng thi</summary>
    public class ExamRoom
    {
        public int Id { get; set; }

        /// <summary>Tên phòng, ví dụ: "Phòng 01", "Phòng 02"</summary>
        public string RoomName { get; set; } = string.Empty;

        /// <summary>Loại phòng (môn nào / ca nào)</summary>
        public RoomType Type { get; set; }

        /// <summary>Các môn thi có trong phòng này (Ca 1/Ca 2 có thể có nhiều môn)</summary>
        public string SubjectNames { get; set; } = string.Empty;

        /// <summary>Kỳ thi</summary>
        public int ExamSessionId { get; set; }

        // Navigation
        public List<RoomAssignment> Assignments { get; set; } = new();
    }

    /// <summary>Phân công thí sinh vào phòng thi</summary>
    public class RoomAssignment
    {
        public int Id { get; set; }
        public int ExamRoomId { get; set; }
        public ExamRoom? ExamRoom { get; set; }

        public int CandidateId { get; set; }
        public Candidate? Candidate { get; set; }

        /// <summary>Số thứ tự trong phòng</summary>
        public int OrderInRoom { get; set; }

        /// <summary>Môn thi của thí sinh trong phòng này</summary>
        public string SubjectName { get; set; } = string.Empty;
    }
}
