using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace QuanLyGame_Final
{
    public partial class FormAdmin : Form
    {
        private DataGridView dgvGame;
        private TextBox txtTen, txtGia, txtAnh;
        private ComboBox cboDanhMuc;
        private PictureBox pbPreview;

        GameContext db = new GameContext();

        // 1. Biến lưu người đang đăng nhập
        private User _currentAdmin;

        // 2. Constructor
        public FormAdmin(User admin)
        {
            _currentAdmin = admin;
            SetupAdminUI();
            LoadData();
        }

        private void SetupAdminUI()
        {
            this.Text = "HỆ THỐNG QUẢN LÝ GAME (ADMIN ONLY)";
            this.Size = new Size(1100, 650);
            this.StartPosition = FormStartPosition.CenterScreen;

            Panel pnlLeft = new Panel() { Dock = DockStyle.Left, Width = 380, BackColor = Color.WhiteSmoke, Padding = new Padding(20) };
            Panel pnlRight = new Panel() { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // --- CỘT TRÁI: NHẬP LIỆU ---
            Label lblTitle = new Label() { Text = "THÔNG TIN GAME", Font = new Font("Arial", 16, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true, ForeColor = Color.DarkBlue };

            Label l1 = new Label() { Text = "Tên Game:", Location = new Point(20, 70), Font = new Font("Arial", 10) };
            txtTen = new TextBox() { Location = new Point(20, 95), Width = 320, Font = new Font("Arial", 10) };

            Label l2 = new Label() { Text = "Giá Tiền:", Location = new Point(20, 130), Font = new Font("Arial", 10) };
            txtGia = new TextBox() { Location = new Point(20, 155), Width = 320, Font = new Font("Arial", 10) };

            // --- [QUAN TRỌNG] GÁN CÁC SỰ KIỆN XỬ LÝ "FREE" ---
            txtGia.KeyPress += TxtGia_KeyPress; // Cho phép nhập
            txtGia.Leave += TxtGia_Leave;       // Rời chuột -> Tự đổi 0 thành Free
            txtGia.Enter += TxtGia_Enter;       // Vào chuột -> Tự đổi Free thành 0
            // ------------------------------------------------

            Label l3 = new Label() { Text = "Danh Mục:", Location = new Point(20, 190), Font = new Font("Arial", 10) };
            cboDanhMuc = new ComboBox() { Location = new Point(20, 215), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 10) };

            Label l4 = new Label() { Text = "Ảnh Bìa (Poster):", Location = new Point(20, 250), Font = new Font("Arial", 10) };
            txtAnh = new TextBox() { Location = new Point(20, 275), Width = 220, ReadOnly = true, Font = new Font("Arial", 10) };
            Button btnChon = new Button() { Text = "Chọn...", Location = new Point(250, 274), Width = 90, BackColor = Color.LightGray };
            btnChon.Click += (s, e) => {
                OpenFileDialog open = new OpenFileDialog();
                open.Filter = "Image Files|*.jpg;*.png;*.jpeg";
                if (open.ShowDialog() == DialogResult.OK)
                {
                    txtAnh.Text = open.FileName;
                    pbPreview.Image = Image.FromFile(open.FileName);
                }
            };

            pbPreview = new PictureBox() { Location = new Point(20, 310), Size = new Size(320, 180), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };

            // NÚT BẤM CHỨC NĂNG
            Button btnThem = new Button() { Text = "THÊM MỚI", BackColor = Color.Teal, ForeColor = Color.White, Location = new Point(20, 510), Size = new Size(100, 45), Font = new Font("Arial", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            Button btnSua = new Button() { Text = "CẬP NHẬT", BackColor = Color.Orange, ForeColor = Color.White, Location = new Point(130, 510), Size = new Size(100, 45), Font = new Font("Arial", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            Button btnXoa = new Button() { Text = "XÓA", BackColor = Color.Red, ForeColor = Color.White, Location = new Point(240, 510), Size = new Size(100, 45), Font = new Font("Arial", 9, FontStyle.Bold), Cursor = Cursors.Hand };

            btnThem.Click += BtnThem_Click;
            btnSua.Click += BtnSua_Click;
            btnXoa.Click += BtnXoa_Click;

            pnlLeft.Controls.AddRange(new Control[] { lblTitle, l1, txtTen, l2, txtGia, l3, cboDanhMuc, l4, txtAnh, btnChon, pbPreview, btnThem, btnSua, btnXoa });

            // --- CỘT PHẢI: GRIDVIEW ---
            dgvGame = new DataGridView();
            dgvGame.Dock = DockStyle.Fill;
            dgvGame.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvGame.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvGame.MultiSelect = false;
            dgvGame.ReadOnly = true;
            dgvGame.BackgroundColor = Color.White;
            dgvGame.CellClick += DgvGame_CellClick;

            pnlRight.Controls.Add(dgvGame);

            this.Controls.Add(pnlRight);
            this.Controls.Add(pnlLeft);
        }

        // --- CÁC HÀM XỬ LÝ THÔNG MINH CHO Ô GIÁ (MỚI) ---

        // 1. Khi nhập xong và rời chuột ra ngoài
        private void TxtGia_Leave(object sender, EventArgs e)
        {
            // Nếu để trống hoặc gõ số 0 -> Tự biến thành chữ "Free" cho đẹp
            if (string.IsNullOrEmpty(txtGia.Text) || txtGia.Text.Trim() == "0")
            {
                txtGia.Text = "Free";
                txtGia.ForeColor = Color.Green; // Đổi màu xanh cho nổi
            }
        }

        // 2. Khi bấm chuột vào để sửa
        private void TxtGia_Enter(object sender, EventArgs e)
        {
            // Nếu đang là chữ "Free" -> Đổi lại thành số 0 hoặc xóa trắng để người dùng nhập
            if (txtGia.Text == "Free")
            {
                txtGia.Text = "0"; // Hoặc để "" nếu bạn muốn xóa trắng
                txtGia.ForeColor = Color.Black; // Trả về màu đen
            }
        }

        // 3. Cho phép nhập cả chữ (để gõ chữ Free) và số
        private void TxtGia_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Tôi đã bỏ chặn phím ở đây để bạn có thể gõ chữ "Free"
            // Nếu bạn muốn chặn các ký tự đặc biệt (@, #, $) thì có thể thêm code, 
            // nhưng hiện tại tôi để mở để bạn thoải mái.
        }

        // 4. HÀM QUAN TRỌNG: Chuyển đổi chữ "Free" thành số 0 để lưu vào DB
        private decimal LayGiaTienTuOInput()
        {
            string input = txtGia.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(input)) return 0;
            if (input == "free" || input == "mien phi") return 0;

            // Nếu là số thì Parse, nếu lỗi (do nhập bậy) thì trả về -1 để báo lỗi
            decimal result;
            if (decimal.TryParse(input, out result))
            {
                return result;
            }
            return -1; // Trả về -1 nghĩa là nhập sai định dạng
        }

        // --------------------------------------------------

        void LoadData()
        {
            try
            {
                var danhSachTheLoai = new List<string>() {
                    "Hành Động (Action)", "Nhập Vai (RPG)", "Bắn Súng (FPS/TPS)",
                    "Chiến Thuật (Strategy)", "Thể Thao (Sports)", "Đua Xe (Racing)",
                    "Sinh Tồn (Survival)", "Kinh Dị (Horror)", "Thế Giới Mở (Open World)",
                    "Đối Kháng (Fighting)", "Giải Đố (Puzzle)", "MOBA", "Indie Game"
                };

                bool coThayDoi = false;
                foreach (var ten in danhSachTheLoai)
                {
                    if (!db.Categories.Any(c => c.TenDanhMuc == ten))
                    {
                        db.Categories.Add(new Category() { TenDanhMuc = ten });
                        coThayDoi = true;
                    }
                }

                if (coThayDoi) db.SaveChanges();

                cboDanhMuc.DataSource = db.Categories.ToList();
                cboDanhMuc.DisplayMember = "TenDanhMuc";
                cboDanhMuc.ValueMember = "Id";

                dgvGame.DataSource = db.Games.Select(g => new {
                    Id = g.Id,
                    TenGame = g.TenGame,
                    GiaTien = g.GiaTien,
                    DanhMuc = g.Category.TenDanhMuc,
                    HinhAnh = g.HinhAnh
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi load dữ liệu: " + ex.Message);
            }
        }

        private bool KiemTraMatKhauAdmin()
        {
            Form frmPass = new Form() { Width = 400, Height = 180, Text = "Yêu cầu bảo mật", StartPosition = FormStartPosition.CenterScreen, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };

            Label lbl = new Label() { Text = $"Xin chào {_currentAdmin.FullName}.\nNhập mật khẩu để xác nhận:", Left = 20, Top = 20, AutoSize = true, Font = new Font("Arial", 10) };
            TextBox txt = new TextBox() { Left = 20, Top = 60, Width = 340, PasswordChar = '*', Font = new Font("Arial", 10) };
            Button btnOk = new Button() { Text = "Xác nhận", Left = 180, Top = 100, DialogResult = DialogResult.OK, Width = 90, BackColor = Color.Teal, ForeColor = Color.White };
            Button btnCancel = new Button() { Text = "Hủy", Left = 280, Top = 100, DialogResult = DialogResult.Cancel, Width = 80 };

            frmPass.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
            frmPass.AcceptButton = btnOk;

            if (frmPass.ShowDialog() == DialogResult.OK)
            {
                if (txt.Text == SecurityHelper.Decrypt(_currentAdmin.Password)) return true;
                else
                {
                    MessageBox.Show("Mật khẩu không đúng! Thao tác bị từ chối.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            return false;
        }

        // --- NÚT THÊM ---
        private void BtnThem_Click(object sender, EventArgs e)
        {
            if (!KiemTraMatKhauAdmin()) return;

            try
            {
                string tenGame = txtTen.Text.Trim();
                if (string.IsNullOrEmpty(tenGame)) { MessageBox.Show("Tên game không được để trống!"); return; }

                if (db.Games.Any(x => x.TenGame.ToLower() == tenGame.ToLower()))
                {
                    MessageBox.Show($"Game '{tenGame}' đã có rồi!", "Báo trùng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // SỬ DỤNG HÀM XỬ LÝ GIÁ MỚI
                decimal giaTien = LayGiaTienTuOInput();
                if (giaTien < 0) { MessageBox.Show("Giá tiền không hợp lệ! Vui lòng nhập số hoặc chữ 'Free'."); return; }

                Game g = new Game()
                {
                    TenGame = tenGame,
                    GiaTien = giaTien, // Dùng biến đã xử lý
                    CategoryId = (int)cboDanhMuc.SelectedValue,
                    HinhAnh = txtAnh.Text
                };
                db.Games.Add(g);
                db.SaveChanges();
                MessageBox.Show("Đã thêm thành công!");
                LoadData();
                ResetForm();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi thêm: " + ex.Message); }
        }

        // --- NÚT SỬA ---
        private void BtnSua_Click(object sender, EventArgs e)
        {
            if (dgvGame.CurrentRow == null) { MessageBox.Show("Chọn game cần sửa!"); return; }
            if (!KiemTraMatKhauAdmin()) return;

            try
            {
                int id = int.Parse(dgvGame.CurrentRow.Cells["Id"].Value.ToString());
                Game g = db.Games.Find(id);
                if (g != null)
                {
                    string tenMoi = txtTen.Text.Trim();
                    if (db.Games.Any(x => x.TenGame.ToLower() == tenMoi.ToLower() && x.Id != id))
                    {
                        MessageBox.Show($"Tên '{tenMoi}' bị trùng với game khác!", "Báo trùng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // SỬ DỤNG HÀM XỬ LÝ GIÁ MỚI
                    decimal giaTien = LayGiaTienTuOInput();
                    if (giaTien < 0) { MessageBox.Show("Giá tiền không hợp lệ! Vui lòng nhập số hoặc chữ 'Free'."); return; }

                    g.TenGame = tenMoi;
                    g.GiaTien = giaTien;
                    g.CategoryId = (int)cboDanhMuc.SelectedValue;
                    g.HinhAnh = txtAnh.Text;

                    db.SaveChanges();
                    MessageBox.Show("Cập nhật thành công!");
                    LoadData();
                    ResetForm();
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi sửa: " + ex.Message); }
        }

        // --- NÚT XÓA ---
        private void BtnXoa_Click(object sender, EventArgs e)
        {
            if (dgvGame.CurrentRow == null) { MessageBox.Show("Chọn game cần xóa!"); return; }
            if (MessageBox.Show("Bạn chắc chắn muốn xóa game này?", "Cảnh báo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) return;
            if (!KiemTraMatKhauAdmin()) return;

            try
            {
                int id = int.Parse(dgvGame.CurrentRow.Cells["Id"].Value.ToString());
                Game g = db.Games.Find(id);
                if (g != null)
                {
                    db.Games.Remove(g);
                    db.SaveChanges();
                    MessageBox.Show("Đã xóa!");
                    LoadData();
                    ResetForm();
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi xóa: " + ex.Message); }
        }

        private void FormAdmin_Load(object sender, EventArgs e)
        {
        }

        private void DgvGame_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = dgvGame.Rows[e.RowIndex];
                txtTen.Text = row.Cells["TenGame"].Value.ToString();

                // Hiển thị giá tiền thông minh
                decimal gia = decimal.Parse(row.Cells["GiaTien"].Value.ToString());
                if (gia == 0)
                {
                    txtGia.Text = "Free";
                    txtGia.ForeColor = Color.Green;
                }
                else
                {
                    txtGia.Text = gia.ToString("N0"); // 50,000
                    txtGia.ForeColor = Color.Black;
                }

                cboDanhMuc.Text = row.Cells["DanhMuc"].Value.ToString();

                string path = row.Cells["HinhAnh"].Value?.ToString();
                txtAnh.Text = path;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    pbPreview.Image = Image.FromFile(path);
                else
                    pbPreview.Image = null;
            }
        }

        void ResetForm()
        {
            txtTen.Clear();
            txtGia.Clear();
            txtGia.ForeColor = Color.Black; // Reset màu chữ
            txtAnh.Clear();
            pbPreview.Image = null;
        }
    }
}