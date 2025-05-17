using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using PDFSearch;

namespace FindInPDFs.Acrobat;

internal class AcrobatWindowManager(string launchDirectory)
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public IntPtr acrobatHandle = IntPtr.Zero;

    // EnumWindows callback
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // Constants for ShowWindow
    private const int SW_SHOWMAXIMIZED = 3;

    #region THis is working but not using it...
    //public void FindOrLaunchAcrobatWindow()
    //{
    //    try
    //    {
    //        // Load configuration to get the file name
    //        ConfigManager config = ConfigManager.LoadConfig();
    //        if (config == null || string.IsNullOrWhiteSpace(config.StartFile))
    //        {
    //            MessageBox.Show("Configuration not found or StartFile is not specified.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    //            return;
    //        }

    //        string fileName = config.StartFile;
    //        string filePath = Path.Combine(_launchDirectory, fileName);

    //        if (!File.Exists(filePath))
    //        {
    //            MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    //            return;
    //        }

    //        // List of potential Acrobat installation paths
    //        string[] possiblePaths =
    //        {
    //            @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
    //            @"C:\Program Files (x86)\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
    //            @"C:\Program Files\Adobe\Reader DC\Reader\AcroRd32.exe",
    //            @"C:\Program Files (x86)\Adobe\Reader DC\Reader\AcroRd32.exe"
    //        };

    //        // Find the first valid path
    //        string acrobatPath = possiblePaths.FirstOrDefault(File.Exists);
    //        if (acrobatPath == null)
    //        {
    //            MessageBox.Show("Adobe Acrobat or Reader is not installed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    //            return;
    //        }

    //        // Check if Acrobat process is running
    //        var acrobatProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(acrobatPath)).FirstOrDefault();
    //        if (acrobatProcess != null)
    //        {
    //            // Acrobat is already running, open the file in the running instance
    //            Process.Start(acrobatPath, $"\"{filePath}\"");
    //        }
    //        else
    //        {
    //            // Acrobat is not running, start a new instance
    //            var process = Process.Start(acrobatPath, $"\"{filePath}\"");
    //            if (process == null)
    //            {
    //                MessageBox.Show("Failed to start Adobe Acrobat.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    //                return;
    //            }

    //            // Allow some time for Acrobat to initialize
    //            Thread.Sleep(5000);
    //        }

    //        // Find the Acrobat window and maximize it
    //        EnumWindows((hWnd, lParam) =>
    //        {
    //            StringBuilder windowText = new(256);
    //            GetWindowText(hWnd, windowText, windowText.Capacity);

    //            if (windowText.ToString().Contains("Adobe Acrobat") || windowText.ToString().Contains("Acrobat"))
    //            {
    //                IntPtr acrobatHandle = hWnd;
    //                ShowWindow(acrobatHandle, SW_SHOWMAXIMIZED);
    //                return false; // Stop further enumeration
    //            }

    //            return true;
    //        }, IntPtr.Zero);
    //    }
    //    catch (Exception ex)
    //    {
    //        MessageBox.Show($"Error finding or launching Acrobat: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    //    }
    //}
    #endregion

    public void FindOrLaunchAcrobatWindow()
    {
        try
        {
            // Load configuration to get the StartFile and PdfOpener
            var config = ConfigManager.LoadConfig(launchDirectory);
            if (config == null || string.IsNullOrWhiteSpace(config.StartFile) || string.IsNullOrWhiteSpace(config.PdfOpener))
            {
                MessageBox.Show("Configuration not found or StartFile/PdfOpener is not specified.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string fileName = config.StartFile;
            string filePath = Path.Combine(launchDirectory, fileName);

            if (!File.Exists(filePath))
            {
                MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Get the PDF opener path from the configuration
            string acrobatPath = config.PdfOpener;

            // Check if the specified PDF opener exists
            if (!File.Exists(acrobatPath))
            {
                MessageBox.Show($"PDF opener not found: {acrobatPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check if the PDF opener process is running
            var acrobatProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(acrobatPath)).FirstOrDefault();
            if (acrobatProcess != null)
            {
                // PDF opener is already running, open the file in the running instance
                Process.Start(acrobatPath, $"\"{filePath}\"");
            }
            else
            {
                // PDF opener is not running, start a new instance
                var process = Process.Start(acrobatPath, $"\"{filePath}\"");
                if (process == null)
                {
                    MessageBox.Show("Failed to start the PDF opener.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Allow some time for the PDF opener to initialize
                Thread.Sleep(5000);
            }

            // Find the PDF opener window and maximize it
            EnumWindows((hWnd, lParam) =>
            {
                StringBuilder windowText = new(256);
                GetWindowText(hWnd, windowText, windowText.Capacity);

                if (windowText.ToString().Contains("Adobe Acrobat") || windowText.ToString().Contains("Acrobat"))
                {
                    IntPtr acrobatHandle = hWnd;
                    ShowWindow(acrobatHandle, SW_SHOWMAXIMIZED);
                    return false; // Stop further enumeration
                }

                return true;
            }, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error finding or launching the PDF opener: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Finds an Adobe Acrobat window and maximizes it.
    /// </summary>
    /// <returns>The handle of the Acrobat window, or IntPtr.Zero if not found.</returns>
    public static void FindAndMaximizeAcrobatWindow()
    {
        EnumWindows((hWnd, lParam) =>
        {
            StringBuilder windowText = new(256);
            GetWindowText(hWnd, windowText, windowText.Capacity);

            if (windowText.ToString().Contains("Adobe Acrobat") || windowText.ToString().Contains("Acrobat"))
            {
                IntPtr acrobatHandle = hWnd;
                ShowWindow(acrobatHandle, SW_SHOWMAXIMIZED);
                return false; // Stop further enumeration
            }

            return true;
        }, IntPtr.Zero);

    }

    public static void EnsureAcrobatClosed()
    {
        foreach (var process in Process.GetProcessesByName("Acrobat"))
        {
            try
            {
                process.Kill();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error closing Acrobat process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}