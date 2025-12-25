using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Data.Entity;

namespace QuanLyGame_Final
{
    public partial class FormStore : Form
    {
        private FlowLayoutPanel pnlGames;
        private Label lblCartCount;

        // Dữ liệu
        private List<Game> _cart = new List<Game>();
        private List<Game> _allGames = new List<Game>();
        private User _currentUser; // Đây chính là User lấy từ FormUserMan -> FormLogin -> FormStore

        GameContext db = new GameContext();

        private TextBox txtSearch;
        private ComboBox cboCategory;

        public FormStore(User user)
        {
            _currentUser = user; // Nhận thông tin người dùng đã đăng nhập thành công
            SetupUI();
            LoadDataFromDB();
        }

        void SetupUI()
        {
            this.Text = $"STORE GAME - Xin chào: {_currentUser.FullName} ({_currentUser.Role})";
            this.WindowState = FormWindowState.Maximized;

            // --- 1. MENU CHỨC NĂNG ---
            Panel pnlMenu = new Panel() { Dock = DockStyle.Top, Height = 50, BackColor = Color.Navy };

            // Nút Đăng xuất
            Button btnLogout = new Button() { Text = "ĐĂNG XUẤT", Location = new Point(10, 8), Size = new Size(100, 34), BackColor = Color.Firebrick, ForeColor = Color.White, Font = new Font("Arial", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            btnLogout.Click += (s, e) => {
                if (MessageBox.Show("Bạn muốn đăng xuất?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    this.DialogResult = DialogResult.OK; // Trả về OK để Program.cs biết và hiện lại Login
                    this.Close();
                }
            };
            pnlMenu.Controls.Add(btnLogout);

            int x = 120;
            Button btnHome = CreateMenuButton("TRANG CHỦ", x, pnlMenu); x += 120;
            btnHome.Click += (s, e) => { txtSearch.Text = ""; cboCategory.SelectedIndex = 0; };

            Button btnHistory = CreateMenuButton("LỊCH SỬ MUA", x, pnlMenu); x += 120;
            btnHistory.Click += (s, e) => new FormHistory().ShowDialog();

            // Chỉ hiện nút Quản lý nếu là Admin (User này khớp với FormUserMan tạo ra)
            // Trong file FormStore.cs (đoạn nút btnAdmin)

            if (_currentUser.Role == "Admin")
            {
                // 1. Nút Quản lý Game (Giữ nguyên)
                Button btnGameMan = CreateMenuButton("QUẢN LÝ GAME", x, pnlMenu);
                btnGameMan.BackColor = Color.DarkRed;
                btnGameMan.Click += (s, e) => {
                    new FormAdmin(_currentUser).ShowDialog(); // Mở form quản lý game
                    LoadDataFromDB(); // Load lại store sau khi chỉnh sửa
                };
                x += 120; // Dịch sang phải để đặt nút tiếp theo

                // 2. Nút Quản lý Nhân viên (THÊM MỚI)
                Button btnUserMan = CreateMenuButton("QL NHÂN VIÊN", x, pnlMenu);
                btnUserMan.BackColor = Color.Purple; // Màu tím để phân biệt
                btnUserMan.Click += (s, e) => {
                    new FormUserMan().ShowDialog(); // Mở form quản lý nhân viên
                };
                x += 120;
            }

            // Nút Giỏ hàng
            Button btnCart = new Button() { Text = "Giỏ hàng (0)", Dock = DockStyle.Right, Width = 150, BackColor = Color.Orange, Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.Black };
            btnCart.Click += BtnCart_Click;

            // Label ảo để trigger sự kiện cập nhật text cho nút giỏ hàng (Kỹ thuật binding đơn giản)
            lblCartCount = new Label() { Visible = false };
            lblCartCount.TextChanged += (s, e) => btnCart.Text = $"Giỏ hàng ({_cart.Count})";

            pnlMenu.Controls.Add(btnCart);

            // --- 2. THANH TÌM KIẾM ---
            Panel pnlSearch = new Panel() { Dock = DockStyle.Top, Height = 60, BackColor = Color.WhiteSmoke, Padding = new Padding(20, 15, 20, 10) };

            Label lblSearch = new Label() { Text = "Tìm tên Game:", Location = new Point(20, 20), AutoSize = true, Font = new Font("Arial", 10) };
            txtSearch = new TextBox() { Location = new Point(130, 18), Width = 250, Font = new Font("Arial", 11) };
            // Cải tiến: Nhập "Free" sẽ tìm game miễn phí
            txtSearch.TextChanged += (s, e) => FilterGames();

            Label lblCat = new Label() { Text = "Lọc Thể loại:", Location = new Point(420, 20), AutoSize = true, Font = new Font("Arial", 10) };
            cboCategory = new ComboBox() { Location = new Point(520, 18), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 10) };
            cboCategory.SelectedIndexChanged += (s, e) => FilterGames();

            pnlSearch.Controls.AddRange(new Control[] { lblSearch, txtSearch, lblCat, cboCategory });

            // --- 3. KHUNG HIỂN THỊ GAME ---
            pnlGames = new FlowLayoutPanel() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(20) };

            this.Controls.Add(pnlGames);
            this.Controls.Add(pnlSearch);
            this.Controls.Add(pnlMenu);
        }

        Button CreateMenuButton(string text, int x, Panel parent)
        {
            Button btn = new Button() { Text = text, Location = new Point(x, 8), Size = new Size(110, 34), BackColor = Color.DodgerBlue, ForeColor = Color.White, Font = new Font("Arial", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            parent.Controls.Add(btn);
            return btn;
        }

        void LoadDataFromDB()
        {
            try
            {
                var listCat = db.Categories.ToList();
                listCat.Insert(0, new Category { Id = 0, TenDanhMuc = "-- Tất cả thể loại --" });

                cboCategory.DataSource = listCat;
                cboCategory.DisplayMember = "TenDanhMuc";
                cboCategory.ValueMember = "Id";

                _allGames = db.Games.Include("Category").ToList();
                FilterGames();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        // --- HÀM HELPER: Xử lý hiển thị giá tiền ---
        // Nếu giá = 0 trả về "Free", ngược lại trả về định dạng tiền tệ
        private string FormatPrice(decimal price)
        {
            return price == 0 ? "Free" : price.ToString("N0") + " đ";
        }

        void FilterGames()
        {
            string keyword = txtSearch.Text.ToLower().Trim();
            int catId = 0;

            if (cboCategory.SelectedValue != null && cboCategory.SelectedValue is int)
            {
                catId = (int)cboCategory.SelectedValue;
            }

            var result = _allGames.Where(g =>
            {
                // 1. Điều kiện Danh mục
                bool matchCategory = (catId == 0 || g.CategoryId == catId);

                // 2. Điều kiện Từ khóa (Logic mới)
                bool matchKeyword = false;

                if (keyword == "free")
                {
                    // NẾU gõ đúng chữ "free" -> CHỈ lấy game giá 0 đồng (Bỏ qua tên)
                    matchKeyword = (g.GiaTien == 0);
                }
                else
                {
                    // NẾU gõ từ khác -> Tìm theo tên như bình thường
                    matchKeyword = string.IsNullOrEmpty(keyword) || g.TenGame.ToLower().Contains(keyword);
                }

                return matchCategory && matchKeyword;
            }).ToList();

            // --- ĐOẠN DƯỚI GIỮ NGUYÊN ---
            pnlGames.Controls.Clear();

            if (result.Count == 0)
            {
                Label lblEmpty = new Label() { Text = "Không tìm thấy game nào!", AutoSize = true, Font = new Font("Arial", 12, FontStyle.Italic), ForeColor = Color.Gray, Padding = new Padding(20) };
                pnlGames.Controls.Add(lblEmpty);
                return;
            }

            foreach (var g in result)
            {
                var card = new GameCard(g);

                // Đảm bảo hiển thị đúng giá tiền
                // (Lưu ý: GameCard của bạn đã có logic tự xử lý hiển thị "Free" trong constructor rồi nên không cần set lại text ở đây nữa)

                card.OnBuyClick += (game) => {
                    _cart.Add(game);
                    lblCartCount.Text = _cart.Count.ToString();
                    MessageBox.Show($"Đã thêm: {game.TenGame}");
                };
                pnlGames.Controls.Add(card);
            }
        }

        private void BtnCart_Click(object sender, EventArgs e)
        {
            if (_cart.Count == 0)
            {
                MessageBox.Show("Giỏ hàng đang trống!");
                return;
            }

            // Mở FormBill
            FormBill frm = new FormBill(_cart, _currentUser);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                _cart.Clear();
                lblCartCount.Text = "0"; // Reset về 0
            }
        }
    }
}