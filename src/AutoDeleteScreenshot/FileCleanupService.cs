using System.Timers;
using Timer = System.Timers.Timer;

namespace AutoDeleteScreenshot;

/// <summary>
/// Service to scan and delete expired screenshot files
/// </summary>
public class FileCleanupService : IDisposable
{
    private readonly Timer _cleanupTimer;
    private readonly string _screenshotsPath;
    private bool _disposed;

    // Tag format: _AUTODEL_{unix_timestamp}
    private const string DELETE_TAG_PREFIX = "_AUTODEL_";

    /// <summary>
    /// Initialize FileCleanupService
    /// </summary>
    /// <param name="intervalSeconds">Scan interval (seconds), default 60 seconds</param>
    public FileCleanupService(int intervalSeconds = 60)
    {
        _screenshotsPath = PathHelper.GetScreenshotsPath();
        System.Diagnostics.Debug.WriteLine($"Watching cleanup path: {_screenshotsPath}");

        // Create periodic scan timer
        _cleanupTimer = new Timer(intervalSeconds * 1000)
        {
            AutoReset = true,
            Enabled = true
        };
        _cleanupTimer.Elapsed += OnCleanupTimerElapsed;

        // Scan immediately on startup
        Task.Run(CleanupExpiredFiles);
    }

    /// <summary>
    /// Handle timer tick
    /// </summary>
    private void OnCleanupTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        CleanupExpiredFiles();
    }

    /// <summary>
    /// Scan and delete expired files
    /// </summary>
    private void CleanupExpiredFiles()
    {
        try
        {
            if (!Directory.Exists(_screenshotsPath))
                return;

            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // ONLY get files with delete tag -> PERFORMANCE OPTIMIZATION
            // Pattern: *{DELETE_TAG_PREFIX}*.png
            string searchPattern = $"*{DELETE_TAG_PREFIX}*.png";
            var files = Directory.GetFiles(_screenshotsPath, searchPattern);

            foreach (var filePath in files)
            {
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    
                    // Check if file has delete tag
                    long? deleteTimestamp = GetDeleteTimestamp(fileName);
                    
                    if (deleteTimestamp.HasValue && deleteTimestamp.Value <= currentTimestamp)
                    {
                        // File expired, delete
                        File.Delete(filePath);
                        System.Diagnostics.Debug.WriteLine($"Deleted expired file: {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    // Ignore error for individual file
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
    /// Check if file has delete tag and return delete timestamp
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <returns>Unix timestamp for deletion, or null if no tag</returns>
    private static long? GetDeleteTimestamp(string fileName)
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
    /// Stop service
    /// </summary>
    public void Stop()
    {
        _cleanupTimer.Stop();
    }

    /// <summary>
    /// Start service
    /// </summary>
    public void Start()
    {
        _cleanupTimer.Start();
    }

    /// <summary>
    /// Force immediate cleanup
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
