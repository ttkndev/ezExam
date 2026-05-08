using ezExam.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Data.Repositories
{
    /// <summary>Repository chuyên biệt cho ExamSession, có thêm truy vấn đặc thù</summary>
    public class ExamSessionRepository : Repository<ExamSession>
    {
        public ExamSessionRepository(AppDbContext db) : base(db) { }

        /// <summary>Lấy kỳ thi đang được đặt làm mặc định</summary>
        public Task<ExamSession?> GetDefaultAsync()
            => _set.FirstOrDefaultAsync(s => s.IsDefault);

        /// <summary>Đặt một kỳ thi làm mặc định, bỏ mặc định của kỳ thi cũ</summary>
        public async Task SetDefaultAsync(int id)
        {
            // Bỏ mặc định tất cả
            await _set.ForEachAsync(s => s.IsDefault = false);
            // Đặt mặc định cho kỳ thi được chọn
            var session = await GetByIdAsync(id);
            if (session != null) session.IsDefault = true;
            await SaveChangesAsync();
        }
    }
}
