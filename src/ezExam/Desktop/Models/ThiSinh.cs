namespace Desktop.Models
{
    public class ThiSinh
    {
        public int Id { get; set; }
        public int STT { get; set; }
        public string SBD { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string NgaySinh { get; set; } = string.Empty;
        public string GioiTinh { get; set; } = string.Empty;
        public string Lop { get; set; } = string.Empty;

        public bool MonVan { get; set; }
        public bool MonToan { get; set; }

        public string MonCa1 { get; set; } = string.Empty;
        public string MonCa2 { get; set; } = string.Empty;
        public string CacMonThi { get; set; } = string.Empty;

        public int KyThiId { get; set; }
    }
}
