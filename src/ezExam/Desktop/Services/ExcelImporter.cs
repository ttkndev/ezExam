using ClosedXML.Excel;
using Desktop.Models;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Desktop.Services
{
    public class ExcelImporter
    {
        private readonly ThiSinhService _thiSinhService;
        private readonly KyThiService _kyThiService;

        public ExcelImporter()
        {
            _thiSinhService = new ThiSinhService();
            _kyThiService = new KyThiService();
        }

        public string[] GetSheetNames(string filePath)
        {
            using var workbook = new XLWorkbook(filePath);
            return workbook.Worksheets.Select(ws => ws.Name).ToArray();
        }

        public void Import(string filePath, string sheetName)
        {
            var kyThiMacDinh = _kyThiService.GetKyThiMacDinh();
            if (kyThiMacDinh == null)
                throw new InvalidOperationException("Chưa có kỳ thi mặc định!");

            // Xóa hết thí sinh của kỳ thi mặc định trước khi import
            _thiSinhService.DeleteAllThiSinhByKyThi(kyThiMacDinh.Id);

            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheet(sheetName);

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                // Giả sử cột 6 chứa chuỗi môn thi: "Ngữ văn, Toán, Sử, KTPL"
                var subjects = row.Cell(6).GetString();
                var subjectList = subjects.Split(',')
                                          .Select(s => s.Trim())
                                          .Where(s => !string.IsNullOrEmpty(s))
                                          .ToList();

                // Ghép lại thành chuỗi CacMonThi
                var cacMonThi = string.Join(", ", subjectList);

                var ts = new ThiSinh
                {
                    STT = row.Cell(1).GetValue<int>(),
                    SBD = row.Cell(2).GetString(),
                    HoTen = row.Cell(3).GetString(),
                    NgaySinh = row.Cell(4).GetString(),
                    Lop = row.Cell(5).GetString(),

                    MonVan = subjectList.Contains("Ngữ văn"),
                    MonToan = subjectList.Contains("Toán"),

                    // Ca1, Ca2 sẽ gán sau bằng thuật toán
                    MonCa1 = "",
                    MonCa2 = "",

                    CacMonThi = cacMonThi,
                    KyThiId = kyThiMacDinh.Id
                };

                _thiSinhService.AddThiSinh(ts, kyThiMacDinh.Id);
            }
        }
    }
}
