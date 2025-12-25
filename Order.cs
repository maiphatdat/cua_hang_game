using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyGame_Final
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; } // [QUAN TRỌNG] Phải đặt tên là OrderID

        public DateTime NgayMua { get; set; }
        public decimal TongTien { get; set; }
        public string NguoiBan { get; set; }

        // Quan hệ 1-N: Một đơn hàng có nhiều chi tiết món
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}