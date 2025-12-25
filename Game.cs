namespace QuanLyGame_Final
{
    public class Game
    {
        public int Id { get; set; }
        public string TenGame { get; set; }
        public decimal GiaTien { get; set; }

        // Thêm trường này để lưu đường dẫn ảnh
        public string HinhAnh { get; set; }

        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }
    }
}