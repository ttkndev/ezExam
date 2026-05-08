using ezExam.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Core.Services
{
    public interface IShiftSchedulerService
    {
        /// <summary>
        /// Xếp ca thi cho các môn tự chọn.
        /// Trả về dictionary: key=tên môn, value=ca (1 hoặc 2)
        /// </summary>
        Task<ShiftScheduleResult> ScheduleAsync(int sessionId);
    }
}
