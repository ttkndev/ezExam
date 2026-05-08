using ezExam.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Core.Services
{
    public interface IRoomSchedulerService
    {
        /// <summary>
        /// Xếp phòng thi cho toàn bộ kỳ thi.
        /// Gọi sau khi đã xếp ca xong.
        /// </summary>
        /// <param name="sessionId">Id kỳ thi</param>
        /// <param name="subjectShiftMap">
        /// Kết quả xếp ca: key = tên môn, value = 1 hoặc 2.
        /// Lấy từ ShiftScheduleResult.SubjectShiftMap
        /// </param>
        /// <param name="capacity">Sức chứa mỗi phòng, mặc định 24</param>
        Task<List<ExamRoom>> ScheduleRoomsAsync(
            int sessionId,
            Dictionary<string, int> subjectShiftMap,
            int capacity = 24);
    }
}
