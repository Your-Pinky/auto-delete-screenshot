using System.Text.Json;

namespace AutoDeleteScreenshot;

/// <summary>
/// Manage saving/loading settings from JSON file
/// </summary>
public class SettingsManager
{
    private readonly string _settingsFilePath;
    private AppSettings _settings;

    /// <summary>
    /// Settings model
    /// </summary>
    public class AppSettings
    {
        public int DeleteAfterMinutes { get; set; } = 30;
        public bool ShowToast { get; set; } = false;
        public string? ScreenshotsPath { get; set; } = null;
    }

    public SettingsManager()
    {
        // Save settings to AppData
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appDataPath, "AutoDeleteScreenshot");
        
        // Create directory if not exists
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }
        
        _settingsFilePath = Path.Combine(appFolder, "settings.json");
        _settings = Load();
    }

    /// <summary>
    /// Load settings from JSON file
    /// </summary>
    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
        
        return new AppSettings();
    }

    /// <summary>
    /// Save settings to JSON file
    /// </summary>
    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete time (minutes)
    /// </summary>
    public int DeleteAfterMinutes
    {
        get => _settings.DeleteAfterMinutes;
        set
        {
            _settings.DeleteAfterMinutes = value;
            Save();
        }
    }

    /// <summary>
    /// Is toast enabled
    /// </summary>
    public bool ShowToast
    {
        get => _settings.ShowToast;
        set
        {
            _settings.ShowToast = value;
            Save();
        }
    }

    /// <summary>
    /// Custom Screenshots folder path
    /// </summary>
    public string? ScreenshotsPath
    {
        get => _settings.ScreenshotsPath;
        set
        {
            _settings.ScreenshotsPath = value;
            Save();
        }
    }

    /// <summary>
    /// Check if path is configured
    /// </summary>
    public bool HasScreenshotsPath => !string.IsNullOrEmpty(_settings.ScreenshotsPath) && Directory.Exists(_settings.ScreenshotsPath);
}
