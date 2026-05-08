using ezExam.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Core.Services
{
    /// <summary>
    /// Interface quản lý kỳ thi
    /// </summary>
    public interface IExamSessionService
    {
        /// <summary>Lấy tất cả kỳ thi</summary>
        Task<List<ExamSession>> GetAllAsync();

        /// <summary>Lấy kỳ thi đang được đặt làm mặc định</summary>
        Task<ExamSession?> GetDefaultAsync();

        /// <summary>Thêm kỳ thi mới</summary>
        Task AddAsync(ExamSession session);

        /// <summary>Cập nhật thông tin kỳ thi</summary>
        Task UpdateAsync(ExamSession session);

        /// <summary>Xóa kỳ thi (cascade xóa luôn thí sinh và phòng thi)</summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Đặt kỳ thi làm mặc định.
        /// Kỳ thi mặc định là kỳ thi đang được thao tác.
        /// </summary>
        Task SetDefaultAsync(int id);
    }
}
