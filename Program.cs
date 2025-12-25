using System;
using System.Linq;
using System.Windows.Forms;

namespace QuanLyGame_Final
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // --- [QUAN TRỌNG] TỰ ĐỘNG QUÉT VÀ MÃ HÓA DỮ LIỆU CŨ ---
            // Chức năng này chạy ngầm mỗi khi mở App
            try
            {
                using (var db = new GameContext())
                {
                    // 1. Kiểm tra nếu chưa có Admin thì tạo mới (Đã mã hóa sẵn)
                    if (!db.Users.Any())
                    {
                        db.Users.Add(new User
                        {
                            Username = "admin",
                            // Mã hóa ngay từ lúc tạo mới
                            Password = SecurityHelper.Encrypt("123"),
                            FullName = "Quản trị viên",
                            Role = "Admin"
                        });
                        db.SaveChanges();
                    }

                    // 2. Quét toàn bộ User cũ để vá lỗi bảo mật
                    var allUsers = db.Users.ToList();
                    bool coThayDoi = false;

                    foreach (var u in allUsers)
                    {
                        // Mẹo: Thử giải mã mật khẩu hiện tại
                        string thuGiaiMa = SecurityHelper.Decrypt(u.Password);

                        // Nếu giải mã ra Y HỆT ban đầu (nghĩa là nó chưa bị mã hóa)
                        // Ví dụ: u.Password là "123", giải mã vẫn ra "123" => Cần mã hóa lại
                        if (thuGiaiMa == u.Password)
                        {
                            u.Password = SecurityHelper.Encrypt(u.Password);
                            coThayDoi = true;
                        }
                    }

                    if (coThayDoi)
                    {
                        db.SaveChanges(); // Lưu xuống SQL
                    }
                }
            }
            catch (Exception ex)
            {
                // Nếu lỗi kết nối DB lần đầu thì bỏ qua, để FormLogin tự xử lý
            }
            // -------------------------------------------------------

            // Chạy quy trình đăng nhập như bình thường
            while (true)
            {
                FormLogin frmLogin = new FormLogin();
                if (frmLogin.ShowDialog() == DialogResult.OK)
                {
                    User user = frmLogin.UserDaDangNhap;
                    FormStore frmStore = new FormStore(user);

                    if (frmStore.ShowDialog() == DialogResult.OK)
                    {
                        continue; // Đăng xuất -> Quay lại Login
                    }
                    else break; // Thoát
                }
                else break;
            }
        }
    }
}