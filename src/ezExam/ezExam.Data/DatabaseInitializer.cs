using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Data
{
    /// <summary>
    /// Khởi tạo database khi ứng dụng chạy lần đầu.
    /// File SQLite được đặt trong thư mục "databases" cùng chỗ với file .exe
    /// Ví dụ: C:\...\ezExam\databases\exam.db
    /// </summary>
    public static class DatabaseInitializer
    {
        /// <summary>
        /// Thư mục chứa file database (tạo tự động nếu chưa có)
        /// </summary>
        private static string DbFolder =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "databases");

        /// <summary>
        /// Đường dẫn đầy đủ đến file SQLite
        /// </summary>
        private static string DbFilePath =>
            Path.Combine(DbFolder, "exam.db");

        /// <summary>
        /// Khởi tạo database: tạo thư mục và chạy migration tự động
        /// </summary>
        public static async Task InitializeAsync(AppDbContext db)
        {
            // Tạo thư mục databases nếu chưa có
            Directory.CreateDirectory(DbFolder);

            // Chạy migration tự động (tạo bảng nếu chưa có)
            await db.Database.MigrateAsync();
        }

        /// <summary>
        /// Connection string trỏ đến file exam.db trong thư mục databases/
        /// </summary>
        public static string GetConnectionString()
        {
            // Đảm bảo thư mục tồn tại trước khi trả về connection string
            Directory.CreateDirectory(DbFolder);
            return $"Data Source={DbFilePath}";
        }
    }
}
