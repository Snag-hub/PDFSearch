using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using PDFSearch.BackgroundPathFinder;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace PDFSearch;

public partial class SearchInPDFs : Form
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    private IntPtr acrobatHandle = IntPtr.Zero;
    private readonly string _launchDirectory;

    // make object of lucene searcher
    private readonly LuceneSearcher LuceneSearcher = new();
    private readonly PdfPathBackgroundService _pdfPathBackgroundService;


    public SearchInPDFs(string launchDirectory)
    {
        _launchDirectory = launchDirectory;
        InitializeComponent();

        // Add event handlers for TreeView redrawing after expand/collapse
        treeVwResult.AfterExpand += (s, e) => treeVwResult.Invalidate();
        treeVwResult.AfterCollapse += (s, e) => treeVwResult.Invalidate();

        // Initialize the logger (you can configure it as per your requirements)
        ILogger<PdfPathBackgroundService> logger = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        }).CreateLogger<PdfPathBackgroundService>();

        // Initialize the IAcrobatService
        IAcrobatService acrobatService = new AcrobatService(); // Use your actual service

        // Initialize the PdfPathBackgroundService
        _pdfPathBackgroundService = new PdfPathBackgroundService(logger, acrobatService);

        // Add form load event handler
        this.Load += SearchInPDFs_Load;
        this.Load += async (s, e) => await ProcessIndexingInBackground();
    }

    private void SearchInPDFs_Load(object sender, EventArgs e)
    {
        // start filePath retriving service
        _pdfPathBackgroundService.Start();

        treeVwResult.DrawMode = TreeViewDrawMode.OwnerDrawText;
        treeVwResult.DrawNode += treeVwResult_DrawNode;
        Thread.Sleep(1000); // Wait for Acrobat to initialize
        ArrangeWindows();
    }

    // EnumWindows callback
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private void FindOrLaunchAcrobatWindow()
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

    private void ArrangeWindows()
    {
        try
        {
            FindOrLaunchAcrobatWindow();  // This method will now set the acrobatHandle

            if (acrobatHandle != IntPtr.Zero)
            {
                // Get screen dimensions
                int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;

                // Calculate window sizes (30/70 split)
                int yourAppWidth = (int)(screenWidth * 0.3);
                int acrobatWidth = screenWidth - yourAppWidth;  // Use remaining space

                const uint SWP_NOZORDER = 0x0004;
                const uint SWP_SHOWWINDOW = 0x0040;
                const uint flags = SWP_SHOWWINDOW | SWP_NOZORDER;

                // Position your app first (left side)
                SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, yourAppWidth, screenHeight, flags);

                // Position Acrobat (right side)
                SetWindowPos(acrobatHandle, IntPtr.Zero, yourAppWidth, 0, acrobatWidth, screenHeight, flags);
            }
            else
            {
                MessageBox.Show("Adobe Acrobat window not found.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error arranging windows: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    #region Unused Code for Search Event
    //private void BtnSearchText_Click(object sender, EventArgs e)
    //{
    //    string? filePath = _pdfPathBackgroundService.FilePath;
    //    filePath = RemoveFileName(filePath);

    //    if (!string.IsNullOrEmpty(filePath))
    //    {
    //        MessageBox.Show($"Current active PDF file path: {filePath}");
    //    }
    //    else
    //    {
    //        MessageBox.Show("No active PDF file found.");
    //    }

    //    try
    //    {
    //        // Clear previous results in the TreeView
    //        treeVwResult.Nodes.Clear();

    //        string searchTerm = txtSearchBox.Text.Trim();
    //        if (string.IsNullOrEmpty(searchTerm))
    //        {
    //            MessageBox.Show("Please enter a search term.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    //            return;
    //        }

    //        // Search the results using Lucene or any other search mechanism
    //        var results = LuceneSearcher.SearchInDirectory(searchTerm, _launchDirectory, filePath);

    //        if (results.Count > 0)
    //        {
    //            foreach (var result in results)
    //            {
    //                // Create the root node for each search result (just the snippet)
    //                TreeNode rootNode = new($"{result.Snippet} - FilePath: {result.RelativePath}")
    //                {
    //                    Tag = result  // Store the full result object in Tag property for reference
    //                };

    //                // Add the root node to the TreeView
    //                treeVwResult.Nodes.Add(rootNode);
    //            }
    //        }
    //        else
    //        {
    //            MessageBox.Show("No results found.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    //    }
    //}
    #endregion

    private void BtnSearchText_Click(object sender, EventArgs e)
    {
        string? filePath = _pdfPathBackgroundService.FilePath;
        filePath = RemoveFileName(filePath);

        if (!string.IsNullOrEmpty(filePath))
        {
            MessageBox.Show($"Current active PDF file path: {filePath}");
        }
        else
        {
            MessageBox.Show("No active PDF file found.");
        }

        try
        {
            // Clear previous results in the TreeView
            treeVwResult.Nodes.Clear();

            string searchTerm = txtSearchBox.Text.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("Please enter a search term.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Search the results using Lucene or any other search mechanism
            var results = LuceneSearcher.SearchInDirectory(searchTerm, _launchDirectory, filePath);

            if (results.Count > 0)
            {
                // Group results by directory
                var groupedResults = results
                    .GroupBy(r => Path.GetDirectoryName(r.FilePath))
                    .OrderBy(g => g.Key); // Sort by directory name

                foreach (var group in groupedResults)
                {
                    string fullDirectoryPath = group.Key;

                    // Apply abbreviation to the directory path
                    string abbreviatedPath = AbbreviateDirectoryPath(fullDirectoryPath);

                    // Create a directory-level node with the abbreviated path
                    var directoryNode = new TreeNode(abbreviatedPath)
                    {
                        Tag = fullDirectoryPath // Store the full directory path for reference
                    };

                    // Further group results by file path within the directory
                    var fileGroups = group
                        .GroupBy(r => r.FilePath)
                        .OrderBy(g => g.Key); // Sort by file name

                    foreach (var fileGroup in fileGroups)
                    {
                        string fileName = Path.GetFileName(fileGroup.Key);

                        // Create the file node
                        var fileNode = new TreeNode(fileName)
                        {
                            Tag = fileGroup.Key // Store the full file path for reference
                        };

                        // Add snippet and page number nodes for each result in the file group
                        foreach (var result in fileGroup)
                        {
                            string snippetText = result.Snippet ?? "No snippet available";
                            string pageText = result.PageNumber > 0 ? $"Page {result.PageNumber}" : "Page number not found";

                            var snippetNode = new TreeNode($"{pageText}: {snippetText}")
                            {
                                Tag = result // Optionally associate the SearchResult object here
                            };

                            fileNode.Nodes.Add(snippetNode);
                        }

                        // Add the file node to the directory node
                        directoryNode.Nodes.Add(fileNode);
                    }

                    // Add the directory node to the TreeView
                    treeVwResult.Nodes.Add(directoryNode);
                }

                treeVwResult.CollapseAll(); // Ensure all nodes are collapsed by default
            }
            else
            {
                MessageBox.Show("No results found.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string AbbreviateDirectoryPath(string fullPath, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(fullPath) || fullPath.Length <= maxLength)
            return fullPath;

        string root = Path.GetPathRoot(fullPath); // E.g., "E:\"
        string[] directories = fullPath.Substring(root.Length).Split(Path.DirectorySeparatorChar);

        // Handle edge cases where there's no middle section
        if (directories.Length <= 2)
            return fullPath;

        string middle = "...";
        string leaf = directories[^1]; // Last directory name or file name
        int remainingLength = maxLength - root.Length - leaf.Length - middle.Length - 2; // -2 for separator chars

        if (remainingLength <= 0)
            return $"{root}{middle}{Path.DirectorySeparatorChar}{leaf}";

        string middlePart = string.Join(Path.DirectorySeparatorChar.ToString(), directories.TakeWhile(d => d.Length <= remainingLength));
        return $"{root}{middle}{Path.DirectorySeparatorChar}{middlePart}{Path.DirectorySeparatorChar}{leaf}";
    }

    private void lstVwResult_DrawItem(object sender, DrawListViewItemEventArgs e)
    {
        // Set the default font and brush
        Font regularFont = e.Item.Font;
        Font boldFont = new Font(e.Item.Font, FontStyle.Bold);

        // Set the background color (light gray for non-selected items)
        if (e.Item.Selected)
        {
            e.Graphics.FillRectangle(Brushes.LightBlue, e.Bounds); // Selected item background
        }
        else
        {
            e.Graphics.FillRectangle(Brushes.White, e.Bounds); // Default background for non-selected items
        }

        // Get the item text
        string text = e.Item.Text;

        // Variables for drawing
        float x = e.Bounds.Left;
        float y = e.Bounds.Top;

        // Split text by <b> and </b> tags
        string[] parts = text.Split(["<b>", "</b>"], StringSplitOptions.None);
        bool isBold = false;

        // Draw each part
        foreach (string part in parts)
        {
            // Choose font based on whether the part is bold
            Font currentFont = isBold ? boldFont : regularFont;

            // Measure the size of the text to calculate the next drawing position
            SizeF textSize = e.Graphics.MeasureString(part, currentFont);

            // Draw the text with the correct font and color (Black or any other color)
            e.Graphics.DrawString(part, currentFont, Brushes.Black, x, y);

            // Move the drawing position to the right
            x += textSize.Width;

            // Toggle bold state after each segment
            isBold = !isBold;
        }

        // Draw the focus rectangle if the item is selected
        e.DrawFocusRectangle();

        // Dispose of the bold font
        boldFont.Dispose();
    }

    private void BtnSearchText_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar != (char)Keys.Enter) return;
        BtnSearchText.PerformClick();
        e.Handled = true;
    }

    #region This is unused code for treeNode double click
    //private void treeVwResult_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
    //{
    //    try
    //    {
    //        // Ensure a node is selected
    //        var selectedNode = e.Node;
    //        if (selectedNode != null)
    //        {
    //            // Retrieve the SearchResult (or equivalent object) stored in the Tag property
    //            if (selectedNode.Tag is SearchResult selectedResult)
    //            {
    //                var filePath = selectedResult.FilePath;
    //                var pageNumber = selectedResult.PageNumber;

    //                // Open the PDF at the specific page (or implement your own logic)
    //                PdfOpener.OpenPdfAtPage(filePath, pageNumber);
    //            }
    //            else
    //            {
    //                MessageBox.Show(@"Invalid search result.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    //            }
    //        }
    //        else
    //        {
    //            MessageBox.Show(@"Please select a valid search result.", @"No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        MessageBox.Show($@"Error: {ex.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    //    }
    //}
    #endregion

    private void treeVwResult_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
    {
        try
        {
            // Ensure a node is selected
            var selectedNode = e.Node;
            if (selectedNode == null)
            {
                MessageBox.Show(@"Please select a valid node.", @"No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if the node represents a search result or a directory
            if (selectedNode.Tag is SearchResult selectedResult)
            {
                // This is a file node
                var filePath = selectedResult.FilePath;
                var pageNumber = selectedResult.PageNumber;

                // Open the PDF at the specific page
                PdfOpener.OpenPdfAtPage(filePath, pageNumber);
            }
            // commented because not not needed in production
            //else if (selectedNode.Tag is string directoryPath)
            //{
            //    // This is a directory node
            //    MessageBox.Show($"Selected directory: {directoryPath}", @"Directory Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    // Optionally open the directory in file explorer
            //    System.Diagnostics.Process.Start("explorer.exe", directoryPath);
            //}
            else
            {
                MessageBox.Show(@"Invalid node selected.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($@"Error: {ex.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private void treeVwResult_DrawNode(object sender, DrawTreeNodeEventArgs e)
    {
        // Skip background drawing if bounds are invalid (can happen during expand/collapse)
        if (e.Bounds.IsEmpty) return;

        // Set the default font and brush
        Font regularFont = e.Node.TreeView.Font;
        Font boldFont = new(e.Node.TreeView.Font, FontStyle.Bold);

        // Check if the current node is selected
        bool isSelected = e.Node == e.Node.TreeView.SelectedNode;

        // Set the background color based on selection
        Brush backgroundBrush = isSelected ? Brushes.LightBlue : Brushes.White;
        e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

        // Draw the node text
        string text = e.Node.Text;

        // Variables for drawing
        float x = e.Bounds.Left + 5; // Add a small margin for text
        float y = e.Bounds.Top + (e.Bounds.Height - e.Node.TreeView.Font.Height) / 2; // Vertically center text

        // Split text by <b> and </b> tags
        string[] parts = text.Split(new[] { "<b>", "</b>" }, StringSplitOptions.None);
        bool isBold = false;

        foreach (string part in parts)
        {
            Font currentFont = isBold ? boldFont : regularFont;

            // Measure text size
            SizeF textSize = e.Graphics.MeasureString(part, currentFont);

            // Draw the text
            e.Graphics.DrawString(part, currentFont, Brushes.Black, x, y);

            // Advance x position
            x += textSize.Width;

            // Toggle bold state
            isBold = !isBold;
        }

        // Draw focus rectangle if selected
        if (isSelected)
            ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds);

        // Dispose of the bold font
        boldFont.Dispose();
    }


    private async Task ProcessIndexingInBackground()
    {
        try
        {
            UpdateStatus($"Indexing started for directory: {_launchDirectory}");
            await Task.Run(() => LuceneIndexer.IndexDirectory(_launchDirectory));
            UpdateStatus($"Indexing completed for directory: {_launchDirectory}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error during indexing: {ex.Message}");
        }
    }

    private void UpdateStatus(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => statusLabel.Text = message));
        }
        else
        {
            statusLabel.Text = message;
        }
    }

    private void SearchInPDFs_FormClosing(object sender, FormClosingEventArgs e)
    {
        _pdfPathBackgroundService.Stop();
        EnsureAcrobatClosed();
    }

    public static string RemoveFileName(string fullPath)
    {
        string path = string.Empty;
        if (!string.IsNullOrEmpty(fullPath))
        {
            path = Path.GetDirectoryName(fullPath);
            if (path != null && path.StartsWith("\\"))
            {
                string driveLetter = fullPath.Substring(0, fullPath.IndexOf(':') + 1);
                path = path.Substring(1).Insert(1, driveLetter + ":");
            }
        }
        return path;
    }

    private void EnsureAcrobatClosed()
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
