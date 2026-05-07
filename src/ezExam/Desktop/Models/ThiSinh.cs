public class ThiSinh
{
    public int Id { get; set; }
    public int STT { get; set; }
    public string SBD { get; set; }
    public string HoTen { get; set; }
    public string NgaySinh { get; set; }
    public string Lop { get; set; }

    // Môn bắt buộc
    public bool MonVan { get; set; }
    public bool MonToan { get; set; }

    // Kết quả phân ca
    public string MonCa1 { get; set; }
    public string MonCa2 { get; set; }

    // Chuỗi tổng hợp các môn đăng ký
    public string CacMonThi { get; set; }

    public int KyThiId { get; set; }
}
