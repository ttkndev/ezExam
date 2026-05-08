using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Core.Models
{
    public class ShiftScheduleResult
    {
        /// <summary>
        /// Map xếp ca theo từng HS + môn.
        /// Key = "CandidateId_TênMôn", Value = 1 hoặc 2.
        /// Dùng để xếp phòng.
        /// </summary>
        public Dictionary<string, int> SubjectShiftMap { get; set; } = new();

        /// <summary>
        /// Map ca theo môn (môn có thể ở cả 2 ca).
        /// Key = tên môn, Value = {1} hoặc {2} hoặc {1,2}.
        /// </summary>
        public Dictionary<string, HashSet<int>> SubjectCaMap { get; set; } = new();

        /// <summary>Danh sách môn thi ở Ca 1</summary>
        public List<string> Ca1Subjects { get; set; } = new();

        /// <summary>Danh sách môn thi ở Ca 2</summary>
        public List<string> Ca2Subjects { get; set; } = new();

        public int ConflictCount { get; set; }
        public bool IsPerfect { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
