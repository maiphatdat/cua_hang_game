using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyGame_Final
{
    public class OrderDetail
    {
        [Key]
        public int ID { get; set; }

        public string TenGame { get; set; }
        public decimal GiaTien { get; set; }

        // Khóa ngoại liên kết với bảng Order
        public int OrderID { get; set; } // [QUAN TRỌNG] Phải có dòng này

        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; }
    }
}