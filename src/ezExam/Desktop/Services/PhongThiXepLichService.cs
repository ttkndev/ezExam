using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using Desktop.Models;

namespace Desktop.Services
{
    public class PhongThiXepLichService
    {
        private readonly string _connectionString = "Data Source=ezExam.db";

        public PhongThiXepLichService()
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

                CREATE TABLE IF NOT EXISTS PhongThiXepLich (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    KyThiId INTEGER NOT NULL,
                    RoomNumber INTEGER NOT NULL,
                    Subject TEXT NOT NULL,
                    CandidateCount INTEGER NOT NULL,
                    StartSbd INTEGER NOT NULL,
                    EndSbd INTEGER NOT NULL,
                    Strategy TEXT NOT NULL,
                    RoomCapacity INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL
                );
            ";
            command.ExecuteNonQuery();
        }

        public void SaveRoomAllocations(int kyThiId, List<RoomAllocationRow> rows, string strategy, int roomCapacity)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM PhongThiXepLich WHERE KyThiId=$kythi";
            deleteCommand.Parameters.AddWithValue("$kythi", kyThiId);
            deleteCommand.ExecuteNonQuery();

            foreach (var row in rows)
            {
                var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = @"
                    INSERT INTO PhongThiXepLich (
                        KyThiId, RoomNumber, Subject, CandidateCount, StartSbd, EndSbd,
                        Strategy, RoomCapacity, CreatedAt
                    ) VALUES (
                        $kythi, $room, $subject, $count, $start, $end,
                        $strategy, $capacity, $createdAt
                    )";

                insertCommand.Parameters.AddWithValue("$kythi", kyThiId);
                insertCommand.Parameters.AddWithValue("$room", row.RoomNumber);
                insertCommand.Parameters.AddWithValue("$subject", row.Subject);
                insertCommand.Parameters.AddWithValue("$count", row.CandidateCount);
                insertCommand.Parameters.AddWithValue("$start", row.StartSbd);
                insertCommand.Parameters.AddWithValue("$end", row.EndSbd);
                insertCommand.Parameters.AddWithValue("$strategy", strategy);
                insertCommand.Parameters.AddWithValue("$capacity", roomCapacity);
                insertCommand.Parameters.AddWithValue("$createdAt", DateTime.UtcNow.ToString("O"));
                insertCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }
}
