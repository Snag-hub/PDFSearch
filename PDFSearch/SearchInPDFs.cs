using Microsoft.Extensions.Logging;
using PDFSearch.Utilities;
using PDFSearch.Acrobat;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using FindInPDFs.Utilities;
using Lucene.Net.QueryParsers.Classic;

namespace PDFSearch;

public partial class SearchInPDFs : Form
{

    private readonly string _launchDirectory;
    private readonly AcrobatWindowManager acrobatWindowManager;
    private readonly LuceneSearcher _luceneSearcher = new();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private const int SW_SHOWMAXIMIZED = 3;
    private const int SW_SHOWNORMAL = 1;
    private const int SW_RESTORE = 9;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_FRAMECHANGED = 0x0020;

    // Static reference to keep track of the instance
    private static SearchInPDFs instance;

    public SearchInPDFs(string launchDirectory)
    {
        _launchDirectory = launchDirectory;
        InitializeComponent();
        InitializeDataGridView();
        this.KeyPreview = true;
        this.KeyPress += BtnSearchText_KeyPress;
        acrobatWindowManager = new AcrobatWindowManager(_launchDirectory);
        this.Load += SearchInPDFs_Load;
        TvSearchRange.AfterCheck += TvSearchRange_AfterCheck;
        instance = this;

    }

    public static SearchInPDFs Instance => instance;

    private void InitializeDataGridView()
    {
        dataGridViewResults.Columns.Clear();

        dataGridViewResults.Columns.Add("Fleet", "Fleet");
        dataGridViewResults.Columns.Add("Carriers", "Carriers");
        dataGridViewResults.Columns.Add("Vessel", "Vessel");
        dataGridViewResults.Columns.Add("Part", "Part");
        dataGridViewResults.Columns.Add("Manual", "Manual");
        dataGridViewResults.Columns.Add("PageNoWithContent", "PageNoWithContent");
        var fullPathColumn = new DataGridViewTextBoxColumn
        {
            Name = "FullPath",
            HeaderText = "FullPath",
            Visible = false // Hide the FullPath column
        };
        dataGridViewResults.Columns.Add(fullPathColumn);

        var pageNo = new DataGridViewTextBoxColumn
        {
            Name = "PageNo",
            HeaderText = "PageNo",
            Visible = false
        };
        dataGridViewResults.Columns.Add(pageNo);
    }




    private void SearchInPDFs_Load(object sender, EventArgs e)
    {
       txtSearchBox.Focus();
        var folderStructure = FolderManager.LoadFolderStructure(_launchDirectory);
        if (folderStructure is { Count: 0})
        {
            FolderManager.SaveFolderStructure(_launchDirectory);
            folderStructure = FolderManager.LoadFolderStructure(_launchDirectory);
        }

        AddDirectoriesToTreeView(_launchDirectory, folderStructure);
        if (TvSearchRange.Nodes.Count > 0)
        {
            TvSearchRange.Nodes[0].Expand();
        }

        BindFromToCombos();
        ShowHideResultGroupBox();

        acrobatWindowManager.FindOrLaunchAcrobatWindow();
        ArrangeWindows();
    }

    private void AddDirectoriesToTreeView(string launchDirectory, List<string>? folderStructure)
    {
        if (folderStructure == null || folderStructure.Count == 0)
        {
            return;
        }

        // Create the root node based on the E-Library starting point
        var rootFolderName = new DirectoryInfo(launchDirectory).Name;
        var rootNode = new TreeNode(rootFolderName)
        {
            Tag = launchDirectory
        };
        TvSearchRange.Nodes.Add(rootNode);

        // Create a dictionary to store nodes by their full paths
        var nodes = new Dictionary<string, TreeNode>
        {
            { launchDirectory, rootNode }
        };

        foreach (var path in folderStructure)
        {
            // Only add paths that are under the _launchDirectory
            if (!path.StartsWith(launchDirectory, StringComparison.OrdinalIgnoreCase)) continue;
            var relativePath = path[launchDirectory.Length..].TrimStart(Path.DirectorySeparatorChar);
            var parts = relativePath.Split(Path.DirectorySeparatorChar);
            var currentPath = launchDirectory;
            var parentNode = rootNode;

            foreach (var part in parts)
            {
                currentPath = Path.Combine(currentPath, part);

                if (!nodes.TryGetValue(currentPath, out var value))
                {
                    var newNode = new TreeNode(part)
                    {
                        Tag = currentPath
                    };

                    parentNode.Nodes.Add(newNode);

                    value = newNode;
                    nodes[currentPath] = value;
                }

                parentNode = value;
            }
        }
    }


    private static void CheckAllChildNodes(TreeNode parentNode, bool isChecked)
    {
        foreach (TreeNode childNode in parentNode.Nodes)
        {
            childNode.Checked = isChecked;
            if (childNode.Nodes.Count > 0)
            {
                CheckAllChildNodes(childNode, isChecked);
            }
        }
    }

    private List<string> GetSelectedDirectories(TreeNodeCollection nodes)
    {
        var selectedDirectories = new List<string>();

        foreach (TreeNode node in nodes)
        {
            // Only add the node if it's checked and all its child nodes are checked
            if (node.Checked && AreAllChildNodesChecked(node))
            {
                if (node.Tag is string fullPath)
                {
                    selectedDirectories.Add(fullPath);
                }
            }
            else
            {
                // Recursively check child nodes
                selectedDirectories.AddRange(GetSelectedDirectories(node.Nodes));
            }
        }

        return selectedDirectories.Distinct().ToList(); // Ensure there are no duplicates
    }

    private static bool AreAllChildNodesChecked(TreeNode parentNode)
    {
        return parentNode.Nodes.Cast<TreeNode>()
            .All(childNode => childNode.Checked && AreAllChildNodesChecked(childNode));
    }




    private void ShowHideResultGroupBox()
    {
        if (grpBoxSearchResult.Visible == false)
        {
            grpBoxSearchResult.Location = new Point(12, 12);
            grpBoxSearchResult.Visible = true;
            grpBoxSearch.Visible = false;
        }
        else
        {
            grpBoxSearchResult.Visible = false;
            grpBoxSearch.Visible = true;
        }
    }

    #region This is not in use for range based search

    //private void BtnSearchText_Click(object sender, EventArgs e)
    //{
    //    string? filePath = _pdfPathBackgroundService.FilePath;
    //    filePath = DirectoryUtils.RemoveFileName(filePath);

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

    //        ShowHideResultGroupBox();

    //        // Search the results using Lucene or any other search mechanism
    //        var results = LuceneSearcher.SearchInDirectory(searchTerm, _launchDirectory, filePath);

    //        if (results.Count > 0)
    //        {
    //            // Group results by directory
    //            var groupedResults = results
    //                .GroupBy(r => Path.GetDirectoryName(r.FilePath))
    //                .OrderBy(g => g.Key); // Sort by directory name

    //            foreach (var group in groupedResults)
    //            {
    //                string fullDirectoryPath = group.Key;

    //                // Apply abbreviation to the directory path
    //                string abbreviatedPath = DirectoryUtils.AbbreviateDirectoryPath(fullDirectoryPath);

    //                // Create a directory-level node with the abbreviated path
    //                var directoryNode = new TreeNode(abbreviatedPath)
    //                {
    //                    Tag = fullDirectoryPath // Store the full directory path for reference
    //                };

    //                // Further group results by file path within the directory
    //                var fileGroups = group
    //                    .GroupBy(r => r.FilePath)
    //                    .OrderBy(g => g.Key); // Sort by file name

    //                foreach (var fileGroup in fileGroups)
    //                {
    //                    string fileName = Path.GetFileName(fileGroup.Key);

    //                    // Create the file node
    //                    var fileNode = new TreeNode(fileName)
    //                    {
    //                        Tag = fileGroup.Key // Store the full file path for reference
    //                    };

    //                    // Add snippet and page number nodes for each result in the file group
    //                    foreach (var result in fileGroup)
    //                    {
    //                        string snippetText = result.Snippet ?? "No snippet available";
    //                        string pageText = result.PageNumber > 0 ? $"Page {result.PageNumber}" : "Page number not found";

    //                        var snippetNode = new TreeNode($"{pageText}: {snippetText}")
    //                        {
    //                            Tag = result // Optionally associate the SearchResult object here
    //                        };

    //                        fileNode.Nodes.Add(snippetNode);
    //                    }

    //                    // Add the file node to the directory node
    //                    directoryNode.Nodes.Add(fileNode);
    //                }

    //                // Add the directory node to the TreeView
    //                treeVwResult.Nodes.Add(directoryNode);
    //            }

    //            treeVwResult.CollapseAll(); // Ensure all nodes are collapsed by default
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
        var matchWord = chkWholeWord.Checked;
        var matchCase = chkCaseSensitive.Checked;
        string? filePath = null;

        try
        {
            // Clear previous results
            dataGridViewResults.Rows.Clear();

            var searchTerm = txtSearchBox.Text.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("Please enter a search term.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Get selected directories
            var selectedDirectories = GetSelectedDirectories(TvSearchRange.Nodes);
            if (!selectedDirectories.Any())
            {
                MessageBox.Show("Please select at least one node or child node in the Search Range.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Perform search across all subfolder indexes
            UpdateStatus("Searching...");
            var results =
                _luceneSearcher.SearchInDirectory(searchTerm, _launchDirectory, matchWord, matchCase, filePath);

            // Filter by selected directories
            var filteredResults = results
                .Where(r => selectedDirectories.Any(d => r.FilePath.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (filteredResults.Count == 0)
            {
                UpdateStatus("Search completed: No results found.");
                MessageBox.Show("No results found.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ShowHideResultGroupBox();
            foreach (var result in filteredResults)
            {
                var fullPath = result.FilePath;
                var relativePath = result.RelativePath;
                var splitFullPath = fullPath.Split(Path.DirectorySeparatorChar);
                var splitRelativePath = relativePath.Split('\\');

                var vesselIndexInFullPath = Array.IndexOf(splitFullPath, splitRelativePath[0]);
                var fleet = vesselIndexInFullPath >= 0 ? splitFullPath[vesselIndexInFullPath - 1] : "";
                var vessel = splitRelativePath.Length > 0 ? splitRelativePath[0] : "";
                var part = splitRelativePath.Length > 1 ? splitRelativePath[1] : "";
                var manual = Path.GetFileNameWithoutExtension(relativePath);
                var pageNoWithContent = result.PageNumber > 0
                    ? $"Page {result.PageNumber}: {result.Snippet}"
                    : result.Snippet;

                dataGridViewResults.Rows.Add(fleet, "", vessel, part, manual, pageNoWithContent, fullPath,
                    result.PageNumber);
            }

            dataGridViewResults.AutoResizeColumns();
            UpdateStatus($"Search completed: {filteredResults.Count} results found.");
        }
        catch (DirectoryNotFoundException ex)
        {
            UpdateStatus("Search failed: Index not found.");
            MessageBox.Show($"Index not found: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (ParseException ex)
        {
            UpdateStatus("Search failed: Invalid search term.");
            MessageBox.Show($"Invalid search term: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            UpdateStatus("Search failed: An error occurred.");
            MessageBox.Show($"Search error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnSearchText_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar != (char)Keys.Enter) return;
        BtnSearchText.PerformClick();
        e.Handled = true;
    }



    private void lstVwResult_DrawItem(object sender, DrawListViewItemEventArgs e)
    {
        // Set the default font and brush
        var regularFont = e.Item.Font;
        var boldFont = new Font(e.Item.Font, FontStyle.Bold);

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
        var text = e.Item.Text;

        // Variables for drawing
        float x = e.Bounds.Left;
        float y = e.Bounds.Top;

        // Split text by <b> and </b> tags
        var parts = text.Split(["<b>", "</b>"], StringSplitOptions.None);
        var isBold = false;

        // Draw each part
        foreach (var part in parts)
        {
            // Choose font based on whether the part is bold
            var currentFont = isBold ? boldFont : regularFont;

            // Measure the size of the text to calculate the next drawing position
            var textSize = e.Graphics.MeasureString(part, currentFont);

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


    private void treeVwResult_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
    {
        try
        {
            // Ensure a node is selected
            var selectedNode = e.Node;

            // Check if the node represents a search result or a directory
            if (selectedNode.Tag is SearchResult selectedResult)
            {
                // This is a file node
                var filePath = selectedResult.FilePath;
                var pageNumber = selectedResult.PageNumber;

                // Open the PDF at the specific page
                PdfOpener.OpenPdfAtPage(filePath, pageNumber, txtSearchBox.Text, _launchDirectory);
            }
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
        var regularFont = e.Node.TreeView.Font;
        Font boldFont = new(e.Node.TreeView.Font, FontStyle.Bold);

        // Check if the current node is selected
        var isSelected = e.Node == e.Node.TreeView.SelectedNode;

        // Set the background color based on selection
        var backgroundBrush = isSelected ? Brushes.LightBlue : Brushes.White;
        e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

        // Draw the node text
        var text = e.Node.Text;

        // Variables for drawing
        float x = e.Bounds.Left + 5; // Add a small margin for text
        float y = e.Bounds.Top + (e.Bounds.Height - e.Node.TreeView.Font.Height) / 2; // Vertically center text

        // Split text by <b> and </b> tags
        var parts = text.Split(new[] { "<b>", "</b>" }, StringSplitOptions.None);
        var isBold = false;

        foreach (var part in parts)
        {
            var currentFont = isBold ? boldFont : regularFont;

            // Measure text size
            var textSize = e.Graphics.MeasureString(part, currentFont);

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
        PopupForm popupForm = new(_launchDirectory);
        popupForm.Close();
        AcrobatWindowManager.EnsureAcrobatClosed();
    }

            private void ArrangeWindows()
    {
        try
        {
            var acrobatHandle = acrobatWindowManager.AcrobatHandle;

            if (acrobatHandle == IntPtr.Zero)
            {
                Console.WriteLine("[INFO] AcrobatHandle is zero, calling FindOrLaunchAcrobatWindow");
                acrobatWindowManager.FindOrLaunchAcrobatWindow();
                acrobatHandle = acrobatWindowManager.AcrobatHandle;
            }

            if (acrobatHandle != IntPtr.Zero)
            {
                // Log Acrobat window details
                StringBuilder windowText = new(256);
                int length = GetWindowText(acrobatHandle, windowText, windowText.Capacity);
                string title = length > 0 ? windowText.ToString() : "(no title)";
                bool isVisible = IsWindowVisible(acrobatHandle);
                Console.WriteLine($"[INFO] Arranging Acrobat window: {title}, hWnd: {acrobatHandle}, Visible: {isVisible}");

                if (Screen.PrimaryScreen == null)
                {
                    UpdateStatus("No primary screen detected.");
                    throw new InvalidOperationException("No primary screen detected.");
                }

                // Get screen dimensions
                var screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                var screenHeight = Screen.PrimaryScreen.WorkingArea.Height;

                // Calculate window sizes (25/75 split)
                var appWidth = screenWidth / 4; // 25%
                var acrobatWidth = screenWidth - appWidth; // 75%
                var acrobatX = appWidth; // Start at right of app

                Console.WriteLine($"[DEBUG] Screen: {screenWidth}x{screenHeight}, App: {appWidth}x{screenHeight}, Acrobat: {acrobatWidth}x{screenHeight} at X={acrobatX}");

                const uint flags = SWP_NOZORDER | SWP_SHOWWINDOW | SWP_FRAMECHANGED;

                // Position SearchInPDFs (left, 25%)
                bool appSuccess = AcrobatWindowManager.SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, appWidth, screenHeight, flags);
                if (!appSuccess)
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.WriteLine($"[WARNING] SetWindowPos failed for SearchInPDFs: Error {error}");
                }

                // Try positioning Acrobat (right, 75%) up to 3 times
                bool acrobatSuccess = false;
                for (int attempt = 1; attempt <= 3 && !acrobatSuccess; attempt++)
                {
                    // Ensure Acrobat is restored
                    ShowWindow(acrobatHandle, SW_RESTORE);
                    Thread.Sleep(500); // Wait for window state

                    // Position Acrobat
                    acrobatSuccess = AcrobatWindowManager.SetWindowPos(acrobatHandle, IntPtr.Zero, acrobatX, 0, acrobatWidth, screenHeight, flags);
                    if (!acrobatSuccess)
                    {
                        int error = Marshal.GetLastWin32Error();
                        Console.WriteLine($"[WARNING] SetWindowPos failed for Acrobat (attempt {attempt}): Error {error}");
                    }
                    else
                    {
                        Console.WriteLine($"[INFO] SetWindowPos succeeded for Acrobat on attempt {attempt}");
                    }
                    Thread.Sleep(500); // Wait before retry
                }

                // Maximize Acrobat within bounds
                ShowWindow(acrobatHandle, SW_SHOWNORMAL);

                UpdateStatus("Windows arranged successfully.");
                Console.WriteLine($"[INFO] Arranged windows: SearchInPDFs ({appWidth}x{screenHeight}), Acrobat ({acrobatWidth}x{screenHeight})");
            }
            else
            {
                UpdateStatus("Adobe Acrobat window not found.");
                Console.WriteLine("[ERROR] Adobe Acrobat window not found.");
                MessageBox.Show("Adobe Acrobat window not found. Please ensure Adobe Acrobat is installed and the configured PDF opener is correct.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            UpdateStatus("Error arranging windows.");
            Console.WriteLine($"[ERROR] ArrangeWindows failed: {ex.Message}");
            MessageBox.Show($"Error arranging windows: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindFromToCombos()
    {

        var directories = FolderManager.LoadFolderStructure(_launchDirectory);
        if (directories.Count == 0) return;
        foreach (var directory in directories)
        {
            if (!string.IsNullOrEmpty(directory))
            {
                var NewPath = Helpers.Helpers.GetShortenedDirectoryPath(directory);
            }
        }
    }

    private void BtnRetSearch_Click(object sender, EventArgs e)
    {
        ShowHideResultGroupBox();
    }

    private void TvSearchRange_AfterCheck(object sender, TreeViewEventArgs e)
    {
        // Check/uncheck all child nodes
        if (e.Node is { Nodes.Count: > 0 })
        {
            CheckAllChildNodes(e.Node, e.Node.Checked);
        }
    }

    private void dataGridViewResults_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        e.Handled = true;
        e.PaintBackground(e.CellBounds, e.State.HasFlag(DataGridViewElementStates.Selected));

        var regularFont = e.CellStyle?.Font;
        if (e.CellStyle?.Font != null)
        {
            var boldFont = new Font(e.CellStyle.Font, FontStyle.Bold);

            // Get the text to draw
            var text = e.FormattedValue?.ToString() ?? string.Empty;

            // Split text by <b> and </b> tags
            var parts = text.Split(new[] { "<b>", "</b>" }, StringSplitOptions.None);
            var isBold = false;

            // Variables for drawing
            float x = e.CellBounds.Left + 5;
            float y = e.CellBounds.Top + (e.CellBounds.Height - e.CellStyle.Font.Height) / 2;

            foreach (var part in parts)
            {
                var currentFont = isBold ? boldFont : regularFont;

                // Measure text size
                if (e.Graphics != null)
                {
                    if (currentFont != null)
                    {
                        var textSize = e.Graphics.MeasureString(part, currentFont);

                        // Draw the text
                        e.Graphics.DrawString(part, currentFont, Brushes.Black, x, y);

                        // Advance x position
                        x += textSize.Width;
                    }
                }

                // Toggle bold state
                isBold = !isBold;
            }
        }

        // Draw the focus rectangle if selected
        if (e.State.HasFlag(DataGridViewElementStates.Selected))
            e.Graphics?.DrawRectangle(Pens.Black, e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width - 1,
                e.CellBounds.Height - 1);
    }

    private void dataGridViewResults_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        try
        {
            // Ensure a cell is selected
            if (e.RowIndex >= 0)
            {
                // Get the full path from the hidden FullPath column
                var selectedRow = dataGridViewResults.Rows[e.RowIndex];
                var fullPath = selectedRow.Cells["FullPath"].Value?.ToString();
                var pageNo = selectedRow.Cells["PageNo"].Value?.ToString();


                if (!string.IsNullOrEmpty(fullPath))
                {
                    var pageNumber = Convert.ToInt32(pageNo);

                    // Open the PDF at the specific page
                    PdfOpener.OpenPdfAtPage(fullPath, pageNumber, txtSearchBox.Text, _launchDirectory);
                }
                else
                {
                    MessageBox.Show("Invalid file path selected.", "Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Please select a valid row.", "No Selection", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SearchInPDFs_Resize(object sender, EventArgs e)
    {
        if (this.WindowState != FormWindowState.Minimized) return;
        // Restore the existing PopupForm if it's minimized
        if (PopupForm.Instance.WindowState != FormWindowState.Minimized) return;
        PopupForm.Instance.WindowState = FormWindowState.Normal;
        PopupForm.Instance.BringToFront();
        acrobatWindowManager.FindOrLaunchAcrobatWindow();
    }
} 