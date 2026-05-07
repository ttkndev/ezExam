using ClosedXML.Excel;
using Desktop.Models;
using System.Linq;

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
                var subjects = row.Cell(6).GetString();
                var subjectList = subjects.Split(',').Select(s => s.Trim()).ToList();

                string nn = subjectList.FirstOrDefault(s =>
                    s == "Anh" || s == "Nga" || s == "Pháp" ||
                    s == "Trung Quốc" || s == "Đức" ||
                    s == "Nhật" || s == "Hàn") ?? "";

                var ts = new ThiSinh
                {
                    STT = row.Cell(1).GetValue<int>(),
                    SBD = row.Cell(2).GetString(),
                    HoTen = row.Cell(3).GetString(),
                    NgaySinh = row.Cell(4).GetString(),
                    Lop = row.Cell(5).GetString(),
                    GioiTinh = "",

                    MonVan = subjectList.Contains("Ngữ văn") ? 1 : 0,
                    MonToan = subjectList.Contains("Toán") ? 1 : 0,
                    MonSu = subjectList.Contains("Sử") ? 1 : 0,
                    MonDia = subjectList.Contains("Địa") ? 1 : 0,
                    MonLy = subjectList.Contains("Lý") ? 1 : 0,
                    MonHoa = subjectList.Contains("Hóa") ? 1 : 0,
                    MonSinh = subjectList.Contains("Sinh") ? 1 : 0,
                    MonKTPL = subjectList.Contains("KTPL") ? 1 : 0,
                    MonTin = subjectList.Contains("Tin") ? 1 : 0,
                    MonCNCN = subjectList.Contains("CNCN") ? 1 : 0,
                    MonCNNN = subjectList.Contains("CNNN") ? 1 : 0,

                    NN = nn,
                    KyThiId = kyThiMacDinh.Id
                };

                _thiSinhService.AddThiSinh(ts, kyThiMacDinh.Id);
            }
        }

    }
}
