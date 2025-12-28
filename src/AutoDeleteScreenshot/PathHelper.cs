using System.Runtime.InteropServices;

namespace AutoDeleteScreenshot;

public static class PathHelper
{
    private static readonly Guid KnownFolderScreenshots = new("b7bede81-dfdb-4f24-936b-7058bbc606ea");

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, 
        uint dwFlags, 
        IntPtr hToken, 
        out IntPtr ppszPath);

    /// <summary>
    /// Lấy đường dẫn thư mục Screenshots chính xác từ Windows Registry
    /// </summary>
    public static string GetScreenshotsPath()
    {
        try
        {
            // Thử lấy bằng Known Folder ID trước
            if (SHGetKnownFolderPath(KnownFolderScreenshots, 0, IntPtr.Zero, out IntPtr pPath) == 0)
            {
                string? path = Marshal.PtrToStringUni(pPath);
                Marshal.FreeCoTaskMem(pPath);
                
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    return path;
                }
            }
        }
        catch
        {
            // Ignore error
        }

        // Fallback về đường dẫn mặc định nếu không tìm thấy
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
    }
}
