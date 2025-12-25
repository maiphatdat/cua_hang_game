using System.Collections.Generic;

namespace QuanLyGame_Final
{
    public class Category
    {
        public int Id { get; set; }
        public string TenDanhMuc { get; set; }
        public virtual ICollection<Game> Games { get; set; }
    }
}