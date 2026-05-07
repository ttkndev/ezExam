namespace Desktop.Models
{
    public class KyThi
    {
        public int Id { get; set; }
        public string TenKyThi { get; set; } = string.Empty;
        public string NgayThi { get; set; } = string.Empty;
        public int MacDinh { get; set; }
    }
}
