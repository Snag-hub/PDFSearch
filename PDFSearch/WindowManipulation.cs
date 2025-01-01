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
            //const string adobeReaderPath = @"C:\Program Files (x86)\Adobe\Acrobat 10.0\Acrobat\Acrobat.exe"; // Path to Adobe Acrobat Pro
            const string adobeReaderPath = @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe";
            var arguments = $"/A \"page={pageNumber}\" \"{filePath}\""; // Open specific page

            IntPtr acrobatHandle = IntPtr.Zero;
            var existingProcesses = Process.GetProcessesByName("Acrobat");

            if (existingProcesses.Length > 0)
            {
                // Acrobat Pro is already running, reuse the existing instance
                acrobatHandle = existingProcesses[1].MainWindowHandle;

                // Open the specified file and page in the already running instance
                Process.Start(adobeReaderPath, arguments);
            }
            else if (File.Exists(adobeReaderPath))
            {
                // Start a new Acrobat Pro process
                var process = Process.Start(adobeReaderPath, arguments);
                if (process != null)
                {
                    int retries = 10;
                    while (retries > 0 && acrobatHandle == IntPtr.Zero)
                    {
                        Thread.Sleep(500); // Wait for the process to initialize
                        acrobatHandle = process.MainWindowHandle;
                        retries--;
                    }
                }
            }
            else
            {
                // Fallback if Acrobat Pro is not found (open with the default PDF viewer)
                Process.Start(filePath);
                return;
            }

            //// Proceed with window manipulation if a valid handle is found
            //if (acrobatHandle != IntPtr.Zero)
            //{
            //    // Get the current form's size and position
            //    var form = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;

            //    if (form != null)
            //    {
            //        int formWidth = form.Width;
            //        int formHeight = form.Height;
            //        int formLeft = form.Left;
            //        int formTop = form.Top;

            //        // Set Acrobat's size and position
            //        int acrobatWidth = 800;  // Adjust as needed
            //        int acrobatHeight = formHeight;  // Match the height of the form
            //        int acrobatLeft = formLeft + formWidth;  // Place it next to the form
            //        int acrobatTop = formTop;

            //        // Move Acrobat window to position beside the form
            //        WindowManipulation.SetWindowPos(
            //            acrobatHandle,
            //            IntPtr.Zero,
            //            acrobatLeft,
            //            acrobatTop,
            //            acrobatWidth,
            //            acrobatHeight,
            //            WindowManipulation.SWP_NOZORDER | WindowManipulation.SWP_NOACTIVATE);
            //    }
            //}
            //else
            //{
            //    MessageBox.Show("Unable to find Acrobat window.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
