using Desktop.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace Desktop.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Data Source=ezExam.db";

        public DatabaseService()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS KyThi (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TenKyThi TEXT NOT NULL,
                    NgayThi TEXT NOT NULL,
                    GhiChu TEXT
                );
            ";
            command.ExecuteNonQuery();
        }
    }
}
