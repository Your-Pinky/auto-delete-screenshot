namespace AutoDeleteScreenshot;

/// <summary>
/// Theo dõi thư mục Screenshots và gắn tag thời gian xóa vào file mới
/// </summary>
public class ScreenshotWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly Func<int> _getDeleteAfterMinutes;
    private readonly Action<string>? _onNewScreenshot;
    private bool _disposed;

    // Tag format: _AUTODEL_{unix_timestamp}
    private const string DELETE_TAG_PREFIX = "_AUTODEL_";

    /// <summary>
    /// Khởi tạo ScreenshotWatcher
    /// </summary>
    /// <param name="getDeleteAfterMinutes">Callback lấy thời gian xóa hiện tại (phút)</param>
    /// <param name="onNewScreenshot">Callback khi có ảnh mới (tên file mới)</param>
    public ScreenshotWatcher(Func<int> getDeleteAfterMinutes, Action<string>? onNewScreenshot = null)
    {
        _getDeleteAfterMinutes = getDeleteAfterMinutes;
        _onNewScreenshot = onNewScreenshot;

        // Đường dẫn thư mục Screenshots chính xác từ Registry
        string screenshotsPath = PathHelper.GetScreenshotsPath();
        System.Diagnostics.Debug.WriteLine($"Watching screenshots path: {screenshotsPath}");

        // Đảm bảo thư mục tồn tại
        if (!Directory.Exists(screenshotsPath))
        {
            Directory.CreateDirectory(screenshotsPath);
        }

        _watcher = new FileSystemWatcher(screenshotsPath)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            Filter = "*.png",
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileCreated;
    }

    /// <summary>
    /// Xử lý khi có file mới được tạo
    /// </summary>
    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Bỏ qua nếu file đã có tag xóa
            if (e.Name?.Contains(DELETE_TAG_PREFIX) == true)
                return;

            int deleteAfterMinutes = _getDeleteAfterMinutes();
            
            // Bỏ qua nếu không xóa tự động
            if (deleteAfterMinutes <= 0)
                return;

            // Đợi file được ghi xong (Windows có thể đang ghi)
            Thread.Sleep(500);

            if (!File.Exists(e.FullPath))
                return;

            // Tính thời gian xóa (Unix timestamp)
            long deleteTimestamp = DateTimeOffset.UtcNow.AddMinutes(deleteAfterMinutes).ToUnixTimeSeconds();

            // Tạo tên file mới với tag
            string directory = Path.GetDirectoryName(e.FullPath) ?? "";
            string fileName = Path.GetFileNameWithoutExtension(e.FullPath);
            string extension = Path.GetExtension(e.FullPath);
            string newFileName = $"{fileName}{DELETE_TAG_PREFIX}{deleteTimestamp}{extension}";
            string newFilePath = Path.Combine(directory, newFileName);

            // Rename file
            File.Move(e.FullPath, newFilePath);

            // Gọi callback
            _onNewScreenshot?.Invoke(newFileName);
        }
        catch (Exception ex)
        {
            // Log lỗi nhưng không crash ứng dụng
            System.Diagnostics.Debug.WriteLine($"ScreenshotWatcher error: {ex.Message}");
        }
    }

    /// <summary>
    /// Kiểm tra xem file có tag xóa không và trả về thời gian xóa
    /// </summary>
    /// <param name="fileName">Tên file</param>
    /// <returns>Unix timestamp để xóa, hoặc null nếu không có tag</returns>
    public static long? GetDeleteTimestamp(string fileName)
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
    /// Dừng theo dõi
    /// </summary>
    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
    }

    /// <summary>
    /// Bắt đầu theo dõi
    /// </summary>
    public void Start()
    {
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Cleanup
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _watcher.Created -= OnFileCreated;
            _watcher.Dispose();
            _disposed = true;
        }
    }
}
