using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace QuanLyGame_Final
{
    public class GameCard : UserControl
    {
        private Game _game;

        // Sự kiện báo ra ngoài
        public event Action<Game> OnBuyClick;

        public GameCard(Game game)
        {
            _game = game;
            SetupUI();

            // Thêm hiệu ứng hover chuột cho đẹp (Optional)
            this.MouseEnter += (s, e) => { this.BackColor = Color.AliceBlue; this.BorderStyle = BorderStyle.Fixed3D; };
            this.MouseLeave += (s, e) => { this.BackColor = Color.White; this.BorderStyle = BorderStyle.FixedSingle; };
        }

        private void SetupUI()
        {
            // 1. Cài đặt thẻ
            this.Size = new Size(200, 290); // Tăng chiều cao một chút cho thoáng
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Margin = new Padding(15);
            this.Cursor = Cursors.Hand; // Rê chuột vào hiện bàn tay

            // 2. Ảnh game (PictureBox)
            PictureBox pb = new PictureBox();
            pb.Size = new Size(180, 150);
            pb.Location = new Point(10, 10);
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.BorderStyle = BorderStyle.None;
            pb.BackColor = Color.WhiteSmoke;

            // --- XỬ LÝ ẢNH AN TOÀN ---
            // Dùng FileStream để không bị khóa file ảnh trong Windows
            if (!string.IsNullOrEmpty(_game.HinhAnh) && File.Exists(_game.HinhAnh))
            {
                try
                {
                    using (FileStream fs = new FileStream(_game.HinhAnh, FileMode.Open, FileAccess.Read))
                    {
                        pb.Image = Image.FromStream(fs);
                    }
                }
                catch
                {
                    // Nếu lỗi file ảnh hỏng thì hiện màu xám
                    pb.BackColor = Color.Gray;
                }
            }
            else
            {
                // Vẽ chữ "No Image" nếu không có ảnh
                pb.Paint += (s, e) => {
                    e.Graphics.DrawString("No Image", new Font("Arial", 10), Brushes.Gray, new PointF(55, 65));
                };
            }

            // 3. Tên Game (Label)
            Label lblName = new Label();
            lblName.Text = _game.TenGame;
            lblName.Font = new Font("Arial", 11, FontStyle.Bold);
            lblName.ForeColor = Color.DarkBlue;
            lblName.Location = new Point(5, 170); // Căn lề rộng hơn chút
            lblName.Size = new Size(190, 45); // Cho phép xuống dòng 2 hàng
            lblName.TextAlign = ContentAlignment.TopCenter;

            // 4. Giá tiền (Label) - LOGIC FREE Ở ĐÂY
            Label lblPrice = new Label();
            lblPrice.Font = new Font("Arial", 12, FontStyle.Bold);
            lblPrice.Location = new Point(10, 220);
            lblPrice.Size = new Size(180, 25);
            lblPrice.TextAlign = ContentAlignment.MiddleCenter;

            // --- CẬP NHẬT LOGIC GIÁ ---
            if (_game.GiaTien == 0)
            {
                lblPrice.Text = "Free";
                lblPrice.ForeColor = Color.Green; // Màu xanh lá
            }
            else
            {
                lblPrice.Text = _game.GiaTien.ToString("N0") + " đ";
                lblPrice.ForeColor = Color.Red;   // Màu đỏ
            }

            // 5. Nút Mua (Button)
            Button btnBuy = new Button();
            btnBuy.Text = "CHỌN MUA";
            btnBuy.BackColor = Color.DodgerBlue; // Đổi màu xanh dương cho hiện đại
            btnBuy.ForeColor = Color.White;
            btnBuy.Font = new Font("Arial", 10, FontStyle.Bold);
            btnBuy.Location = new Point(35, 250);
            btnBuy.Size = new Size(130, 32);
            btnBuy.FlatStyle = FlatStyle.Flat;
            btnBuy.FlatAppearance.BorderSize = 0;
            btnBuy.Cursor = Cursors.Hand;

            // Sự kiện Click
            btnBuy.Click += (sender, e) =>
            {
                OnBuyClick?.Invoke(_game);
            };

            // Click vào thẻ cũng tính là mua (Optional - tùy bạn chọn)
            // this.Click += (s, e) => OnBuyClick?.Invoke(_game);

            // Thêm vào Controls
            this.Controls.Add(pb);
            this.Controls.Add(lblName);
            this.Controls.Add(lblPrice);
            this.Controls.Add(btnBuy);
        }
    }
}