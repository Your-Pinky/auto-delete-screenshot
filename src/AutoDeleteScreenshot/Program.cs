using System.Diagnostics;

namespace AutoDeleteScreenshot;

static class Program
{
    /// <summary>
    /// Entry point của ứng dụng
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Đặt process priority thấp để không ảnh hưởng đến các ứng dụng khác
        try
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch
        {
            // Ignore nếu không thể set priority
        }

        // Chỉ cho phép chạy 1 instance duy nhất
        using var mutex = new Mutex(true, "AutoDeleteScreenshot_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "Auto Delete Screenshot đã đang chạy!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            return;
        }

        // Khởi tạo Windows Forms
        ApplicationConfiguration.Initialize();
        
        // Chạy ứng dụng với TrayApplicationContext
        Application.Run(new TrayApplicationContext());
    }
}
