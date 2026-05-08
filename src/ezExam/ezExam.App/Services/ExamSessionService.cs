using ezExam.Core.Models;
using ezExam.Core.Services;
using ezExam.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.App.Services
{
    /// <summary>
    /// Service quản lý kỳ thi: thêm, sửa, xóa, đặt mặc định
    /// </summary>
    public class ExamSessionService : IExamSessionService
    {
        private readonly ExamSessionRepository _repo;

        public ExamSessionService(ExamSessionRepository repo)
        {
            _repo = repo;
        }

        public Task<List<ExamSession>> GetAllAsync()
            => _repo.GetAllAsync();

        public Task<ExamSession?> GetDefaultAsync()
            => _repo.GetDefaultAsync();

        public async Task AddAsync(ExamSession session)
        {
            // Nếu chưa có kỳ thi nào thì tự động đặt làm mặc định
            var all = await _repo.GetAllAsync();
            if (!all.Any()) session.IsDefault = true;

            await _repo.AddAsync(session);
            await _repo.SaveChangesAsync();
        }

        public async Task UpdateAsync(ExamSession session)
        {
            await _repo.UpdateAsync(session);
            await _repo.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var session = await _repo.GetByIdAsync(id);
            if (session == null) return;

            await _repo.DeleteAsync(session);
            await _repo.SaveChangesAsync();

            // Nếu xóa kỳ thi đang mặc định, đặt kỳ thi đầu tiên còn lại làm mặc định
            if (session.IsDefault)
            {
                var remaining = await _repo.GetAllAsync();
                if (remaining.Any())
                {
                    remaining[0].IsDefault = true;
                    await _repo.UpdateAsync(remaining[0]);
                    await _repo.SaveChangesAsync();
                }
            }
        }

        public Task SetDefaultAsync(int id)
            => _repo.SetDefaultAsync(id);
    }
}
