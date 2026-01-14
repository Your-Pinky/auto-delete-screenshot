using System.Drawing;

namespace AutoDeleteScreenshot;

/// <summary>
/// ApplicationContext to manage System Tray icon and menu
/// </summary>
public class TrayApplicationContext : ApplicationContext
{
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    extern static bool DestroyIcon(IntPtr handle);
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _contextMenu;
    private ScreenshotWatcher? _screenshotWatcher;
    private FileCleanupService? _fileCleanupService;
    private readonly SettingsManager _settingsManager;
    private bool _isFirstRun;
    
    // Menu items for deletion time
    private readonly ToolStripMenuItem _menuNoDelete;
    private readonly ToolStripMenuItem _menu15Min;
    private readonly ToolStripMenuItem _menu30Min;
    private readonly ToolStripMenuItem _menu1Hour;
    private readonly ToolStripMenuItem _menu24Hours;
    private readonly ToolStripMenuItem _menuShowToast;
    
    // Current deletion time (minutes), 0 = no delete
    private int _deleteAfterMinutes = 0;
    private bool _showToast = false;

    public TrayApplicationContext()
    {
        // Load settings from file
        _settingsManager = new SettingsManager();
        _deleteAfterMinutes = _settingsManager.DeleteAfterMinutes;
        _showToast = _settingsManager.ShowToast;
        _isFirstRun = !_settingsManager.HasScreenshotsPath;
        
        // Initialize PathHelper with settings
        PathHelper.Initialize(_settingsManager);
        
        // Create context menu
        _contextMenu = new ContextMenuStrip();
        
        // Header
        var header = new ToolStripLabel("⏱️ Auto Delete Screenshot")
        {
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        _contextMenu.Items.Add(header);
        _contextMenu.Items.Add(new ToolStripSeparator());
        
        // Time options
        _menuNoDelete = new ToolStripMenuItem("No auto-delete", null, OnDeleteTimeChanged) { Tag = 0, Checked = true };
        _menu15Min = new ToolStripMenuItem("15 minutes", null, OnDeleteTimeChanged) { Tag = 15 };
        _menu30Min = new ToolStripMenuItem("30 minutes", null, OnDeleteTimeChanged) { Tag = 30 };
        _menu1Hour = new ToolStripMenuItem("1 hour", null, OnDeleteTimeChanged) { Tag = 60 };
        _menu24Hours = new ToolStripMenuItem("24 hours", null, OnDeleteTimeChanged) { Tag = 1440 };
        
        _contextMenu.Items.Add(_menuNoDelete);
        _contextMenu.Items.Add(_menu15Min);
        _contextMenu.Items.Add(_menu30Min);
        _contextMenu.Items.Add(_menu1Hour);
        _contextMenu.Items.Add(_menu24Hours);
        
        _contextMenu.Items.Add(new ToolStripSeparator());
        
        // Toast option
        _menuShowToast = new ToolStripMenuItem("Show notification on capture", null, OnShowToastChanged)
        {
            CheckOnClick = true,
            Checked = _showToast
        };
        _contextMenu.Items.Add(_menuShowToast);
        
        _contextMenu.Items.Add(new ToolStripSeparator());
        
        // Select Screenshots folder
        var folderItem = new ToolStripMenuItem("📂 Select Screenshots folder...", null, OnSelectFolder);
        _contextMenu.Items.Add(folderItem);
        
        // Create Desktop shortcut
        var shortcutItem = new ToolStripMenuItem("📌 Create Desktop shortcut", null, OnCreateShortcut);
        _contextMenu.Items.Add(shortcutItem);
        
        // Run at startup
        var startupItem = new ToolStripMenuItem("🚀 Run at Windows startup", null, OnStartupChanged)
        {
            CheckOnClick = true,
            Checked = StartupManager.IsEnabled()
        };
        _contextMenu.Items.Add(startupItem);
        
        _contextMenu.Items.Add(new ToolStripSeparator());
        
        // Exit button
        var exitItem = new ToolStripMenuItem("❌ Exit", null, OnExit);
        _contextMenu.Items.Add(exitItem);
        
        // Create tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Auto Delete Screenshot - No auto-delete",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };
        
        // Double click to open menu
        _trayIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                // Show menu on left click
                var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                mi?.Invoke(_trayIcon, null);
            }
        };
        
        UpdateMenuCheckmarks();
        
        // Check if folder is selected, if not prompt to select (first run)
        if (_isFirstRun)
        {
            // Defer first run setup to after tray icon is shown
            var context = SynchronizationContext.Current;
            Task.Run(async () =>
            {
                await Task.Delay(500); // Wait for tray icon to appear
                context?.Post(_ => RunFirstTimeSetup(), null);
            });
        }
        else
        {
            // Initialize services with existing path
            InitializeServices();
        }
    }
    
    /// <summary>
    /// First time setup wizard
    /// </summary>
    private void RunFirstTimeSetup()
    {
        // Step 1: Select folder
        PromptForScreenshotsFolder();
        
        if (!_settingsManager.HasScreenshotsPath)
        {
            // User cancelled, show warning
            MessageBox.Show(
                "You need to select a Screenshots folder for the app to work.\n\nRight-click the icon and select 'Select Screenshots folder...'",
                "Auto Delete Screenshot",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }
        
        // Step 2: Ask to create Desktop shortcut
        if (!ShortcutManager.Exists())
        {
            var result = MessageBox.Show(
                "Would you like to create a Desktop shortcut for easy access?",
                "Auto Delete Screenshot - Setup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            
            if (result == DialogResult.Yes)
            {
                if (ShortcutManager.Create())
                {
                    _trayIcon.ShowBalloonTip(
                        2000,
                        "📌 Shortcut Created",
                        "Desktop shortcut has been created",
                        ToolTipIcon.Info
                    );
                }
            }
        }
        
        // Initialize services after setup
        InitializeServices();
        
        _trayIcon.ShowBalloonTip(
            3000,
            "✅ Setup Complete",
            "Auto Delete Screenshot is ready to use!",
            ToolTipIcon.Info
        );
    }
    
    /// <summary>
    /// Initialize or reinitialize services
    /// </summary>
    private void InitializeServices()
    {
        if (!_settingsManager.HasScreenshotsPath)
            return;
            
        // Dispose old services if exists
        _screenshotWatcher?.Dispose();
        _fileCleanupService?.Dispose();
        
        // Initialize ScreenshotWatcher
        _screenshotWatcher = new ScreenshotWatcher(
            () => _deleteAfterMinutes,
            OnNewScreenshot
        );
        
        // Initialize FileCleanupService - scan every 60 seconds
        _fileCleanupService = new FileCleanupService(60);
    }
    
    /// <summary>
    /// Handle new screenshot event
    /// </summary>
    private void OnNewScreenshot(string fileName)
    {
        if (_showToast)
        {
            string timeText = _deleteAfterMinutes switch
            {
                15 => "15 minutes",
                30 => "30 minutes",
                60 => "1 hour",
                1440 => "24 hours",
                _ => $"{_deleteAfterMinutes} minutes"
            };
            
            // Show balloon tip
            _trayIcon.ShowBalloonTip(
                3000,
                "📷 Auto Delete Screenshot",
                $"Screenshot will be deleted in {timeText}",
                ToolTipIcon.Info
            );
        }
    }

    /// <summary>
    /// Load icon from Embedded Resource or create default
    /// </summary>
    private Icon LoadIcon()
    {
        try
        {
            // Load from Embedded Resource
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "AutoDeleteScreenshot.Resources.icon.png";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var bitmap = new Bitmap(stream);
                IntPtr hIcon = bitmap.GetHicon();
                Icon icon = Icon.FromHandle(hIcon);
                
                // Add cleanup handler for this specific icon instance if needed, 
                // but Icon.FromHandle wrapper usually manages its own copy. 
                // However, bitmap.GetHicon() creates a NEW handle that MUST be destroyed.
                // The Icon.FromHandle creates a managed wrapper around it.
                // Best practice: Clone the icon and destroy original handle.
                Icon clonedIcon = (Icon)icon.Clone();
                DestroyIcon(hIcon);
                return clonedIcon;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading icon: {ex.Message}");
        }
        
        // Create default icon if loading fails
        return CreateDefaultIcon();
    }

    /// <summary>
    /// Create default blue icon (High Resolution)
    /// </summary>
    private Icon CreateDefaultIcon()
    {
        // Create 64x64 icon for better scaling
        var bitmap = new Bitmap(64, 64);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            
            using var brush = new SolidBrush(Color.FromArgb(0, 120, 215)); // Windows blue
            g.FillEllipse(brush, 4, 4, 56, 56);
            
            using var whiteBrush = new SolidBrush(Color.White);
            // Clock hands
            g.FillRectangle(whiteBrush, 28, 14, 8, 22); // Vertical
            g.FillRectangle(whiteBrush, 28, 30, 20, 8); // Horizontal
        }
        IntPtr hIcon = bitmap.GetHicon();
        Icon icon = Icon.FromHandle(hIcon);
        Icon clonedIcon = (Icon)icon.Clone();
        DestroyIcon(hIcon);
        return clonedIcon;
    }

    /// <summary>
    /// Handle delete time change
    /// </summary>
    private void OnDeleteTimeChanged(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item && item.Tag is int minutes)
        {
            _deleteAfterMinutes = minutes;
            UpdateMenuCheckmarks();
            UpdateTooltip();
            
            // Save setting
            _settingsManager.DeleteAfterMinutes = minutes;
        }
    }

    /// <summary>
    /// Handle toast toggle
    /// </summary>
    private void OnShowToastChanged(object? sender, EventArgs e)
    {
        _showToast = _menuShowToast.Checked;
        // Save setting
        _settingsManager.ShowToast = _showToast;
    }

    /// <summary>
    /// Handle select folder menu click
    /// </summary>
    private void OnSelectFolder(object? sender, EventArgs e)
    {
        string? selectedPath = PathHelper.PromptForFolder();
        
        if (!string.IsNullOrEmpty(selectedPath))
        {
            PathHelper.SetScreenshotsPath(selectedPath);
            
            _trayIcon.ShowBalloonTip(
                3000,
                "📂 Folder Selected",
                $"Watching: {selectedPath}",
                ToolTipIcon.Info
            );
            
            // Reinitialize services with new path
            InitializeServices();
        }
    }
    
    /// <summary>
    /// Handle create shortcut menu click
    /// </summary>
    private void OnCreateShortcut(object? sender, EventArgs e)
    {
        if (ShortcutManager.Exists())
        {
            MessageBox.Show(
                "Desktop shortcut already exists!",
                "Auto Delete Screenshot",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            return;
        }
        
        if (ShortcutManager.Create())
        {
            _trayIcon.ShowBalloonTip(
                2000,
                "📌 Shortcut Created",
                "Desktop shortcut has been created",
                ToolTipIcon.Info
            );
        }
        else
        {
            MessageBox.Show(
                "Failed to create Desktop shortcut.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    /// <summary>
    /// Handle startup setting change
    /// </summary>
    private void OnStartupChanged(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item)
        {
            bool success;
            if (item.Checked)
            {
                success = StartupManager.Enable();
                if (success)
                {
                    _trayIcon.ShowBalloonTip(
                        2000,
                        "🚀 Enabled",
                        "App will start with Windows",
                        ToolTipIcon.Info
                    );
                }
            }
            else
            {
                success = StartupManager.Disable();
                if (success)
                {
                    _trayIcon.ShowBalloonTip(
                        2000,
                        "🚀 Disabled",
                        "App will not start with Windows",
                        ToolTipIcon.Info
                    );
                }
            }

            if (!success)
            {
                item.Checked = !item.Checked; // Revert checkbox on error
                MessageBox.Show(
                    "Cannot change startup settings.\nTry running the app as Administrator.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }

    /// <summary>
    /// Show folder selection dialog (used for first run)
    /// </summary>
    private void PromptForScreenshotsFolder()
    {
        string? selectedPath = PathHelper.PromptForFolder();
        
        if (!string.IsNullOrEmpty(selectedPath))
        {
            PathHelper.SetScreenshotsPath(selectedPath);
        }
    }



    /// <summary>
    /// Update checkmarks for menu items
    /// </summary>
    private void UpdateMenuCheckmarks()
    {
        _menuNoDelete.Checked = _deleteAfterMinutes == 0;
        _menu15Min.Checked = _deleteAfterMinutes == 15;
        _menu30Min.Checked = _deleteAfterMinutes == 30;
        _menu1Hour.Checked = _deleteAfterMinutes == 60;
        _menu24Hours.Checked = _deleteAfterMinutes == 1440;
    }

    /// <summary>
    /// Update tray icon tooltip
    /// </summary>
    private void UpdateTooltip()
    {
        string timeText = _deleteAfterMinutes switch
        {
            0 => "No auto-delete",
            15 => "15 min",
            30 => "30 min",
            60 => "1 hour",
            1440 => "24 hours",
            _ => $"{_deleteAfterMinutes} min"
        };
        _trayIcon.Text = $"Auto Delete Screenshot - {timeText}";
    }

    /// <summary>
    /// Handle Exit button click
    /// </summary>
    private void OnExit(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    /// <summary>
    /// Get current deletion time
    /// </summary>
    public int DeleteAfterMinutes => _deleteAfterMinutes;

    /// <summary>
    /// Is toast enabled
    /// </summary>
    public bool ShowToast => _showToast;

    /// <summary>
    /// Cleanup on application exit
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fileCleanupService?.Dispose();
            _screenshotWatcher?.Dispose();
            _trayIcon?.Dispose();
            _contextMenu?.Dispose();
        }
        base.Dispose(disposing);
    }
}
