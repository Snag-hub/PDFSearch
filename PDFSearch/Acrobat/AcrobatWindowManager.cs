﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDFSearch.Acrobat;

internal class AcrobatWindowManager
{
    private readonly string _launchDirectory = string.Empty;

    public AcrobatWindowManager(string launchDirectory)
    {
        _launchDirectory = launchDirectory;
    }

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

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public IntPtr acrobatHandle = IntPtr.Zero;

    // EnumWindows callback
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // Constants for ShowWindow
    private const int SW_SHOWMAXIMIZED = 3;

    public void FindOrLaunchAcrobatWindow()
    {
        try
        {
            // Load configuration to get the file name
            ConfigManager config = ConfigManager.LoadConfig();
            if (config == null || string.IsNullOrWhiteSpace(config.StartFile))
            {
                MessageBox.Show("Configuration not found or StartFile is not specified.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string fileName = config.StartFile;
            string filePath = Path.Combine(_launchDirectory, fileName);

            if (!File.Exists(filePath))
            {
                MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // List of potential Acrobat installation paths
            string[] possiblePaths =
            {
                @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
                @"C:\Program Files (x86)\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
                @"C:\Program Files\Adobe\Reader DC\Reader\AcroRd32.exe",
                @"C:\Program Files (x86)\Adobe\Reader DC\Reader\AcroRd32.exe"
            };

            // Find the first valid path
            string acrobatPath = possiblePaths.FirstOrDefault(File.Exists);
            if (acrobatPath == null)
            {
                MessageBox.Show("Adobe Acrobat or Reader is not installed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check if Acrobat process is running
            var acrobatProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(acrobatPath)).FirstOrDefault();
            if (acrobatProcess != null)
            {
                // Acrobat is already running, open the file in the running instance
                Process.Start(acrobatPath, $"\"{filePath}\"");
            }
            else
            {
                // Acrobat is not running, start a new instance
                var process = Process.Start(acrobatPath, $"\"{filePath}\"");
                if (process == null)
                {
                    MessageBox.Show("Failed to start Adobe Acrobat.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Allow some time for Acrobat to initialize
                Thread.Sleep(5000);
            }

            // Find the Acrobat window and maximize it
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
