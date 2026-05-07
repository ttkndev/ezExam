using Desktop.Models;
using Desktop.Models.Desktop.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace Desktop.Services
{
    public class KyThiService
    {
        private readonly string _connectionString = "Data Source=ezExam.db";

        public KyThiService()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS KyThi (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TenKyThi TEXT,
                    NgayThi TEXT,
                    MacDinh INTEGER
                );
            ";
            command.ExecuteNonQuery();
        }

        public List<KyThi> GetKyThi()
        {
            var list = new List<KyThi>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM KyThi";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new KyThi
                {
                    Id = reader.GetInt32(0),
                    TenKyThi = reader.GetString(1),
                    NgayThi = reader.GetString(2),
                    MacDinh = reader.GetInt32(3)
                });
            }
            return list;
        }

        public void AddKyThi(KyThi kt)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO KyThi (TenKyThi, NgayThi, MacDinh)
                VALUES ($ten, $ngay, $macdinh)";
            command.Parameters.AddWithValue("$ten", kt.TenKyThi);
            command.Parameters.AddWithValue("$ngay", kt.NgayThi);
            command.Parameters.AddWithValue("$macdinh", kt.MacDinh);
            command.ExecuteNonQuery();
        }

        public void UpdateKyThi(KyThi kt)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE KyThi SET TenKyThi=$ten, NgayThi=$ngay, MacDinh=$macdinh
                WHERE Id=$id";
            command.Parameters.AddWithValue("$id", kt.Id);
            command.Parameters.AddWithValue("$ten", kt.TenKyThi);
            command.Parameters.AddWithValue("$ngay", kt.NgayThi);
            command.Parameters.AddWithValue("$macdinh", kt.MacDinh);
            command.ExecuteNonQuery();
        }

        public void DeleteKyThi(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM KyThi WHERE Id=$id";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }

        public void SetMacDinh(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE KyThi SET MacDinh=0";
            command.ExecuteNonQuery();

            command = connection.CreateCommand();
            command.CommandText = "UPDATE KyThi SET MacDinh=1 WHERE Id=$id";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }

        public KyThi GetKyThiMacDinh()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM KyThi WHERE MacDinh=1 LIMIT 1";
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new KyThi
                {
                    Id = reader.GetInt32(0),
                    TenKyThi = reader.GetString(1),
                    NgayThi = reader.GetString(2),
                    MacDinh = reader.GetInt32(3)
                };
            }
            return null;
        }
    }
}
