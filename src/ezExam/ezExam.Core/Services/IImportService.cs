using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Core.Services
{
    /// <summary>
    /// Interface import danh sách thí sinh từ file Excel
    /// </summary>
    public interface IImportService
    {
        /// <summary>
        /// Đọc file Excel, parse thí sinh và lưu vào database.
        /// Nếu đã có dữ liệu cũ trong kỳ thi → xóa và import lại.
        /// </summary>
        /// <param name="filePath">Đường dẫn file .xlsx</param>
        /// <param name="sessionId">Id kỳ thi đang chọn</param>
        Task ImportFromExcelAsync(string filePath, int sessionId);
    }
}
