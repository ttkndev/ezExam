namespace Desktop.Models
{
    public class ThiSinh
    {
        public int Id { get; set; }          // Khóa chính
        public int STT { get; set; }         // Số thứ tự
        public string SBD { get; set; }      // Số báo danh
        public string HoTen { get; set; }    // Họ và tên
        public string NgaySinh { get; set; } // Ngày sinh
        public string GioiTinh { get; set; } // Giới tính
        public string Lop { get; set; }      // Lớp

        // Các môn thi (0 = không đăng ký, 1 = có đăng ký)
        public int MonToan { get; set; }
        public int MonVan { get; set; }
        public int MonSu { get; set; }
        public int MonDia { get; set; }
        public int MonLy { get; set; }
        public int MonHoa { get; set; }
        public int MonSinh { get; set; }
        public int MonKTPL { get; set; }
        public int MonTin { get; set; }
        public int MonCNCN { get; set; }
        public int MonCNNN { get; set; }

        public string NN { get; set; }       // Ngoại ngữ (Anh, Đức, Nhật, Hàn, Trung Quốc, …)

        public int KyThiId { get; set; }     // Liên kết tới kỳ thi
    }
}
