using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace QuanLyGame_Final
{
    public class FormUserMan : Form
    {
        TabControl tabControl;
        GameContext db = new GameContext();

        // Khai báo biến giao diện
        TextBox txtSearch, txtUser, txtPass, txtName, txtPassVerify;
        ComboBox cboRole;
        DataGridView dgvUser;
        DataGridView dgvLog;

        public FormUserMan()
        {
            SetupUI();
        }

        void SetupUI()
        {
            this.Text = "QUẢN TRỊ HỆ THỐNG";
            this.Size = new Size(1100, 750);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Tab Control
            tabControl = new TabControl() { Dock = DockStyle.Fill, Font = new Font("Arial", 11, FontStyle.Bold) };

            // Tab 1: Nhân viên
            TabPage tabUser = new TabPage("Danh sách Nhân viên");
            SetupTabUser(tabUser);
            tabControl.TabPages.Add(tabUser);

            // Tab 2: Lịch sử
            TabPage tabLog = new TabPage("Lịch sử Ra/Vào");
            SetupTabLog(tabLog);
            tabControl.TabPages.Add(tabLog);

            this.Controls.Add(tabControl);
        }

        void SetupTabUser(TabPage tab)
        {
            // --- 1. PANEL NHẬP LIỆU (TOP) ---
            Panel pnlTop = new Panel() { Dock = DockStyle.Top, Height = 300, BackColor = Color.WhiteSmoke };

            // --- A. KHUNG TÌM KIẾM (Căn giữa cho đẹp) ---
            // Tính toán vị trí để GroupBox nằm giữa: (1100 - 800) / 2 = 150
            GroupBox grpSearch = new GroupBox() { Text = "Tìm kiếm & Công cụ", Location = new Point(150, 10), Size = new Size(800, 70), Font = new Font("Arial", 11) };

            txtSearch = new TextBox() { Location = new Point(100, 25), Width = 350, Font = new Font("Arial", 12) };
            txtSearch.TextChanged += (s, e) => LoadUserList(txtSearch.Text);

            Button btnReload = new Button() { Text = "TẢI LẠI", Location = new Point(470, 23), Width = 100, Height = 30, BackColor = Color.Blue, ForeColor = Color.White };
            btnReload.Click += (s, e) => { txtSearch.Clear(); LoadUserList(); ResetInput(); };

            // [THÊM MỚI] Nút Soi Mật Khẩu Cả Bảng
            Button btnSoiBang = new Button() { Text = "👁 Soi bảng", Location = new Point(590, 23), Width = 150, Height = 30, BackColor = Color.White, Cursor = Cursors.Hand };

            // Sự kiện nhấn giữ: Hiện pass thật cho toàn bộ bảng
            btnSoiBang.MouseDown += (s, e) => {
                foreach (DataGridViewRow row in dgvUser.Rows)
                {
                    if (row.Tag != null) row.Cells[1].Value = row.Tag.ToString();
                }
            };

            // Sự kiện thả chuột: Che lại
            btnSoiBang.MouseUp += (s, e) => {
                foreach (DataGridViewRow row in dgvUser.Rows)
                {
                    row.Cells[1].Value = "******";
                }
            };

            // Sự kiện chuột rời đi: Che lại (cho chắc ăn)
            btnSoiBang.MouseLeave += (s, e) => {
                foreach (DataGridViewRow row in dgvUser.Rows)
                {
                    row.Cells[1].Value = "******";
                }
            };

            grpSearch.Controls.AddRange(new Control[] { new Label() { Text = "Tìm tên:", Location = new Point(20, 28), AutoSize = true }, txtSearch, btnReload, btnSoiBang });
            pnlTop.Controls.Add(grpSearch);


            // --- B. KHUNG NHẬP LIỆU (Chia 2 cột) ---
            // Cấu hình tọa độ
            int col1_LabelX = 150;  // Nhãn cột trái
            int col1_TextX = 280;   // Ô nhập cột trái

            int col2_LabelX = 600;  // Nhãn cột phải
            int col2_TextX = 700;   // Ô nhập cột phải

            int row1_Y = 100;
            int row2_Y = 150;
            int row3_Y = 200; // Dành cho nhập lại mật khẩu

            // --- DÒNG 1: User (Trái) - Họ tên (Phải) ---
            pnlTop.Controls.Add(new Label() { Text = "Tài khoản (User):", Location = new Point(col1_LabelX, row1_Y + 3), AutoSize = true, Font = new Font("Arial", 11) });
            txtUser = new TextBox() { Location = new Point(col1_TextX, row1_Y), Width = 250, Font = new Font("Arial", 11) };

            pnlTop.Controls.Add(new Label() { Text = "Họ và tên:", Location = new Point(col2_LabelX, row1_Y + 3), AutoSize = true, Font = new Font("Arial", 11) });
            txtName = new TextBox() { Location = new Point(col2_TextX, row1_Y), Width = 250, Font = new Font("Arial", 11) };

            // --- DÒNG 2: Mật khẩu (Trái) - Quyền (Phải) ---
            pnlTop.Controls.Add(new Label() { Text = "Mật khẩu:", Location = new Point(col1_LabelX, row2_Y + 3), AutoSize = true, Font = new Font("Arial", 11) });
            txtPass = new TextBox() { Location = new Point(col1_TextX, row2_Y), Width = 250, Font = new Font("Arial", 11), PasswordChar = '*' };

            // --- [THÊM MỚI] NÚT MẮT THẦN CHO Ô NHẬP LIỆU ---
            Button btnShowPass = new Button();
            btnShowPass.Text = "👁"; // Icon con mắt
            btnShowPass.Size = new Size(40, 26);
            btnShowPass.Location = new Point(col1_TextX + 255, row2_Y - 1); // Đặt ngay cạnh ô mật khẩu
            btnShowPass.Cursor = Cursors.Hand;
            btnShowPass.BackColor = Color.White;

            // Logic: Nhấn giữ -> Hiện, Thả ra -> Ẩn
            btnShowPass.MouseDown += (s, e) => {
                txtPass.PasswordChar = '\0'; // Hiện
                txtPassVerify.PasswordChar = '\0'; // Hiện luôn ô xác nhận
            };
            btnShowPass.MouseUp += (s, e) => {
                txtPass.PasswordChar = '*'; // Ẩn
                txtPassVerify.PasswordChar = '*'; // Ẩn
            };
            // Thêm MouseLeave để lỡ kéo chuột ra ngoài thì nó cũng tự che lại
            btnShowPass.MouseLeave += (s, e) => {
                txtPass.PasswordChar = '*';
                txtPassVerify.PasswordChar = '*';
            };
            pnlTop.Controls.Add(btnShowPass);
            // ------------------------------------------------

            pnlTop.Controls.Add(new Label() { Text = "Chức vụ:", Location = new Point(col2_LabelX, row2_Y + 3), AutoSize = true, Font = new Font("Arial", 11) });
            cboRole = new ComboBox() { Location = new Point(col2_TextX, row2_Y), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 11) };
            cboRole.Items.AddRange(new string[] { "Staff", "Admin" });
            cboRole.SelectedIndex = 0;

            // --- DÒNG 3: Nhập lại mật khẩu (Trái) ---
            // Label nhập lại mật khẩu để màu đỏ cho chú ý
            pnlTop.Controls.Add(new Label() { Text = "Nhập lại M.Khẩu:", Location = new Point(col1_LabelX, row3_Y + 3), AutoSize = true, ForeColor = Color.Red, Font = new Font("Arial", 11) });
            txtPassVerify = new TextBox() { Location = new Point(col1_TextX, row3_Y), Width = 250, Font = new Font("Arial", 11), PasswordChar = '*' };


            // --- C. CÁC NÚT BẤM (Căn giữa Form) ---
            int btnY = 250;
            int btnW = 120;
            int btnH = 40;
            int space = 20; // Khoảng cách giữa các nút
            int startX = 280;

            Button btnAdd = new Button() { Text = "THÊM", Location = new Point(startX, btnY), BackColor = Color.Teal, ForeColor = Color.White, Width = btnW, Height = btnH, Font = new Font("Arial", 10, FontStyle.Bold) };
            btnAdd.Click += BtnAdd_Click;

            Button btnEdit = new Button() { Text = "SỬA", Location = new Point(startX + btnW + space, btnY), BackColor = Color.Orange, ForeColor = Color.White, Width = btnW, Height = btnH, Font = new Font("Arial", 10, FontStyle.Bold) };
            btnEdit.Click += BtnEdit_Click;

            Button btnDel = new Button() { Text = "XÓA", Location = new Point(startX + (btnW + space) * 2, btnY), BackColor = Color.Red, ForeColor = Color.White, Width = btnW, Height = btnH, Font = new Font("Arial", 10, FontStyle.Bold) };
            btnDel.Click += BtnDel_Click;

            Button btnReset = new Button() { Text = "LÀM MỚI", Location = new Point(startX + (btnW + space) * 3, btnY), BackColor = Color.Purple, ForeColor = Color.White, Width = btnW, Height = btnH, Font = new Font("Arial", 10, FontStyle.Bold) };
            btnReset.Click += (s, e) => ResetInput();

            // Add controls vào Panel
            pnlTop.Controls.AddRange(new Control[] { txtUser, txtPass, txtPassVerify, txtName, cboRole, btnAdd, btnEdit, btnDel, btnReset });

            // --- 2. BẢNG DỮ LIỆU ---
            dgvUser = new DataGridView();
            dgvUser.Dock = DockStyle.Fill;
            dgvUser.BackgroundColor = Color.White;
            dgvUser.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvUser.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvUser.ReadOnly = true;
            dgvUser.RowTemplate.Height = 40; // Tăng chiều cao dòng cho dễ nhìn
            dgvUser.ColumnHeadersHeight = 40;
            dgvUser.DefaultCellStyle.Font = new Font("Arial", 11);
            dgvUser.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 11, FontStyle.Bold);

            dgvUser.Columns.Add("c1", "Tài khoản (User)");
            dgvUser.Columns.Add("c2", "Mật khẩu");
            dgvUser.Columns.Add("c3", "Họ tên");
            dgvUser.Columns.Add("c4", "Chức vụ");

            dgvUser.CellClick += DgvUser_CellClick;

            // --- 3. KẾT HỢP ---
            tab.Controls.Add(dgvUser);
            tab.Controls.Add(pnlTop);
            dgvUser.BringToFront(); // Để Grid ở dưới, Panel ở trên (Dock Top)

            LoadUserList();
        }

        void LoadUserList(string keyword = "")
        {
            try
            {
                dgvUser.Rows.Clear();

                // 1. Tạo câu truy vấn (chưa chạy xuống DB ngay)
                var query = db.Users.AsQueryable();

                // 2. Nếu có từ khóa thì thêm điều kiện Where vào câu truy vấn
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(u => u.Username.Contains(keyword) || u.FullName.Contains(keyword));
                }

                // 3. Lúc này mới thực thi lấy dữ liệu về (Tối ưu hơn)
                var list = query.ToList();

                // Tự động tạo Admin nếu rỗng (chỉ check khi không search)
                if (list.Count == 0 && string.IsNullOrEmpty(keyword))
                {
                    if (!db.Users.Any()) // Check kỹ lại trong DB
                    {
                        db.Users.Add(new User { Username = "admin", Password = SecurityHelper.Encrypt("123"), FullName = "Quản trị viên", Role = "Admin" });
                        db.SaveChanges();
                        list = db.Users.ToList();
                    }
                }

                foreach (var u in list)
                {
                    // Hiển thị mật khẩu dạng ẩn (******) để bảo mật, 
                    // ta lưu mật khẩu thật vào thuộc tính Tag của dòng để dùng khi cần.
                    int rowIndex = dgvUser.Rows.Add(u.Username, "******", u.FullName, u.Role);
                    dgvUser.Rows[rowIndex].Tag = SecurityHelper.Decrypt(u.Password);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void DgvUser_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dgvUser.Rows.Count)
            {
                var row = dgvUser.Rows[e.RowIndex];

                txtUser.Text = row.Cells[0].Value?.ToString();

                // Lấy pass thật từ Tag đã lưu ở hàm LoadUserList
                string realPass = row.Tag?.ToString() ?? "";
                txtPass.Text = realPass;
                txtPassVerify.Text = realPass; // Tự động điền luôn ô xác nhận

                txtName.Text = row.Cells[2].Value?.ToString();
                cboRole.Text = row.Cells[3].Value?.ToString();

                txtUser.Enabled = false;
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            // --- XỬ LÝ CHẾ ĐỘ THÊM MỚI KHI ĐANG CHỌN DÒNG CŨ ---
            // Nếu ô User đang bị khóa (tức là đang chọn dòng cũ), thì tự động Reset để nhập mới
            if (txtUser.Enabled == false)
            {
                ResetInput(); // Gọi hàm làm mới để mở khóa ô User
                MessageBox.Show("Đã chuyển sang chế độ Thêm mới. Vui lòng nhập thông tin và bấm Thêm lần nữa.");
                return; // Dừng lại để bạn nhập liệu
            }

            // 1. Kiểm tra dữ liệu nhập
            if (string.IsNullOrEmpty(txtUser.Text) || string.IsNullOrEmpty(txtPass.Text))
            {
                MessageBox.Show("Vui lòng nhập đủ thông tin User và Mật khẩu!");
                return;
            }

            // 2. CHECK PASS VERIFY (Quan trọng)
            if (txtPass.Text != txtPassVerify.Text)
            {
                MessageBox.Show("Mật khẩu nhập lại không khớp!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassVerify.Focus();
                return;
            }

            if (db.Users.Find(txtUser.Text) != null)
            {
                MessageBox.Show("Tên đăng nhập đã tồn tại!");
                return;
            }

            db.Users.Add(new User { Username = txtUser.Text, Password = SecurityHelper.Encrypt(txtPass.Text), FullName = txtName.Text, Role = cboRole.Text });
            db.SaveChanges();

            MessageBox.Show("Thêm nhân viên thành công!"); // Thông báo thành công
            LoadUserList();
            ResetInput();
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            // Check Pass Verify khi sửa
            if (txtPass.Text != txtPassVerify.Text)
            {
                MessageBox.Show("Mật khẩu nhập lại không khớp!");
                return;
            }

            var u = db.Users.Find(txtUser.Text);
            if (u != null)
            {
                u.Password = SecurityHelper.Encrypt(txtPass.Text);
                u.FullName = txtName.Text;
                u.Role = cboRole.Text;
                db.SaveChanges();

                MessageBox.Show("Cập nhật thành công!");
                LoadUserList();
                ResetInput();
            }
        }

        private void BtnDel_Click(object sender, EventArgs e)
        {
            if (txtUser.Text == "admin") { MessageBox.Show("Không thể xóa Admin!"); return; }

            var u = db.Users.Find(txtUser.Text);
            if (u != null)
            {
                db.Users.Remove(u);
                db.SaveChanges();
                LoadUserList();
                ResetInput();
            }
        }

        void ResetInput()
        {
            txtUser.Clear();
            txtPass.Clear();
            txtPassVerify.Clear(); // <--- Bổ sung cái này
            txtName.Clear();

            cboRole.SelectedIndex = 0; // Reset combobox về mặc định
            txtUser.Enabled = true;
            txtUser.Focus();
        }

        void SetupTabLog(TabPage tab)
        {
            dgvLog = new DataGridView() { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true, BackgroundColor = Color.White };
            tab.Controls.Add(dgvLog);
            try { dgvLog.DataSource = db.LoginLogs.OrderByDescending(x => x.ThoiGianLogin).Take(50).ToList(); } catch { }
        }
    }
}