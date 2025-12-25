using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyGame_Final
{
    public class LoginLog
    {
        public int Id { get; set; }
        public string Username { get; set; } // Ai đăng nhập?
        public string FullName { get; set; }
        public string Role { get; set; }
        public DateTime ThoiGianLogin { get; set; } // Vào lúc mấy giờ?
    }
}