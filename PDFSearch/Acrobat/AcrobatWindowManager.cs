using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PDFSearch.Acrobat;

internal partial class AcrobatWindowManager(string launchDirectory)
{
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    public IntPtr AcrobatHandle { get; private set; } = IntPtr.Zero;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public const int SW_SHOWMAXIMIZED = 3;
    public const int SW_RESTORE = 9;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_SHOWWINDOW = 0x0040;
    public const uint SWP_FRAMECHANGED = 0x0020;

    public void FindOrLaunchAcrobatWindow()
    {
        var config = ConfigManager.LoadConfig(launchDirectory);
        try
        {
            // Validate configuration
            if (config == null || string.IsNullOrWhiteSpace(config.StartFile) || string.IsNullOrWhiteSpace(config.PdfOpener))
            {
                Console.WriteLine("[ERROR] Invalid configuration: StartFile or PdfOpener missing.");
                MessageBox.Show("Configuration not found or StartFile/PdfOpener is not specified.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var fileName = config.StartFile;
            var filePath = Path.Combine(launchDirectory, fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[ERROR] StartFile not found: {filePath}");
                MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var acrobatPath = config.PdfOpener;
            if (!File.Exists(acrobatPath))
            {
                Console.WriteLine($"[ERROR] PdfOpener not found: {acrobatPath}");
                MessageBox.Show($"PDF opener not found: {acrobatPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Log config for debugging
            Console.WriteLine($"[DEBUG] Config: StartFile={filePath}, PdfOpener={acrobatPath}");

            // Get initial Acrobat processes
            var processName = Path.GetFileNameWithoutExtension(acrobatPath);
            var initialProcesses = Process.GetProcessesByName(processName);
            Console.WriteLine($"[DEBUG] Initial Acrobat processes: {initialProcesses.Length}");
            foreach (var proc in initialProcesses)
                Console.WriteLine($"[DEBUG] PID: {proc.Id}, MainWindowHandle: {proc.MainWindowHandle}, HasExited: {proc.HasExited}");

            // Launch Acrobat
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = acrobatPath,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = true
            });

            if (process == null)
            {
                Console.WriteLine($"[ERROR] Failed to start Acrobat process: {acrobatPath}");
                MessageBox.Show("Failed to start PDF opener.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var launchedProcessId = process.Id;
            Console.WriteLine($"[INFO] Launched Acrobat process: {processName}, PID: {launchedProcessId}, HasExited: {process.HasExited}");

            // Collect all Acrobat processes (including existing and new)
            var allProcessIds = new List<int> { launchedProcessId };
            var currentProcesses = Process.GetProcessesByName(processName);
            foreach (var proc in currentProcesses)
            {
                if (!allProcessIds.Contains(proc.Id))
                    allProcessIds.Add(proc.Id);
                Console.WriteLine($"[DEBUG] Current Acrobat process: PID: {proc.Id}, MainWindowHandle: {proc.MainWindowHandle}, HasExited: {proc.HasExited}");
            }

            // Wait for MainWindowHandle across all Acrobat processes (up to 15 seconds)
            int waitSeconds = 15;
            for (int i = 0; i < waitSeconds; i++)
            {
                foreach (var pid in allProcessIds)
                {
                    try
                    {
                        var proc = Process.GetProcessById(pid);
                        if (!proc.HasExited && proc.MainWindowHandle != IntPtr.Zero)
                        {
                            StringBuilder windowText = new(256);
                            var length = GetWindowText(proc.MainWindowHandle, windowText, windowText.Capacity);
                            var title = length > 0 ? windowText.ToString() : "(no title)";
                            Console.WriteLine($"[INFO] Found candidate window: {title}, hWnd: {proc.MainWindowHandle}, PID: {pid}");
                            // Prioritize windows with PDF filename
                            if (title.Contains(Path.GetFileNameWithoutExtension(fileName), StringComparison.OrdinalIgnoreCase))
                            {
                                AcrobatHandle = proc.MainWindowHandle;
                                Console.WriteLine($"[INFO] Selected MainWindowHandle: {title}, hWnd: {proc.MainWindowHandle}, PID: {pid}");
                                ShowWindow(AcrobatHandle, SW_RESTORE);
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WARNING] Error checking process PID {pid}: {ex.Message}");
                    }
                }
                Console.WriteLine($"[INFO] Waiting for Acrobat MainWindowHandle ({i + 1}/{waitSeconds} seconds)");
                Thread.Sleep(1000);
            }

            // Fallback to EnumWindows across all Acrobat process IDs
            var retries = 5;
            while (retries > 0 && AcrobatHandle == IntPtr.Zero)
            {
                Console.WriteLine($"[INFO] MainWindowHandle not found, searching windows by PIDs, retries left: {retries}");
                EnumWindows((hWnd, lParam) =>
                {
                    GetWindowThreadProcessId(hWnd, out var processId);
                    if (!allProcessIds.Contains(processId))
                        return true;

                    StringBuilder windowText = new(256);
                    var length = GetWindowText(hWnd, windowText, windowText.Capacity);
                    if (length == 0)
                    {
                        var error = Marshal.GetLastWin32Error();
                        if (error != 0)
                            Console.WriteLine($"[WARNING] GetWindowText failed for hWnd {hWnd}: Error {error}");
                        return true;
                    }

                    var title = windowText.ToString();
                    Console.WriteLine($"[INFO] Checking window: {title}, hWnd: {hWnd}, PID: {processId}");
                    // Prioritize windows with PDF filename
                    if (title.Contains(Path.GetFileNameWithoutExtension(fileName), StringComparison.OrdinalIgnoreCase))
                    {
                        AcrobatHandle = hWnd;
                        ShowWindow(hWnd, SW_RESTORE);
                        Console.WriteLine($"[INFO] Selected Acrobat window: {title}, hWnd: {hWnd}, PID: {processId}");
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);

                if (AcrobatHandle == IntPtr.Zero)
                {
                    retries--;
                    Thread.Sleep(2000);
                }
            }

            // Final fallback: Take any Acrobat window
            if (AcrobatHandle == IntPtr.Zero)
            {
                Console.WriteLine("[INFO] No PDF-specific window found, trying any Acrobat window");
                EnumWindows((hWnd, lParam) =>
                {
                    GetWindowThreadProcessId(hWnd, out var processId);
                    if (!allProcessIds.Contains(processId))
                        return true;

                    StringBuilder windowText = new(256);
                    var length = GetWindowText(hWnd, windowText, windowText.Capacity);
                    if (length == 0)
                        return true;

                    var title = windowText.ToString();
                    Console.WriteLine($"[INFO] Checking fallback window: {title}, hWnd: {hWnd}, PID: {processId}");
                    AcrobatHandle = hWnd;
                    ShowWindow(hWnd, SW_RESTORE);
                    Console.WriteLine($"[INFO] Selected fallback Acrobat window: {title}, hWnd: {hWnd}, PID: {processId}");
                    return false;
                }, IntPtr.Zero);
            }

            if (AcrobatHandle == IntPtr.Zero)
            {
                Console.WriteLine("[ERROR] Adobe Acrobat window not found after retries.");
                MessageBox.Show("Adobe Acrobat window not found. Please ensure Adobe Acrobat is installed and the configured PDF opener is correct.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] FindOrLaunchAcrobatWindow failed: {ex.Message}");
            MessageBox.Show($"Error finding or launching the PDF opener: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public static void EnsureAcrobatClosed()
    {
        foreach (var process in Process.GetProcessesByName("Acrobat"))
        {
            try
            {
                Console.WriteLine($"[INFO] Closing Acrobat process, PID: {process.Id}");
                process.Kill();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error closing Acrobat process: {ex.Message}");
                MessageBox.Show($"Error closing Acrobat process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}