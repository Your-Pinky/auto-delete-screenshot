using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace AutoDeleteScreenshot;

/// <summary>
/// Manage Desktop shortcut creation
/// </summary>
public static class ShortcutManager
{
    private const string ShortcutName = "Auto Delete Screenshot.lnk";

    /// <summary>
    /// Get Desktop shortcut path
    /// </summary>
    private static string GetDesktopShortcutPath()
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        return Path.Combine(desktopPath, ShortcutName);
    }

    /// <summary>
    /// Check if Desktop shortcut exists
    /// </summary>
    public static bool Exists()
    {
        return File.Exists(GetDesktopShortcutPath());
    }

    /// <summary>
    /// Create Desktop shortcut
    /// </summary>
    public static bool Create()
    {
        try
        {
            string shortcutPath = GetDesktopShortcutPath();
            string exePath = Application.ExecutablePath;
            string workingDir = Path.GetDirectoryName(exePath) ?? "";

            // Use COM to create shortcut
            IShellLink link = (IShellLink)new ShellLink();

            link.SetPath(exePath);
            link.SetWorkingDirectory(workingDir);
            link.SetDescription("Auto Delete Screenshot - Keep your desktop clean");

            // Save shortcut
            IPersistFile file = (IPersistFile)link;
            file.Save(shortcutPath, false);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating shortcut: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Delete Desktop shortcut
    /// </summary>
    public static bool Delete()
    {
        try
        {
            string shortcutPath = GetDesktopShortcutPath();
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting shortcut: {ex.Message}");
            return false;
        }
    }

    #region COM Interop for Shell Link

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    #endregion
}
