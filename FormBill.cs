using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using OfficeOpenXml; // Thư viện Excel
using OfficeOpenXml.Style; // Thư viện để tô màu, kẻ bảng

namespace QuanLyGame_Final
{
    public partial class FormBill : Form
    {
        private List<Game> _cart;
        private User _seller;
        private GameContext db = new GameContext();

        // Giao diện
        private DataGridView dgvBill;
        private Label lblTotal;
        private PictureBox pbQRCode;
        private Label lblHuongDan;
        private Button btnConfirm;

        // In ấn
        private PrintDocument printDocument1 = new PrintDocument();
        private PrintPreviewDialog printPreviewDialog1 = new PrintPreviewDialog();

        public FormBill(List<Game> cartItems, User seller)
        {
            _cart = cartItems;
            _seller = seller;

            // Cấu hình bản quyền EPPlus (Bắt buộc)
            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            SetupUI();
            LoadGridData();

            printDocument1.PrintPage += new PrintPageEventHandler(InNoiDungHoaDon);
        }

        void SetupUI()
        {
            this.Text = "THANH TOÁN ĐƠN HÀNG - ILF STORE";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            Label lblTitle = new Label() { Text = "HÓA ĐƠN CHI TIẾT", Font = new Font("Arial", 16, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            Label lblSub = new Label() { Text = $"(Nhân viên: {_seller.FullName})", Font = new Font("Arial", 9, FontStyle.Italic), ForeColor = Color.Gray, Location = new Point(20, 50), AutoSize = true };

            dgvBill = new DataGridView();
            dgvBill.Location = new Point(20, 80);
            dgvBill.Size = new Size(500, 300);
            dgvBill.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvBill.BackgroundColor = Color.WhiteSmoke;
            dgvBill.ReadOnly = true;
            dgvBill.RowHeadersVisible = false;

            DataGridViewButtonColumn btnXoa = new DataGridViewButtonColumn();
            btnXoa.Name = "colXoa"; btnXoa.HeaderText = "Thao tác"; btnXoa.Text = "Xóa"; btnXoa.UseColumnTextForButtonValue = true; btnXoa.Width = 80;
            dgvBill.Columns.Add(btnXoa);
            dgvBill.CellClick += DgvBill_CellClick;

            lblTotal = new Label() { Text = "TỔNG TIỀN: 0 đ", Font = new Font("Arial", 16, FontStyle.Bold), ForeColor = Color.Red, Location = new Point(20, 400), AutoSize = true };

            GroupBox grpMethod = new GroupBox() { Text = "Hình thức thanh toán", Location = new Point(540, 80), Size = new Size(320, 300) };
            RadioButton rdoCash = new RadioButton() { Text = "Tiền mặt", Location = new Point(20, 30), Checked = true };
            RadioButton rdoMomo = new RadioButton() { Text = "Ví Momo", Location = new Point(20, 60) };
            RadioButton rdoZalo = new RadioButton() { Text = "ZaloPay", Location = new Point(150, 60) };
            RadioButton rdoBank = new RadioButton() { Text = "Chuyển khoản", Location = new Point(20, 90) };

            pbQRCode = new PictureBox() { Location = new Point(35, 150), Size = new Size(250, 140), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, Visible = false };
            lblHuongDan = new Label() { Text = "", Location = new Point(35, 125), Visible = false, ForeColor = Color.Blue, AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) };

            EventHandler onMethodChange = (s, e) => {
                pbQRCode.Visible = false; lblHuongDan.Visible = false;
                if (rdoMomo.Checked) { HienThiAnhQR("qr_momo.jpg"); lblHuongDan.Text = "Mở MoMo quét mã:"; }
                else if (rdoZalo.Checked) { HienThiAnhQR("qr_zalo.jpg"); lblHuongDan.Text = "Mở ZaloPay quét mã:"; }
                else if (rdoBank.Checked) { HienThiAnhQR("qr_bank.jpg"); lblHuongDan.Text = "App Ngân hàng:"; }
            };
            rdoCash.CheckedChanged += onMethodChange; rdoMomo.CheckedChanged += onMethodChange; rdoZalo.CheckedChanged += onMethodChange; rdoBank.CheckedChanged += onMethodChange;
            grpMethod.Controls.AddRange(new Control[] { rdoCash, rdoMomo, rdoZalo, rdoBank, pbQRCode, lblHuongDan });

            // --- NÚT THANH TOÁN DUY NHẤT (Sạch sẽ giao diện) ---
            btnConfirm = new Button() { Text = "THANH TOÁN (LƯU)", BackColor = Color.ForestGreen, ForeColor = Color.White, Location = new Point(540, 400), Size = new Size(320, 60), Font = new Font("Arial", 14, FontStyle.Bold), Cursor = Cursors.Hand };
            btnConfirm.Click += BtnConfirm_Click;

            this.Controls.AddRange(new Control[] { lblTitle, lblSub, dgvBill, lblTotal, grpMethod, btnConfirm });
        }

        // --- HÀM XỬ LÝ THANH TOÁN & HỘP THOẠI LỰA CHỌN ---
        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (_cart.Count == 0) { MessageBox.Show("Giỏ hàng trống!"); return; }

            decimal total = _cart.Sum(x => x.GiaTien);

            // 1. Lưu SQL trước (Chỉ lưu 1 lần duy nhất)
            try
            {
                Order order = new Order() { NgayMua = DateTime.Now, TongTien = total, NguoiBan = _seller.FullName, OrderDetails = new List<OrderDetail>() };
                foreach (var item in _cart) order.OrderDetails.Add(new OrderDetail() { TenGame = item.TenGame, GiaTien = item.GiaTien });
                db.Orders.Add(order);
                db.SaveChanges();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi lưu SQL: " + ex.Message); return; }

            // 2. TẠO HỘP THOẠI MENU (Không dùng MessageBox thường)
            Form luaChon = new Form()
            {
                Width = 550,
                Height = 280,
                Text = "Thanh toán thành công!",
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ControlBox = false // [QUAN TRỌNG] Tắt nút X góc trên để bắt buộc bấm nút Đóng bên dưới
            };

            Label lblHoi = new Label() { Text = "Đơn hàng đã lưu. Chọn các thao tác bên dưới:", Location = new Point(20, 20), AutoSize = true, Font = new Font("Arial", 11) };

            // --- CÁC NÚT CHỨC NĂNG ---
            Button btnIn = new Button() { Text = "🖨 In Hóa Đơn", Location = new Point(20, 60), Width = 150, Height = 50, BackColor = Color.DarkBlue, ForeColor = Color.White, Cursor = Cursors.Hand, Font = new Font("Arial", 10, FontStyle.Bold) };
            Button btnLuuNhanh = new Button() { Text = "💾 Lưu Auto", Location = new Point(190, 60), Width = 150, Height = 50, BackColor = Color.Teal, ForeColor = Color.White, Cursor = Cursors.Hand, Font = new Font("Arial", 10, FontStyle.Bold) };
            Button btnCaHai = new Button() { Text = "🚀 In & Lưu", Location = new Point(360, 60), Width = 150, Height = 50, BackColor = Color.Purple, ForeColor = Color.White, Cursor = Cursors.Hand, Font = new Font("Arial", 10, FontStyle.Bold) };
            Button btnXuatRieng = new Button() { Text = "📂 Xuất Riêng\n(Chọn nơi lưu)", Location = new Point(190, 120), Width = 150, Height = 50, BackColor = Color.OrangeRed, ForeColor = Color.White, Cursor = Cursors.Hand, Font = new Font("Arial", 10, FontStyle.Bold) };

            // Nút thoát duy nhất
            Button btnHuy = new Button() { Text = "❌ Đóng (Xong)", Location = new Point(200, 190), Width = 130, Height = 35, Cursor = Cursors.Hand, BackColor = Color.WhiteSmoke };

            // --- [THAY ĐỔI LỚN TẠI ĐÂY] ---
            // Các nút chức năng sẽ KHÔNG đóng hộp thoại nữa.
            // Bấm xong nó vẫn ở đó để bạn bấm lại hoặc bấm nút khác.

            btnIn.Click += (s, ev) =>
            {
                MoCheDoIn();
                // Sau khi in xong, code chạy tiếp ở đây: KHÔNG CÓ LỆNH CLOSE. Form vẫn hiện.
            };

            btnLuuNhanh.Click += (s, ev) =>
            {
                XuatFileExcel(autoSave: true);
            };

            btnCaHai.Click += (s, ev) =>
            {
                MoCheDoIn();
                XuatFileExcel(autoSave: true);
            };

            btnXuatRieng.Click += (s, ev) =>
            {
                XuatFileExcel(autoSave: false);
            };

            // Chỉ có nút này mới được phép đóng hộp thoại
            btnHuy.Click += (s, ev) => { luaChon.Close(); };

            luaChon.Controls.AddRange(new Control[] { lblHoi, btnIn, btnLuuNhanh, btnCaHai, btnXuatRieng, btnHuy });

            // Treo màn hình ở đây cho đến khi người dùng bấm nút Đóng
            luaChon.ShowDialog();

            // Khi hộp thoại đóng rồi thì mới đóng Form Bill chính
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        void MoCheDoIn()
        {
            printPreviewDialog1.Document = printDocument1;
            printPreviewDialog1.Width = 800;
            printPreviewDialog1.Height = 600;
            printPreviewDialog1.ShowDialog();
        }

        // --- HÀM XUẤT EXCEL (ĐÃ CHỈNH SỬA TÊN ILF & BORDER MƯỢT) ---
        private void XuatFileExcel(bool autoSave = false)
        {
            try
            {
                using (ExcelPackage package = new ExcelPackage())
                {
                    ExcelWorksheet ws = package.Workbook.Worksheets.Add("HoaDon");

                    // 1. HEADER (TÊN CỬA HÀNG ILF)
                    ws.Cells["B1:E1"].Merge = true;
                    ws.Cells["B1"].Value = "CỬA HÀNG GAME ILF - HÓA ĐƠN"; // [SỬA TÊN TẠI ĐÂY]
                    ws.Cells["B1"].Style.Font.Bold = true;
                    ws.Cells["B1"].Style.Font.Size = 16;
                    ws.Cells["B1"].Style.Font.Color.SetColor(Color.Red);
                    ws.Cells["B1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells["B1"].Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#C6E0B4")); // Xanh lá nhạt
                    ws.Cells["B1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells["B1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                    ws.Cells["B2:E2"].Merge = true;
                    ws.Cells["B2"].Value = $"Ngày: {DateTime.Now:dd/MM/yyyy HH:mm} - Thu ngân: {_seller.FullName}";
                    ws.Cells["B2"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells["B2"].Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#E2EFDA"));
                    ws.Cells["B2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // 2. TIÊU ĐỀ CỘT
                    ws.Cells["B4"].Value = "STT";
                    ws.Cells["C4"].Value = "Tên Game";
                    ws.Cells["D4"].Value = "Giá tiền";
                    ws.Cells["E4"].Value = "Ghi chú";

                    // Kẻ khung tiêu đề
                    using (var range = ws.Cells["B4:E4"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                        // Viền đậm cho tiêu đề
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thick;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    }

                    // 3. DỮ LIỆU & KẺ KHUNG (SỬA LẠI CHO ĐỀU)
                    int row = 5;
                    int stt = 1;
                    foreach (var item in _cart)
                    {
                        ws.Cells[row, 2].Value = stt++;
                        ws.Cells[row, 3].Value = item.TenGame;

                        if (item.GiaTien == 0)
                        {
                            ws.Cells[row, 4].Value = "Free";
                            ws.Cells[row, 4].Style.Font.Color.SetColor(Color.Green);
                            ws.Cells[row, 4].Style.Font.Bold = true;
                        }
                        else
                        {
                            ws.Cells[row, 4].Value = item.GiaTien;
                            ws.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                        }

                        // [MỚI] Kẻ khung từng ô một để đảm bảo đều tăm tắp
                        for (int col = 2; col <= 5; col++)
                        {
                            ws.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        row++;
                    }

                    // 4. TỔNG CỘNG
                    ws.Cells[row, 2].Value = "TỔNG CỘNG:";
                    ws.Cells[row, 2].Style.Font.Bold = true;
                    // Gộp ô tổng tiền cho đẹp nếu cần, hoặc để nguyên

                    // Tô màu nền vàng cho dòng tổng
                    ws.Cells[row, 2, row, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 2, row, 3].Style.Fill.BackgroundColor.SetColor(Color.Yellow);

                    ws.Cells[row, 4].Value = _cart.Sum(x => x.GiaTien);
                    ws.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                    ws.Cells[row, 4].Style.Font.Bold = true;
                    ws.Cells[row, 4].Style.Font.Color.SetColor(Color.Red);

                    // Kẻ khung đậm bao quanh dòng tổng
                    using (var r = ws.Cells[row, 2, row, 5])
                    {
                        r.Style.Border.Top.Style = ExcelBorderStyle.Thick;
                        r.Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
                        r.Style.Border.Left.Style = ExcelBorderStyle.Thick;
                        r.Style.Border.Right.Style = ExcelBorderStyle.Thick;
                    }


                    ws.Column(2).Width = 8; ws.Column(3).Width = 40; ws.Column(4).Width = 20; ws.Column(5).Width = 20;

                    // 5. LƯU FILE (XỬ LÝ 2 CHẾ ĐỘ)
                    if (autoSave)
                    {
                        // Lưu tự động (Cho nhân viên dùng nhanh)
                        string folder = Path.Combine(Application.StartupPath, "HoaDonExcel");
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                        string fileName = $"Bill_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        string fullPath = Path.Combine(folder, fileName);

                        package.SaveAs(new FileInfo(fullPath));

                        // Hỏi mở thư mục
                        if (MessageBox.Show($"Đã lưu Auto thành công!\nFile: {fileName}\n\nBạn có muốn mở thư mục không?", "Xong", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start("explorer.exe", folder);
                        }
                    }
                    else
                    {
                        // Lưu riêng (Save As - Cho sếp)
                        SaveFileDialog sfd = new SaveFileDialog();
                        sfd.Filter = "Excel Files|*.xlsx";
                        sfd.Title = "Chọn nơi lưu hóa đơn (Cho Sếp)";
                        sfd.FileName = $"HoaDon_ILF_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"; // Tên mặc định có chữ ILF

                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            package.SaveAs(new FileInfo(sfd.FileName));
                            MessageBox.Show("Đã xuất file Excel riêng thành công!");
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi xuất Excel: " + ex.Message); }
        }

        // --- HÀM IN GIẤY (ĐÃ SỬA TÊN ILF) ---
        private void InNoiDungHoaDon(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics; Font fontH = new Font("Arial", 18, FontStyle.Bold); Font fontN = new Font("Arial", 11); Font fontB = new Font("Arial", 11, FontStyle.Bold);
            int y = 20; int center = e.PageBounds.Width / 2; int right = e.PageBounds.Width - 50;
            StringFormat c = new StringFormat() { Alignment = StringAlignment.Center }; StringFormat r = new StringFormat() { Alignment = StringAlignment.Far };

            g.DrawString("CỬA HÀNG GAME ILF", fontH, Brushes.Black, center, y, c); // [SỬA TÊN TẠI ĐÂY]
            y += 40;
            g.DrawString($"HÓA ĐƠN BÁN LẺ", new Font("Arial", 14, FontStyle.Bold), Brushes.Black, center, y, c); y += 30;
            g.DrawString($"Ngày: {DateTime.Now:dd/MM/yyyy HH:mm}", fontN, Brushes.Black, 50, y); y += 30;

            g.DrawString("Tên Game", fontB, Brushes.Black, 50, y); g.DrawString("Thành tiền", fontB, Brushes.Black, right, y, r); y += 20;
            g.DrawString("----------------------------------------------------------------", fontN, Brushes.Black, center, y, c); y += 20;

            foreach (var item in _cart)
            {
                g.DrawString(item.TenGame, fontN, Brushes.Black, 50, y);
                g.DrawString(item.GiaTien == 0 ? "Free" : item.GiaTien.ToString("N0"), fontN, Brushes.Black, right, y, r);
                y += 25;
            }

            g.DrawString("----------------------------------------------------------------", fontN, Brushes.Black, center, y, c); y += 20;
            g.DrawString("TỔNG CỘNG:", fontB, Brushes.Black, 50, y);
            g.DrawString($"{_cart.Sum(x => x.GiaTien):N0} VNĐ", fontH, Brushes.Red, right, y - 5, r);
        }

        void LoadGridData()
        {
            dgvBill.DataSource = _cart.Select(x => new { TenGame = x.TenGame, GiaTien = x.GiaTien }).ToList();
            if (dgvBill.Columns["TenGame"] != null) dgvBill.Columns["TenGame"].HeaderText = "Tên Game";
            if (dgvBill.Columns["GiaTien"] != null) dgvBill.Columns["GiaTien"].HeaderText = "Giá Tiền";
            lblTotal.Text = $"TỔNG TIỀN: {_cart.Sum(x => x.GiaTien):N0} đ";
        }
        private void DgvBill_CellClick(object sender, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0 && e.ColumnIndex == dgvBill.Columns["colXoa"].Index) { _cart.RemoveAt(e.RowIndex); LoadGridData(); } }
        void HienThiAnhQR(string f) { try { pbQRCode.Visible = true; lblHuongDan.Visible = true; pbQRCode.Image = File.Exists(f) ? Image.FromFile(f) : null; } catch { } }
    }
}