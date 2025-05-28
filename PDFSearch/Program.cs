namespace FindInPDFs;

using Serilog;
using System;
using System.IO;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Configure Serilog
        string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FindInPDFs",
            "logs",
            "log-.txt");

        // Ensure the logs directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(logPath));

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information() // Log Information and above
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day, // New file each day
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB limit per file
                retainedFileCountLimit: 7, // Keep logs for 7 days
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        try
        {
            Log.Information("FindInPDFs application started.");

            string folderPath;

            if (args.Length > 0)
            {
                folderPath = args[0]; // Path from the context menu
                Log.Information("Folder path received from context menu: {FolderPath}", folderPath);
            }
            else
            {
                folderPath = Environment.CurrentDirectory; // Default fallback
                Log.Information("No arguments provided, using default folder path: {FolderPath}", folderPath);
            }

            var path = folderPath.Trim();
            Log.Debug("Trimmed folder path: {Path}", path);

            if (Directory.Exists(path))
            {
                Log.Information("Opening location: {FolderPath}", path);
                Application.Run(new PopupForm(path));
            }
            else
            {
                Log.Error("Invalid folder path: {FolderPath}", path);
                MessageBox.Show($"Invalid folder path: {folderPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An unhandled exception occurred during application startup.");
            MessageBox.Show($"An error occurred: {ex.Message}\nApp will Crash!...", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw; // Optional: rethrow to ensure the app exits
        }
        finally
        {
            Log.Information("FindInPDFs application stopped.");
            Log.CloseAndFlush();
        }
    }
}