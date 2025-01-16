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
    public static void OpenPdfAtPage(string filePath, int pageNumber)
    {
        try
        {
            // Define the common paths for Adobe Acrobat or Reader
            string[] possiblePaths =
            {
                @"C:\Program Files (x86)\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
                @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
                @"C:\Program Files (x86)\Adobe\Reader DC\Reader\AcroRd32.exe",
                @"C:\Program Files\Adobe\Reader DC\Reader\AcroRd32.exe"
            };

            // Find the first valid path
            string adobeReaderPath = possiblePaths.FirstOrDefault(File.Exists);
            if (adobeReaderPath == null)
            {
                MessageBox.Show("Adobe Acrobat or Reader is not installed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Build the command-line arguments to open the file at a specific page
            string arguments = $"/A \"page={pageNumber}\" \"{filePath}\"";

            // Check for running instances of Acrobat or Reader
            var existingProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(adobeReaderPath));
            if (existingProcesses.Length > 0)
            {
                // An instance of Acrobat/Reader is already running, send the command-line arguments
                Process.Start(adobeReaderPath, arguments);
            }
            else
            {
                // Start a new instance of Acrobat/Reader
                var process = Process.Start(adobeReaderPath, arguments);
                if (process == null)
                {
                    MessageBox.Show("Failed to start Adobe Acrobat/Reader.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
