using ezExam.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Data.Repositories
{
    /// <summary>Repository chuyên biệt cho Candidate</summary>
    public class CandidateRepository : Repository<Candidate>
    {
        public CandidateRepository(AppDbContext db) : base(db) { }

        /// <summary>Lấy tất cả thí sinh theo kỳ thi, sắp xếp theo SBD</summary>
        public Task<List<Candidate>> GetBySessionAsync(int sessionId)
            => _set.Where(c => c.ExamSessionId == sessionId)
                   .OrderBy(c => c.CandidateNumber)
                   .ToListAsync();

        /// <summary>Xóa toàn bộ thí sinh của một kỳ thi (dùng khi re-import)</summary>
        public async Task DeleteBySessionAsync(int sessionId)
        {
            var list = await _set.Where(c => c.ExamSessionId == sessionId).ToListAsync();
            _set.RemoveRange(list);
            await SaveChangesAsync();
        }
    }
}
