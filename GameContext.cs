using System.Data.Entity;

namespace QuanLyGame_Final
{
    public class GameContext : DbContext
    {
        public GameContext() : base("name=ChuoiKetNoi")
        {
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<GameContext>());
            // Chỉ tạo nếu chưa có. Có rồi thì giữ nguyên (không xóa, không sửa cấu trúc)
            Database.SetInitializer(new CreateDatabaseIfNotExists<GameContext>());
        }

        public DbSet<Game> Games { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        // --- BẮT BUỘC PHẢI CÓ DÒNG NÀY ĐỂ LƯU LỊCH SỬ ---
        public DbSet<LoginLog> LoginLogs { get; set; }

    }
}