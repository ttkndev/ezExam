using ClosedXML.Excel;
using ezExam.Core.Models;
using ezExam.Core.Services;
using ezExam.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.App.Services
{
    /// <summary>
    /// Service import danh sách thí sinh từ file Excel (.xlsx)
    /// Cấu trúc Excel: STT | Số báo danh | Họ và tên | Ngày sinh | Lớp | Các môn thi
    /// </summary>
    public class ImportService : IImportService
    {
        private readonly CandidateRepository _candidateRepo;

        // Tên các môn bắt buộc (không phân biệt hoa thường)
        private static readonly HashSet<string> RequiredSubjects = new(StringComparer.OrdinalIgnoreCase)
    {
        "ngữ văn", "văn", "toán"
    };

        public ImportService(CandidateRepository candidateRepo)
        {
            _candidateRepo = candidateRepo;
        }

        /// <summary>
        /// Import thí sinh từ Excel vào kỳ thi được chọn.
        /// Nếu đã có dữ liệu cũ sẽ xóa và import lại.
        /// </summary>
        public async Task ImportFromExcelAsync(string filePath, int sessionId)
        {
            // Xóa dữ liệu cũ trước khi import lại
            await _candidateRepo.DeleteBySessionAsync(sessionId);

            var candidates = new List<Candidate>();

            using var workbook = new XLWorkbook(filePath);
            var sheet = workbook.Worksheet(1); // Lấy sheet đầu tiên

            // Tìm dòng dữ liệu đầu tiên (bỏ qua header)
            // Giả sử dòng 1 là header, dữ liệu bắt đầu từ dòng 2
            var rows = sheet.RangeUsed()!.RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                try
                {
                    // Đọc từng cột theo cấu trúc Excel đầu vào
                    var fullName = row.Cell(3).GetString().Trim(); // Cột C: Họ và tên
                    var dobStr = row.Cell(4).GetString().Trim(); // Cột D: Ngày sinh
                    var className = row.Cell(5).GetString().Trim(); // Cột E: Lớp
                    var subjectStr = row.Cell(6).GetString().Trim(); // Cột F: Các môn thi

                    // Bỏ qua dòng trống
                    if (string.IsNullOrWhiteSpace(fullName)) continue;

                    // Parse ngày sinh
                    DateTime dob = DateTime.MinValue;
                    if (!string.IsNullOrEmpty(dobStr))
                        DateTime.TryParseExact(dobStr, "dd/MM/yyyy",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out dob);

                    // Tách họ đệm và tên (dùng để sắp xếp theo tên)
                    var (firstName, lastName) = SplitName(fullName);

                    // Chuẩn hóa danh sách môn thi
                    var subjects = ParseSubjects(subjectStr);

                    candidates.Add(new Candidate
                    {
                        FullName = fullName,
                        FirstName = firstName,
                        LastName = lastName,
                        DateOfBirth = dob,
                        ClassName = className,
                        SubjectNames = string.Join(", ", subjects),
                        ExamSessionId = sessionId,
                        CandidateNumber = "" // Sẽ được đánh sau khi sắp xếp
                    });
                }
                catch
                {
                    // Bỏ qua dòng lỗi, tiếp tục import
                    continue;
                }
            }

            await _candidateRepo.AddRangeAsync(candidates);
            await _candidateRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Tách họ đệm và tên từ họ tên đầy đủ.
        /// Ví dụ: "Nguyễn Thị Kim Ngân" → firstName="Ngân", lastName="Nguyễn Thị Kim"
        /// </summary>
        private static (string firstName, string lastName) SplitName(string fullName)
        {
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return (parts[0], "");

            var firstName = parts[^1]; // Tên = từ cuối cùng
            var lastName = string.Join(" ", parts[..^1]); // Họ đệm = phần còn lại
            return (firstName, lastName);
        }

        /// <summary>
        /// Parse và chuẩn hóa danh sách môn thi từ chuỗi.
        /// Ví dụ: "Ngữ văn, Toán, Sử, KTPL" → ["Ngữ văn", "Toán", "Lịch sử", "KTPL"]
        /// </summary>
        private static List<string> ParseSubjects(string subjectStr)
        {
            if (string.IsNullOrWhiteSpace(subjectStr)) return new();

            return subjectStr
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => NormalizeSubjectName(s.Trim()))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        /// <summary>
        /// Chuẩn hóa tên môn thi về dạng thống nhất.
        /// Ví dụ: "Sử" → "Lịch sử", "KTPL" → "KTPL"
        /// </summary>
        private static string NormalizeSubjectName(string name) => name.ToLower() switch
        {
            "sử" or "lịch sử" => "Lịch sử",
            "địa" or "địa lý" => "Địa lý",
            "lý" or "vật lý" => "Vật lý",
            "hóa" or "hóa học" => "Hóa học",
            "sinh" or "sinh học" => "Sinh học",
            "tin" or "tin học" => "Tin học",
            "ktpl" or "kinh tế pháp luật" => "KTPL",
            "ngữ văn" or "văn" => "Ngữ văn",
            "toán" => "Toán",
            "gdqp" or "gdqpan" => "GDQP",
            _ => name // Giữ nguyên nếu không khớp
        };
    }
}
