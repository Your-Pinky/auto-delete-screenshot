using System.Diagnostics;

namespace AutoDeleteScreenshot;

static class Program
{
    /// <summary>
    /// Application entry point
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Set process priority to low to avoid impacting other applications
        try
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch
        {
            // Ignore if unable to set priority
        }

        // Allow only one instance to run
        using var mutex = new Mutex(true, "AutoDeleteScreenshot_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "Auto Delete Screenshot is already running!",
                "Info",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            return;
        }

        // Initialize Windows Forms
        ApplicationConfiguration.Initialize();
        
        // Run application with TrayApplicationContext
        Application.Run(new TrayApplicationContext());
    }
}
