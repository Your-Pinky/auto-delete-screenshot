using System.Drawing;

namespace AutoDeleteScreenshot;

/// <summary>
/// ApplicationContext để quản lý System Tray icon và menu
/// </summary>
public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _contextMenu;
    
    // Menu items cho thời gian xóa
    private readonly ToolStripMenuItem _menuNoDelete;
    private readonly ToolStripMenuItem _menu15Min;
    private readonly ToolStripMenuItem _menu30Min;
    private readonly ToolStripMenuItem _menu1Hour;
    private readonly ToolStripMenuItem _menu24Hours;
    private readonly ToolStripMenuItem _menuShowToast;
    
    // Thời gian xóa hiện tại (phút), 0 = không xóa
    private int _deleteAfterMinutes = 30;
    private bool _showToast = false;

    public TrayApplicationContext()
    {
        // Tạo context menu
        _contextMenu = new ContextMenuStrip();
        
        // Header
        var header = new ToolStripLabel("⏱️ Auto Delete Screenshot")
        {
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        _contextMenu.Items.Add(header);
        _contextMenu.Items.Add(new ToolStripSeparator());
        
        // Các tùy chọn thời gian
        _menuNoDelete = new ToolStripMenuItem("Không xóa tự động", null, OnDeleteTimeChanged) { Tag = 0 };
        _menu15Min = new ToolStripMenuItem("15 phút", null, OnDeleteTimeChanged) { Tag = 15 };
        _menu30Min = new ToolStripMenuItem("30 phút", null, OnDeleteTimeChanged) { Tag = 30, Checked = true };
        _menu1Hour = new ToolStripMenuItem("1 giờ", null, OnDeleteTimeChanged) { Tag = 60 };
        _menu24Hours = new ToolStripMenuItem("24 giờ", null, OnDeleteTimeChanged) { Tag = 1440 };
        
        _contextMenu.Items.Add(_menuNoDelete);
        _contextMenu.Items.Add(_menu15Min);
        _contextMenu.Items.Add(_menu30Min);
        _contextMenu.Items.Add(_menu1Hour);
        _contextMenu.Items.Add(_menu24Hours);
        
        _contextMenu.Items.Add(new ToolStripSeparator());
        
        // Tùy chọn Toast
        _menuShowToast = new ToolStripMenuItem("Hiện thông báo khi chụp", null, OnShowToastChanged)
        {
            CheckOnClick = true,
            Checked = _showToast
        };
        _contextMenu.Items.Add(_menuShowToast);
        
        _contextMenu.Items.Add(new ToolStripSeparator());
        
        // Nút thoát
        var exitItem = new ToolStripMenuItem("❌ Thoát", null, OnExit);
        _contextMenu.Items.Add(exitItem);
        
        // Tạo tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Auto Delete Screenshot - 30 phút",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };
        
        // Double click để mở menu
        _trayIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                // Hiện menu khi click trái
                var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                mi?.Invoke(_trayIcon, null);
            }
        };
        
        UpdateMenuCheckmarks();
    }

    /// <summary>
    /// Load icon từ file hoặc tạo icon mặc định
    /// </summary>
    private Icon LoadIcon()
    {
        try
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "icon.png");
            if (File.Exists(iconPath))
            {
                using var bitmap = new Bitmap(iconPath);
                return Icon.FromHandle(bitmap.GetHicon());
            }
        }
        catch { }
        
        // Tạo icon mặc định nếu không load được
        return CreateDefaultIcon();
    }

    /// <summary>
    /// Tạo icon mặc định màu xanh
    /// </summary>
    private Icon CreateDefaultIcon()
    {
        var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(Color.FromArgb(0, 120, 215)); // Windows blue
            g.FillEllipse(brush, 1, 1, 14, 14);
            using var whiteBrush = new SolidBrush(Color.White);
            g.FillRectangle(whiteBrush, 6, 4, 4, 5); // Clock hand
            g.FillRectangle(whiteBrush, 6, 6, 5, 2); // Clock hand horizontal
        }
        return Icon.FromHandle(bitmap.GetHicon());
    }

    /// <summary>
    /// Xử lý khi thay đổi thời gian xóa
    /// </summary>
    private void OnDeleteTimeChanged(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item && item.Tag is int minutes)
        {
            _deleteAfterMinutes = minutes;
            UpdateMenuCheckmarks();
            UpdateTooltip();
            
            // TODO: Lưu setting và thông báo cho các service khác
        }
    }

    /// <summary>
    /// Xử lý khi bật/tắt toast
    /// </summary>
    private void OnShowToastChanged(object? sender, EventArgs e)
    {
        _showToast = _menuShowToast.Checked;
        // TODO: Lưu setting
    }

    /// <summary>
    /// Cập nhật checkmark cho menu items
    /// </summary>
    private void UpdateMenuCheckmarks()
    {
        _menuNoDelete.Checked = _deleteAfterMinutes == 0;
        _menu15Min.Checked = _deleteAfterMinutes == 15;
        _menu30Min.Checked = _deleteAfterMinutes == 30;
        _menu1Hour.Checked = _deleteAfterMinutes == 60;
        _menu24Hours.Checked = _deleteAfterMinutes == 1440;
    }

    /// <summary>
    /// Cập nhật tooltip của tray icon
    /// </summary>
    private void UpdateTooltip()
    {
        string timeText = _deleteAfterMinutes switch
        {
            0 => "Không xóa tự động",
            15 => "15 phút",
            30 => "30 phút",
            60 => "1 giờ",
            1440 => "24 giờ",
            _ => $"{_deleteAfterMinutes} phút"
        };
        _trayIcon.Text = $"Auto Delete Screenshot - {timeText}";
    }

    /// <summary>
    /// Xử lý khi nhấn nút Thoát
    /// </summary>
    private void OnExit(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    /// <summary>
    /// Lấy thời gian xóa hiện tại
    /// </summary>
    public int DeleteAfterMinutes => _deleteAfterMinutes;

    /// <summary>
    /// Có hiện toast không
    /// </summary>
    public bool ShowToast => _showToast;

    /// <summary>
    /// Cleanup khi đóng ứng dụng
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon?.Dispose();
            _contextMenu?.Dispose();
        }
        base.Dispose(disposing);
    }
}
