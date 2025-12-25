using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuanLyGame_Final
{
    public partial class FormLogin : Form
    {
        private TextBox txtUser, txtPass;
        private Button btnLogin, btnExit;
        GameContext db = new GameContext();

        // Biến này để truyền User sang FormStore thông qua Program
        public User UserDaDangNhap { get; private set; }

        public FormLogin()
        {
            SetupUI();
            KhoiTaoAdminMacDinh();
        }

        void KhoiTaoAdminMacDinh()
        {
            try
            {
                if (!db.Users.Any())
                {
                    db.Users.Add(new User { Username = "admin", Password = "123", FullName = "Quản trị viên", Role = "Admin" });
                    db.Users.Add(new User { Username = "staff", Password = "123", FullName = "Nhân viên 1", Role = "Staff" });
                    db.SaveChanges();
                }
            }
            catch { }
        }

        void SetupUI()
        {
            this.Text = "ĐĂNG NHẬP HỆ THỐNG";
            this.Size = new Size(450, 320);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Controls.Clear();

            Label lblTitle = new Label() { Text = "LOGIN SYSTEM", Font = new Font("Arial", 20, FontStyle.Bold), ForeColor = Color.White, Location = new Point(0, 30), Size = new Size(450, 40), TextAlign = ContentAlignment.MiddleCenter };

            Label l1 = new Label() { Text = "Tài khoản:", ForeColor = Color.LightGray, Location = new Point(50, 90), Font = new Font("Arial", 10), AutoSize = true };
            txtUser = new TextBox() { Location = new Point(140, 87), Width = 230, Font = new Font("Arial", 11) };

            Label l2 = new Label() { Text = "Mật khẩu:", ForeColor = Color.LightGray, Location = new Point(50, 140), Font = new Font("Arial", 10), AutoSize = true };
            txtPass = new TextBox() { Location = new Point(140, 137), Width = 230, PasswordChar = '*', Font = new Font("Arial", 11) };

            btnLogin = new Button() { Text = "ĐĂNG NHẬP", BackColor = Color.DodgerBlue, ForeColor = Color.White, Location = new Point(50, 190), Width = 320, Height = 45, Font = new Font("Arial", 10, FontStyle.Bold), Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += XuLyDangNhap;

            btnExit = new Button() { Text = "Thoát", BackColor = Color.Crimson, ForeColor = Color.White, Location = new Point(300, 245), Width = 70, Height = 30, Cursor = Cursors.Hand, FlatStyle = FlatStyle.Flat };
            btnExit.Click += (s, e) => Application.Exit();

            this.Controls.AddRange(new Control[] { lblTitle, l1, txtUser, l2, txtPass, btnLogin, btnExit });

            lblTitle.BringToFront(); txtUser.BringToFront(); txtPass.BringToFront(); btnLogin.BringToFront(); btnExit.BringToFront();
            this.AcceptButton = btnLogin;
        }

        private void XuLyDangNhap(object sender, EventArgs e)
        {
            string u = txtUser.Text.Trim();
            string p = txtPass.Text.Trim();

            // 1. Chỉ tìm user theo tên đăng nhập (chưa check pass vội)
            var user = db.Users.FirstOrDefault(x => x.Username == u);

            // 2. Nếu tìm thấy user, lôi pass ra GIẢI MÃ rồi mới so sánh với pass nhập vào
            if (user != null && SecurityHelper.Decrypt(user.Password) == p)
            {
                // --- ĐĂNG NHẬP THÀNH CÔNG (Code cũ giữ nguyên) ---
                LoginLog log = new LoginLog()
                {
                    Username = user.Username,
                    FullName = user.FullName,
                    Role = user.Role,
                    ThoiGianLogin = DateTime.Now
                };
                db.LoginLogs.Add(log);
                db.SaveChanges();

                UserDaDangNhap = user;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Sai tài khoản hoặc mật khẩu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}