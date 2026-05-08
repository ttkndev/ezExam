using ezExam.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Data.Repositories
{
    /// <summary>Repository chuyên biệt cho ExamRoom</summary>
    public class ExamRoomRepository : Repository<ExamRoom>
    {
        public ExamRoomRepository(AppDbContext db) : base(db) { }

        /// <summary>
        /// Lấy danh sách phòng theo kỳ thi và loại phòng,
        /// kèm theo danh sách thí sinh (Assignments + Candidate)
        /// </summary>
        public Task<List<ExamRoom>> GetBySessionAndTypeAsync(int sessionId, RoomType type)
            => _set
               .Where(r => r.ExamSessionId == sessionId && r.Type == type)
               .Include(r => r.Assignments)
                   .ThenInclude(a => a.Candidate)
               .OrderBy(r => r.RoomName)
               .ToListAsync();

        /// <summary>Xóa toàn bộ phòng thi của một kỳ thi (dùng khi xếp lại)</summary>
        public async Task DeleteBySessionAsync(int sessionId)
        {
            var list = await _set.Where(r => r.ExamSessionId == sessionId).ToListAsync();
            _set.RemoveRange(list);
            await SaveChangesAsync();
        }
    }
}
