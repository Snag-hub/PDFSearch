using PDFSearch;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

public class WindowManipulation
{
    // Import SetWindowPos from user32.dll to control window position
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int width, int height, uint flags);

    // Constants for SetWindowPos function
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
}


public class PdfOpener
{
    #region This works without hightlighting
    public static void OpenPdfAtPage(string filePath, int pageNumber, string rootPath)
    {
        try
        {
            // Load configuration to get the PDF opener path
            ConfigManager config = ConfigManager.LoadConfig(rootPath);
            if (config == null || string.IsNullOrWhiteSpace(config.PdfOpener))
            {
                MessageBox.Show("Configuration not found or PDF opener path is not specified.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Get the PDF opener path from the configuration
            string pdfOpenerPath = config.PdfOpener;

            // If the configured path doesn't exist, try default paths
            if (!File.Exists(pdfOpenerPath))
            {
                string[] possiblePaths =
                [
                    @"C:\Program Files (x86)\Adobe\Acrobat 11.0\Acrobat\Acrobat.exe",
                    @"C:\Program Files\Adobe\Acrobat 11.0\Acrobat\Acrobat.exe",
                    @"C:\Program Files (x86)\Adobe\Acrobat 10.0\Acrobat\Acrobat.exe",
                    @"C:\Program Files\Adobe\Acrobat 10.0\Acrobat\Acrobat.exe",
                    @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
                    @"C:\Program Files (x86)\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
                    @"C:\Program Files\Adobe\Reader DC\Reader\Acrobat.exe",
                    @"C:\Program Files (x86)\Adobe\Reader DC\Reader\Acrobat.exe"
                ];

                // Find the first valid path from the default paths
                pdfOpenerPath = possiblePaths.FirstOrDefault(File.Exists);
                if (pdfOpenerPath == null)
                {
                    MessageBox.Show("No valid PDF opener found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Build the command-line arguments to open the file at a specific page
            string arguments = $"/A \"page={pageNumber}\" \"{filePath}\"";

            // Check for running instances of the PDF opener
            var existingProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(pdfOpenerPath));
            if (existingProcesses.Length > 0)
            {
                // An instance of the PDF opener is already running, send the command-line arguments
                Process.Start(pdfOpenerPath, arguments);
            }
            else
            {
                // Start a new instance of the PDF opener
                var process = Process.Start(pdfOpenerPath, arguments);
                if (process == null)
                {
                    MessageBox.Show("Failed to start the PDF opener.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    #endregion

    public static void OpenPdfAtPage(string filePath, int pageNumber, string searchTerm, string rootPath)
    {
        try
        {
            // Load configuration to get the PDF opener path
            ConfigManager config = ConfigManager.LoadConfig(rootPath);
            if (config == null || string.IsNullOrWhiteSpace(config.PdfOpener))
            {
                MessageBox.Show("Configuration not found or PDF opener path is not specified.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Get the PDF opener path from the configuration
            string pdfOpenerPath = config.PdfOpener;

            // If the configured path doesn't exist, try default paths
            if (!File.Exists(pdfOpenerPath))
            {
                string[] possiblePaths =
                [
                    @"C:\Program Files (x86)\Adobe\Acrobat 11.0\Acrobat\Acrobat.exe",
                    @"C:\Program Files\Adobe\Acrobat 11.0\Acrobat\Acrobat.exe",
                    @"C:\Program Files (x86)\Adobe\Acrobat 10.0\Acrobat\Acrobat.exe",
                    @"C:\Program Files\Adobe\Acrobat 10.0\Acrobat\Acrobat.exe",
                    @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
                    @"C:\Program Files (x86)\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
                    @"C:\Program Files\Adobe\Reader DC\Reader\Acrobat.exe",
                    @"C:\Program Files (x86)\Adobe\Reader DC\Reader\Acrobat.exe"
                ];

                // Find the first valid path from the default paths
                pdfOpenerPath = possiblePaths.FirstOrDefault(File.Exists);
                if (pdfOpenerPath == null)
                {
                    MessageBox.Show("No valid PDF opener found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Create a temporary JavaScript file to highlight the search term
            string jsFilePath = Path.GetTempFileName();
            File.WriteAllText(jsFilePath, $"this.searchQuery = \"{searchTerm}\"; this.highlightText(this.searchQuery);");

            // Build the command-line arguments to open the file at a specific page and execute the JavaScript
            string arguments = $"/A \"page={pageNumber}&nameddest=page={pageNumber}&exec=app.execMenuItem('RunJavaScript', '{jsFilePath}')\" \"{filePath}\"";

            // Check for running instances of the PDF opener
            var existingProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(pdfOpenerPath));
            if (existingProcesses.Length > 0)
            {
                // An instance of the PDF opener is already running, send the command-line arguments
                Process.Start(pdfOpenerPath, arguments);
            }
            else
            {
                // Start a new instance of the PDF opener
                var process = Process.Start(pdfOpenerPath, arguments);
                if (process == null)
                {
                    MessageBox.Show("Failed to start the PDF opener.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
