using Microsoft.Win32;

namespace AutoDeleteScreenshot;

/// <summary>
/// Manage Windows startup via Registry
/// </summary>
public static class StartupManager
{
    private const string AppName = "AutoDeleteScreenshot";
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Check if app is configured to run at Windows startup
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
    /// Enable Windows startup
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
    /// Disable Windows startup
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
    /// Toggle Windows startup status
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
