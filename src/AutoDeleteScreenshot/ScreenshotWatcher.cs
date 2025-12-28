namespace AutoDeleteScreenshot;

/// <summary>
/// Monitor Screenshots folder and append delete timestamp tag to new files
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
    /// Initialize ScreenshotWatcher
    /// </summary>
    /// <param name="getDeleteAfterMinutes">Callback to get current delete time (minutes)</param>
    /// <param name="onNewScreenshot">Callback when new screenshot detected (new file name)</param>
    public ScreenshotWatcher(Func<int> getDeleteAfterMinutes, Action<string>? onNewScreenshot = null)
    {
        _getDeleteAfterMinutes = getDeleteAfterMinutes;
        _onNewScreenshot = onNewScreenshot;

        // Exact Screenshots folder path from Registry
        string screenshotsPath = PathHelper.GetScreenshotsPath();
        System.Diagnostics.Debug.WriteLine($"Watching screenshots path: {screenshotsPath}");

        // Ensure directory exists
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
    /// Handle new file created event
    /// </summary>
    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Skip if file already has delete tag
            if (e.Name?.Contains(DELETE_TAG_PREFIX) == true)
                return;

            int deleteAfterMinutes = _getDeleteAfterMinutes();
            
            // Skip if auto-delete is disabled
            if (deleteAfterMinutes <= 0)
                return;

            // Wait for file write completion (Windows might be writing)
            Thread.Sleep(500);

            if (!File.Exists(e.FullPath))
                return;

            // Calculate delete time (Unix timestamp)
            long deleteTimestamp = DateTimeOffset.UtcNow.AddMinutes(deleteAfterMinutes).ToUnixTimeSeconds();

            // Create new file name with tag
            string directory = Path.GetDirectoryName(e.FullPath) ?? "";
            string fileName = Path.GetFileNameWithoutExtension(e.FullPath);
            string extension = Path.GetExtension(e.FullPath);
            string newFileName = $"{fileName}{DELETE_TAG_PREFIX}{deleteTimestamp}{extension}";
            string newFilePath = Path.Combine(directory, newFileName);

            // Rename file
            File.Move(e.FullPath, newFilePath);

            // Invoke callback
            _onNewScreenshot?.Invoke(newFileName);
        }
        catch (Exception ex)
        {
            // Log error but do not crash application
            System.Diagnostics.Debug.WriteLine($"ScreenshotWatcher error: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if file has delete tag and return delete timestamp
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <returns>Unix timestamp for deletion, or null if no tag</returns>
    public static long? GetDeleteTimestamp(string fileName)
    {
        int tagIndex = fileName.IndexOf(DELETE_TAG_PREFIX);
        if (tagIndex < 0)
            return null;

        string timestampStr = fileName.Substring(tagIndex + DELETE_TAG_PREFIX.Length);
        
        // Remove extension if exists
        int dotIndex = timestampStr.IndexOf('.');
        if (dotIndex >= 0)
            timestampStr = timestampStr.Substring(0, dotIndex);

        if (long.TryParse(timestampStr, out long timestamp))
            return timestamp;

        return null;
    }

    /// <summary>
    /// Stop monitoring
    /// </summary>
    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
    }

    /// <summary>
    /// Start monitoring
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
