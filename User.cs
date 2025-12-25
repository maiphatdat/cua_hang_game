using System.ComponentModel.DataAnnotations;

namespace QuanLyGame_Final
{
    public class User
    {
        [Key]
        // PHẢI CÓ { get; set; } THÌ MỚI LẤY ĐƯỢC DỮ LIỆU
        public string Username { get; set; }
        public string Password { get; set; }
        public string PasswordVerify { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
    }
}