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

                    MonToan INTEGER,
                    MonVan INTEGER,
                    MonSu INTEGER,
                    MonDia INTEGER,
                    MonLy INTEGER,
                    MonHoa INTEGER,
                    MonSinh INTEGER,
                    MonKTPL INTEGER,
                    MonTin INTEGER,
                    MonCNCN INTEGER,
                    MonCNNN INTEGER,

                    NN TEXT,
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
                    MonToan = reader.GetInt32(7),
                    MonVan = reader.GetInt32(8),
                    MonSu = reader.GetInt32(9),
                    MonDia = reader.GetInt32(10),
                    MonLy = reader.GetInt32(11),
                    MonHoa = reader.GetInt32(12),
                    MonSinh = reader.GetInt32(13),
                    MonKTPL = reader.GetInt32(14),
                    MonTin = reader.GetInt32(15),
                    MonCNCN = reader.GetInt32(16),
                    MonCNNN = reader.GetInt32(17),
                    NN = reader.GetString(18),
                    KyThiId = reader.GetInt32(19)
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
                    MonToan, MonVan, MonSu, MonDia, MonLy, MonHoa, MonSinh,
                    MonKTPL, MonTin, MonCNCN, MonCNNN, NN, KyThiId)
                VALUES ($stt, $sbd, $hoten, $ngaysinh, $gioitinh, $lop,
                    $toan, $van, $su, $dia, $ly, $hoa, $sinh,
                    $ktpl, $tin, $cncn, $cnnn, $nn, $kythi)";
            command.Parameters.AddWithValue("$stt", ts.STT);
            command.Parameters.AddWithValue("$sbd", ts.SBD);
            command.Parameters.AddWithValue("$hoten", ts.HoTen);
            command.Parameters.AddWithValue("$ngaysinh", ts.NgaySinh);
            command.Parameters.AddWithValue("$gioitinh", ts.GioiTinh);
            command.Parameters.AddWithValue("$lop", ts.Lop);
            command.Parameters.AddWithValue("$toan", ts.MonToan);
            command.Parameters.AddWithValue("$van", ts.MonVan);
            command.Parameters.AddWithValue("$su", ts.MonSu);
            command.Parameters.AddWithValue("$dia", ts.MonDia);
            command.Parameters.AddWithValue("$ly", ts.MonLy);
            command.Parameters.AddWithValue("$hoa", ts.MonHoa);
            command.Parameters.AddWithValue("$sinh", ts.MonSinh);
            command.Parameters.AddWithValue("$ktpl", ts.MonKTPL);
            command.Parameters.AddWithValue("$tin", ts.MonTin);
            command.Parameters.AddWithValue("$cncn", ts.MonCNCN);
            command.Parameters.AddWithValue("$cnnn", ts.MonCNNN);
            command.Parameters.AddWithValue("$nn", ts.NN);
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
                    MonToan=$toan, MonVan=$van, MonSu=$su, MonDia=$dia, MonLy=$ly, MonHoa=$hoa, MonSinh=$sinh,
                    MonKTPL=$ktpl, MonTin=$tin, MonCNCN=$cncn, MonCNNN=$cnnn, NN=$nn, KyThiId=$kythi
                WHERE Id=$id";
            command.Parameters.AddWithValue("$id", ts.Id);
            command.Parameters.AddWithValue("$stt", ts.STT);
            command.Parameters.AddWithValue("$sbd", ts.SBD);
            command.Parameters.AddWithValue("$hoten", ts.HoTen);
            command.Parameters.AddWithValue("$ngaysinh", ts.NgaySinh);
            command.Parameters.AddWithValue("$gioitinh", ts.GioiTinh);
            command.Parameters.AddWithValue("$lop", ts.Lop);
            command.Parameters.AddWithValue("$toan", ts.MonToan);
            command.Parameters.AddWithValue("$van", ts.MonVan);
            command.Parameters.AddWithValue("$su", ts.MonSu);
            command.Parameters.AddWithValue("$dia", ts.MonDia);
            command.Parameters.AddWithValue("$ly", ts.MonLy);
            command.Parameters.AddWithValue("$hoa", ts.MonHoa);
            command.Parameters.AddWithValue("$sinh", ts.MonSinh);
            command.Parameters.AddWithValue("$ktpl", ts.MonKTPL);
            command.Parameters.AddWithValue("$tin", ts.MonTin);
            command.Parameters.AddWithValue("$cncn", ts.MonCNCN);
            command.Parameters.AddWithValue("$cnnn", ts.MonCNNN);
            command.Parameters.AddWithValue("$nn", ts.NN);
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
