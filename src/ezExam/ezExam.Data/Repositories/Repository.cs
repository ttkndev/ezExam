using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Data.Repositories
{
    /// <summary>
    /// Repository generic dùng chung cho mọi entity.
    /// Gộp unit-of-work vào đây cho đơn giản (project nhỏ).
    /// </summary>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _db;
        protected readonly DbSet<T> _set;

        public Repository(AppDbContext db)
        {
            _db = db;
            _set = db.Set<T>();
        }

        public Task<List<T>> GetAllAsync() => _set.ToListAsync();
        public Task<T?> GetByIdAsync(int id) => _set.FindAsync(id).AsTask();
        public async Task AddAsync(T entity) { await _set.AddAsync(entity); }
        public async Task AddRangeAsync(IEnumerable<T> entities) { await _set.AddRangeAsync(entities); }
        public Task UpdateAsync(T entity) { _db.Entry(entity).State = EntityState.Modified; return Task.CompletedTask; }
        public Task DeleteAsync(T entity) { _set.Remove(entity); return Task.CompletedTask; }
        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
