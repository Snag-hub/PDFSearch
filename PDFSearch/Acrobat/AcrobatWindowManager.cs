using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PDFSearch.Acrobat;

internal class AcrobatWindowManager(string launchDirectory)
{
    private readonly string _launchDirectory = launchDirectory;
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    public IntPtr acrobatHandle = IntPtr.Zero;
    // EnumWindows callback
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public void FindOrLaunchAcrobatWindow()
    {
        try
        {
            //EnsureAcrobatClosed(); // Ensure that Acrobat is not running before launching

            // Check if Acrobat process is running
            var acrobatProcess = Process.GetProcessesByName("Acrobat").FirstOrDefault();
            string indexFilePath = Path.Combine(_launchDirectory, "Index.pdf");

            if (acrobatProcess == null)
            {
                // Acrobat is not running, attempt to start it
                string acrobatPath = @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe"; // Adjust path if needed

                if (File.Exists(acrobatPath))
                {
                    if (File.Exists(indexFilePath))
                    {
                        Process.Start(acrobatPath, $"\"{indexFilePath}\""); // Pass the file path as an argument
                        Thread.Sleep(5000); // Wait for Acrobat to initialize
                    }
                    else
                    {
                        MessageBox.Show($"Index.pdf not found in the directory: {_launchDirectory}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Adobe Acrobat not found at the expected location.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                // Ensure that Acrobat opens Index.pdf even if it's already running
                if (File.Exists(indexFilePath))
                {
                    try
                    {
                        string acrobatPath = @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe"; // Adjust path if needed

                        if (File.Exists(acrobatPath))
                        {
                            // Use ProcessStartInfo to launch Adobe Acrobat with the file
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = acrobatPath,
                                Arguments = $"\"{indexFilePath}\"", // Pass the PDF file as an argument
                                UseShellExecute = false // Do not use shell execute
                            };

                            Process.Start(startInfo);
                        }
                        else
                        {
                            MessageBox.Show("Adobe Acrobat executable not found at the specified path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening Index.pdf with Acrobat: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"Index.pdf not found in the directory: {_launchDirectory}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }



            // Find the Acrobat window
            EnumWindows((hWnd, lParam) =>
            {
                StringBuilder windowText = new StringBuilder(256);
                GetWindowText(hWnd, windowText, windowText.Capacity);

                if (windowText.ToString().Contains("Adobe Acrobat") || windowText.ToString().Contains("Acrobat"))
                {
                    acrobatHandle = hWnd;
                    return false; // Stop further enumeration
                }

                return true;
            }, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error finding or launching Acrobat: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    
    public void EnsureAcrobatClosed()
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
