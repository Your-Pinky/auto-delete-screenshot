using System.Timers;
using Timer = System.Timers.Timer;

namespace AutoDeleteScreenshot;

/// <summary>
/// Service quét và xóa các file ảnh đã hết hạn
/// </summary>
public class FileCleanupService : IDisposable
{
    private readonly Timer _cleanupTimer;
    private readonly string _screenshotsPath;
    private bool _disposed;

    // Tag format: _AUTODEL_{unix_timestamp}
    private const string DELETE_TAG_PREFIX = "_AUTODEL_";

    /// <summary>
    /// Khởi tạo FileCleanupService
    /// </summary>
    /// <param name="intervalSeconds">Khoảng thời gian quét (giây), mặc định 60 giây</param>
    public FileCleanupService(int intervalSeconds = 60)
    {
        _screenshotsPath = GetScreenshotsPath();

        // Tạo timer quét định kỳ
        _cleanupTimer = new Timer(intervalSeconds * 1000)
        {
            AutoReset = true,
            Enabled = true
        };
        _cleanupTimer.Elapsed += OnCleanupTimerElapsed;

        // Quét ngay lập tức khi khởi động
        Task.Run(CleanupExpiredFiles);
    }

    /// <summary>
    /// Lấy đường dẫn thư mục Screenshots
    /// </summary>
    private static string GetScreenshotsPath()
    {
        string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        return Path.Combine(picturesPath, "Screenshots");
    }

    /// <summary>
    /// Xử lý khi timer tick
    /// </summary>
    private void OnCleanupTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        CleanupExpiredFiles();
    }

    /// <summary>
    /// Quét và xóa các file đã hết hạn
    /// </summary>
    private void CleanupExpiredFiles()
    {
        try
        {
            if (!Directory.Exists(_screenshotsPath))
                return;

            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Lấy tất cả file trong thư mục
            var files = Directory.GetFiles(_screenshotsPath, "*.png");

            foreach (var filePath in files)
            {
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    
                    // Kiểm tra xem file có tag xóa không
                    long? deleteTimestamp = GetDeleteTimestamp(fileName);
                    
                    if (deleteTimestamp.HasValue && deleteTimestamp.Value <= currentTimestamp)
                    {
                        // File đã hết hạn, xóa
                        File.Delete(filePath);
                        System.Diagnostics.Debug.WriteLine($"Deleted expired file: {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    // Bỏ qua lỗi cho từng file
                    System.Diagnostics.Debug.WriteLine($"Error deleting file: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CleanupExpiredFiles error: {ex.Message}");
        }
    }

    /// <summary>
    /// Kiểm tra xem file có tag xóa không và trả về thời gian xóa
    /// </summary>
    /// <param name="fileName">Tên file</param>
    /// <returns>Unix timestamp để xóa, hoặc null nếu không có tag</returns>
    private static long? GetDeleteTimestamp(string fileName)
    {
        int tagIndex = fileName.IndexOf(DELETE_TAG_PREFIX);
        if (tagIndex < 0)
            return null;

        string timestampStr = fileName.Substring(tagIndex + DELETE_TAG_PREFIX.Length);
        
        // Loại bỏ extension nếu có
        int dotIndex = timestampStr.IndexOf('.');
        if (dotIndex >= 0)
            timestampStr = timestampStr.Substring(0, dotIndex);

        if (long.TryParse(timestampStr, out long timestamp))
            return timestamp;

        return null;
    }

    /// <summary>
    /// Dừng service
    /// </summary>
    public void Stop()
    {
        _cleanupTimer.Stop();
    }

    /// <summary>
    /// Bắt đầu service
    /// </summary>
    public void Start()
    {
        _cleanupTimer.Start();
    }

    /// <summary>
    /// Buộc quét ngay lập tức
    /// </summary>
    public void ForceCleanup()
    {
        Task.Run(CleanupExpiredFiles);
    }

    /// <summary>
    /// Cleanup
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer.Elapsed -= OnCleanupTimerElapsed;
            _cleanupTimer.Dispose();
            _disposed = true;
        }
    }
}
