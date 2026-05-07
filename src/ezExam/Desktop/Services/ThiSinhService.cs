using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using Desktop.Models;

namespace Desktop.Services
{
    public class ThiSinhService
    {
        private readonly string _connectionString = "Data Source=ezExam.db";

        public ThiSinhService()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ThiSinh (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    STT INTEGER,
                    SBD TEXT,
                    HoTen TEXT,
                    NgaySinh TEXT,
                    GioiTinh TEXT,
                    Lop TEXT,

                    MonVan INTEGER,
                    MonToan INTEGER,

                    CacMonThi TEXT,
                    MonCa1 TEXT,
                    MonCa2 TEXT,

                    KyThiId INTEGER
                );
            ";
            command.ExecuteNonQuery();
        }

        // READ: lấy danh sách theo kỳ thi
        public List<ThiSinh> GetThiSinhByKyThi(int kyThiId)
        {
            var list = new List<ThiSinh>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM ThiSinh WHERE KyThiId=$kythi";
            command.Parameters.AddWithValue("$kythi", kyThiId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ThiSinh
                {
                    Id = reader.GetInt32(0),
                    STT = reader.GetInt32(1),
                    SBD = reader.GetString(2),
                    HoTen = reader.GetString(3),
                    NgaySinh = reader.GetString(4),
                    GioiTinh = reader.GetString(5),
                    Lop = reader.GetString(6),
                    MonVan = reader.GetInt32(7) == 1,
                    MonToan = reader.GetInt32(8) == 1,
                    CacMonThi = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    MonCa1 = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    MonCa2 = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    KyThiId = reader.GetInt32(12)
                });
            }
            return list;
        }

        // CREATE: thêm thí sinh
        public void AddThiSinh(ThiSinh ts, int kyThiId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ThiSinh (STT, SBD, HoTen, NgaySinh, GioiTinh, Lop,
                    MonVan, MonToan, CacMonThi, MonCa1, MonCa2, KyThiId)
                VALUES ($stt, $sbd, $hoten, $ngaysinh, $gioitinh, $lop,
                    $van, $toan, $cacmon, $ca1, $ca2, $kythi)";
            command.Parameters.AddWithValue("$stt", ts.STT);
            command.Parameters.AddWithValue("$sbd", ts.SBD);
            command.Parameters.AddWithValue("$hoten", ts.HoTen);
            command.Parameters.AddWithValue("$ngaysinh", ts.NgaySinh);
            command.Parameters.AddWithValue("$gioitinh", ts.GioiTinh);
            command.Parameters.AddWithValue("$lop", ts.Lop);
            command.Parameters.AddWithValue("$van", ts.MonVan ? 1 : 0);
            command.Parameters.AddWithValue("$toan", ts.MonToan ? 1 : 0);
            command.Parameters.AddWithValue("$cacmon", ts.CacMonThi ?? "");
            command.Parameters.AddWithValue("$ca1", ts.MonCa1 ?? "");
            command.Parameters.AddWithValue("$ca2", ts.MonCa2 ?? "");
            command.Parameters.AddWithValue("$kythi", kyThiId);
            command.ExecuteNonQuery();
        }

        // UPDATE: sửa thí sinh
        public void UpdateThiSinh(ThiSinh ts)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ThiSinh SET
                    STT=$stt, SBD=$sbd, HoTen=$hoten, NgaySinh=$ngaysinh, GioiTinh=$gioitinh, Lop=$lop,
                    MonVan=$van, MonToan=$toan,
                    CacMonThi=$cacmon, MonCa1=$ca1, MonCa2=$ca2, KyThiId=$kythi
                WHERE Id=$id";
            command.Parameters.AddWithValue("$id", ts.Id);
            command.Parameters.AddWithValue("$stt", ts.STT);
            command.Parameters.AddWithValue("$sbd", ts.SBD);
            command.Parameters.AddWithValue("$hoten", ts.HoTen);
            command.Parameters.AddWithValue("$ngaysinh", ts.NgaySinh);
            command.Parameters.AddWithValue("$gioitinh", ts.GioiTinh);
            command.Parameters.AddWithValue("$lop", ts.Lop);
            command.Parameters.AddWithValue("$van", ts.MonVan ? 1 : 0);
            command.Parameters.AddWithValue("$toan", ts.MonToan ? 1 : 0);
            command.Parameters.AddWithValue("$cacmon", ts.CacMonThi ?? "");
            command.Parameters.AddWithValue("$ca1", ts.MonCa1 ?? "");
            command.Parameters.AddWithValue("$ca2", ts.MonCa2 ?? "");
            command.Parameters.AddWithValue("$kythi", ts.KyThiId);
            command.ExecuteNonQuery();
        }

        // DELETE: xóa thí sinh theo Id
        public void DeleteThiSinh(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ThiSinh WHERE Id=$id";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }

        // DELETE ALL: xóa toàn bộ thí sinh của kỳ thi
        public void DeleteAllThiSinhByKyThi(int kyThiId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ThiSinh WHERE KyThiId=$kythi";
            command.Parameters.AddWithValue("$kythi", kyThiId);
            command.ExecuteNonQuery();
        }
    }
}
