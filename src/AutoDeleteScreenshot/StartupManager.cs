using Microsoft.Win32;

namespace AutoDeleteScreenshot;

/// <summary>
/// Quản lý khởi động cùng Windows qua Registry
/// </summary>
public static class StartupManager
{
    private const string AppName = "AutoDeleteScreenshot";
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Kiểm tra xem app có được cấu hình khởi động cùng Windows không
    /// </summary>
    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Bật khởi động cùng Windows
    /// </summary>
    public static bool Enable()
    {
        try
        {
            string exePath = Application.ExecutablePath;
            
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.SetValue(AppName, $"\"{exePath}\"");
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error enabling startup: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tắt khởi động cùng Windows
    /// </summary>
    public static bool Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.DeleteValue(AppName, false);
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error disabling startup: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Toggle trạng thái khởi động cùng Windows
    /// </summary>
    public static bool Toggle()
    {
        if (IsEnabled())
        {
            return Disable();
        }
        else
        {
            return Enable();
        }
    }
}
