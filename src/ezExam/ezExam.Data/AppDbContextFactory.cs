using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Data
{
    /// <summary>
    /// Factory để EF Core CLI tạo migration.
    /// Dùng cùng đường dẫn với DatabaseInitializer để tránh xung đột.
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var opt = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(DatabaseInitializer.GetConnectionString()) // ← dùng chung 1 chỗ
                .Options;
            return new AppDbContext(opt);
        }
    }
}
