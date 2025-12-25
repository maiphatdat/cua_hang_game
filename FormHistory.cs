using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing; // Để in ấn
using System.Linq;
using System.Windows.Forms;
using System.IO;
using OfficeOpenXml; // Để xuất Excel
using OfficeOpenXml.Style;

namespace QuanLyGame_Final
{
    public partial class FormHistory : Form
    {
        GameContext db = new GameContext();

        // Các biến giao diện
        DataGridView dgvOrders;      // Bảng danh sách hóa đơn
        DataGridView dgvDetails;     // Bảng chi tiết món ăn
        Button btnPrintAgain;        // Nút In lại
        Button btnExcelAgain;        // Nút Xuất Excel lại
        DateTimePicker dtpTuNgay, dtpDenNgay;
        Button btnLoc;

        // Biến lưu đơn hàng đang chọn để in
        Order _selectedOrder = null;
        PrintDocument printDocument1 = new PrintDocument();
        PrintPreviewDialog printPreviewDialog1 = new PrintPreviewDialog();

        public FormHistory()
        {
            // Cấu hình bản quyền EPPlus (Bắt buộc)
            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            SetupUI();
            LoadDataOrders();

            // Cấu hình in ấn
            printDocument1.PrintPage += InNoiDungHoaDonCu;
        }

        void SetupUI()
        {
            this.Text = "LỊCH SỬ GIAO DỊCH & TRA CỨU";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // --- 1. THANH CÔNG CỤ (TOP) ---
            Panel pnlTop = new Panel() { Dock = DockStyle.Top, Height = 60, BackColor = Color.WhiteSmoke };

            Label lblTu = new Label() { Text = "Từ ngày:", Location = new Point(20, 20), AutoSize = true };
            dtpTuNgay = new DateTimePicker() { Location = new Point(80, 17), Format = DateTimePickerFormat.Short, Width = 120, Value = DateTime.Now.AddDays(-7) }; // Mặc định xem 7 ngày qua

            Label lblDen = new Label() { Text = "Đến:", Location = new Point(220, 20), AutoSize = true };
            dtpDenNgay = new DateTimePicker() { Location = new Point(260, 17), Format = DateTimePickerFormat.Short, Width = 120 };

            btnLoc = new Button() { Text = "🔍 Lọc Dữ Liệu", Location = new Point(400, 15), Width = 120, Height = 30, BackColor = Color.Teal, ForeColor = Color.White };
            btnLoc.Click += (s, e) => LoadDataOrders();

            // NÚT IN LẠI
            btnPrintAgain = new Button() { Text = "🖨 IN LẠI BILL", Location = new Point(750, 10), Width = 140, Height = 40, BackColor = Color.DarkBlue, ForeColor = Color.White, Font = new Font("Arial", 9, FontStyle.Bold), Enabled = false };
            btnPrintAgain.Click += BtnPrintAgain_Click;

            // NÚT XUẤT EXCEL LẠI
            btnExcelAgain = new Button() { Text = "📊 XUẤT EXCEL", Location = new Point(900, 10), Width = 140, Height = 40, BackColor = Color.Green, ForeColor = Color.White, Font = new Font("Arial", 9, FontStyle.Bold), Enabled = false };
            btnExcelAgain.Click += BtnExcelAgain_Click;

            pnlTop.Controls.AddRange(new Control[] { lblTu, dtpTuNgay, lblDen, dtpDenNgay, btnLoc, btnPrintAgain, btnExcelAgain });

            // --- 2. KHUNG CHỨA HAI BẢNG (SPLIT) ---
            SplitContainer split = new SplitContainer() { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 600 };

            // Bảng TRÁI: Danh sách Hóa đơn
            GroupBox grpLeft = new GroupBox() { Text = "Danh sách Hóa đơn", Dock = DockStyle.Fill };
            dgvOrders = new DataGridView() { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, ReadOnly = true, RowHeadersVisible = false };
            dgvOrders.CellClick += DgvOrders_CellClick;
            grpLeft.Controls.Add(dgvOrders);

            // Bảng PHẢI: Chi tiết món
            GroupBox grpRight = new GroupBox() { Text = "Chi tiết món trong đơn", Dock = DockStyle.Fill };
            dgvDetails = new DataGridView() { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true, RowHeadersVisible = false };
            grpRight.Controls.Add(dgvDetails);

            split.Panel1.Controls.Add(grpLeft);
            split.Panel2.Controls.Add(grpRight);

            this.Controls.Add(split);
            this.Controls.Add(pnlTop);
        }

        // --- TẢI DANH SÁCH HÓA ĐƠN ---
        void LoadDataOrders()
        {
            DateTime start = dtpTuNgay.Value.Date;
            DateTime end = dtpDenNgay.Value.Date.AddDays(1).AddTicks(-1); // Cuối ngày

            var list = db.Orders
                .Where(x => x.NgayMua >= start && x.NgayMua <= end)
                .OrderByDescending(x => x.NgayMua) // Mới nhất lên đầu
                .Select(x => new {
                    x.OrderID,
                    x.NgayMua,
                    x.TongTien,
                    x.NguoiBan
                })
                .ToList();

            dgvOrders.DataSource = list;

            // Đặt tên cột cho đẹp
            dgvOrders.Columns["OrderID"].HeaderText = "Mã Bill";
            dgvOrders.Columns["NgayMua"].HeaderText = "Thời gian";
            dgvOrders.Columns["NgayMua"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            dgvOrders.Columns["TongTien"].HeaderText = "Tổng tiền";
            dgvOrders.Columns["TongTien"].DefaultCellStyle.Format = "N0";
            dgvOrders.Columns["NguoiBan"].HeaderText = "Thu ngân";

            // Reset bảng chi tiết
            dgvDetails.DataSource = null;
            btnPrintAgain.Enabled = false;
            btnExcelAgain.Enabled = false;
        }

        // --- KHI BẤM VÀO MỘT HÓA ĐƠN ---
        private void DgvOrders_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                int id = Convert.ToInt32(dgvOrders.Rows[e.RowIndex].Cells["OrderID"].Value);

                // Lấy thông tin chi tiết từ DB (Quan trọng: Include OrderDetails)
                _selectedOrder = db.Orders.Include("OrderDetails").FirstOrDefault(x => x.OrderID == id);

                if (_selectedOrder != null)
                {
                    // Hiển thị chi tiết sang bảng bên phải
                    dgvDetails.DataSource = _selectedOrder.OrderDetails.Select(x => new {
                        x.TenGame,
                        Gia = x.GiaTien
                    }).ToList();

                    dgvDetails.Columns["TenGame"].HeaderText = "Tên Game";
                    dgvDetails.Columns["Gia"].HeaderText = "Giá bán";
                    dgvDetails.Columns["Gia"].DefaultCellStyle.Format = "N0";

                    // Bật sáng 2 nút chức năng
                    btnPrintAgain.Enabled = true;
                    btnExcelAgain.Enabled = true;
                }
            }
        }

        // --- CHỨC NĂNG 1: IN LẠI BILL ---
        private void BtnPrintAgain_Click(object sender, EventArgs e)
        {
            if (_selectedOrder == null) return;

            // Mở lại khung xem trước y hệt lúc thanh toán
            printPreviewDialog1.Document = printDocument1;
            printPreviewDialog1.Width = 800;
            printPreviewDialog1.Height = 600;
            printPreviewDialog1.ShowDialog();
        }

        // --- LOGIC VẼ HÓA ĐƠN CŨ (Copy từ FormBill sang nhưng sửa nguồn dữ liệu) ---
        private void InNoiDungHoaDonCu(object sender, PrintPageEventArgs e)
        {
            if (_selectedOrder == null) return;

            Graphics g = e.Graphics;
            Font fontH = new Font("Arial", 18, FontStyle.Bold);
            Font fontN = new Font("Arial", 11);
            Font fontB = new Font("Arial", 11, FontStyle.Bold);

            int y = 20;
            int center = e.PageBounds.Width / 2;
            int right = e.PageBounds.Width - 50;
            StringFormat c = new StringFormat() { Alignment = StringAlignment.Center };
            StringFormat r = new StringFormat() { Alignment = StringAlignment.Far };

            // HEADER
            g.DrawString("CỬA HÀNG GAME ILF", fontH, Brushes.Black, center, y, c); y += 40;
            g.DrawString("(BẢN SAO / IN LẠI)", new Font("Arial", 10, FontStyle.Italic), Brushes.Black, center, y, c); y += 20; // Đánh dấu là in lại
            g.DrawString($"HÓA ĐƠN BÁN LẺ", new Font("Arial", 14, FontStyle.Bold), Brushes.Black, center, y, c); y += 30;
            g.DrawString($"Ngày: {_selectedOrder.NgayMua:dd/MM/yyyy HH:mm}", fontN, Brushes.Black, 50, y); y += 20;
            g.DrawString($"Thu ngân: {_selectedOrder.NguoiBan}", fontN, Brushes.Black, 50, y); y += 30;

            // TABLE HEADER
            g.DrawString("Tên Game", fontB, Brushes.Black, 50, y);
            g.DrawString("Thành tiền", fontB, Brushes.Black, right, y, r); y += 20;
            g.DrawString("----------------------------------------------------------------", fontN, Brushes.Black, center, y, c); y += 20;

            // ITEMS (Lấy từ _selectedOrder.OrderDetails)
            foreach (var item in _selectedOrder.OrderDetails)
            {
                g.DrawString(item.TenGame, fontN, Brushes.Black, 50, y);
                string gia = item.GiaTien == 0 ? "Free" : item.GiaTien.ToString("N0");
                g.DrawString(gia, fontN, Brushes.Black, right, y, r);
                y += 25;
            }

            // FOOTER
            g.DrawString("----------------------------------------------------------------", fontN, Brushes.Black, center, y, c); y += 20;
            g.DrawString("TỔNG CỘNG:", fontB, Brushes.Black, 50, y);
            g.DrawString($"{_selectedOrder.TongTien:N0} VNĐ", fontH, Brushes.Red, right, y - 5, r);
        }

        // --- CHỨC NĂNG 2: XUẤT EXCEL LẠI ---
        private void BtnExcelAgain_Click(object sender, EventArgs e)
        {
            if (_selectedOrder == null) return;

            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Excel Files|*.xlsx";
                sfd.Title = "Chọn nơi lưu file Excel (In lại)";
                sfd.FileName = $"HoaDon_ILF_Reprint_{_selectedOrder.OrderID}_{DateTime.Now:yyyyMMdd}.xlsx";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (ExcelPackage package = new ExcelPackage())
                    {
                        ExcelWorksheet ws = package.Workbook.Worksheets.Add("HoaDonCu");

                        // 1. Header ILF
                        ws.Cells["B1:E1"].Merge = true;
                        ws.Cells["B1"].Value = "CỬA HÀNG GAME ILF - HÓA ĐƠN (SAO LƯU)";
                        ws.Cells["B1"].Style.Font.Bold = true;
                        ws.Cells["B1"].Style.Font.Size = 16;
                        ws.Cells["B1"].Style.Font.Color.SetColor(Color.Red);
                        ws.Cells["B1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells["B1"].Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#C6E0B4"));
                        ws.Cells["B1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        ws.Cells["B2:E2"].Merge = true;
                        ws.Cells["B2"].Value = $"Ngày: {_selectedOrder.NgayMua:dd/MM/yyyy HH:mm} - Thu ngân: {_selectedOrder.NguoiBan} - Mã Bill: #{_selectedOrder.OrderID}";
                        ws.Cells["B2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        // 2. Cột
                        ws.Cells["B4"].Value = "STT";
                        ws.Cells["C4"].Value = "Tên Game";
                        ws.Cells["D4"].Value = "Giá tiền";
                        ws.Cells["E4"].Value = "Ghi chú";

                        using (var range = ws.Cells["B4:E4"])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
                        }

                        // 3. Dữ liệu
                        int row = 5;
                        int stt = 1;
                        foreach (var item in _selectedOrder.OrderDetails)
                        {
                            ws.Cells[row, 2].Value = stt++;
                            ws.Cells[row, 3].Value = item.TenGame;
                            ws.Cells[row, 4].Value = item.GiaTien;
                            if (item.GiaTien == 0) ws.Cells[row, 4].Value = "Free";
                            else ws.Cells[row, 4].Style.Numberformat.Format = "#,##0";

                            // Kẻ khung
                            for (int c = 2; c <= 5; c++) ws.Cells[row, c].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            row++;
                        }

                        // 4. Tổng
                        ws.Cells[row, 2].Value = "TỔNG CỘNG:";
                        ws.Cells[row, 2].Style.Font.Bold = true;
                        ws.Cells[row, 4].Value = _selectedOrder.TongTien;
                        ws.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                        ws.Cells[row, 4].Style.Font.Bold = true;
                        ws.Cells[row, 4].Style.Font.Color.SetColor(Color.Red);
                        ws.Cells[row, 2, row, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[row, 2, row, 5].Style.Fill.BackgroundColor.SetColor(Color.Yellow);

                        ws.Column(2).Width = 8; ws.Column(3).Width = 40; ws.Column(4).Width = 20; ws.Column(5).Width = 20;

                        package.SaveAs(new FileInfo(sfd.FileName));
                        MessageBox.Show("Đã xuất lại file Excel thành công!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }
    }
}