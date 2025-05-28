using System.Runtime.InteropServices;
using System.Text;
using FindInPDFs.Acrobat;
using FindInPDFs.Utilities;
using Lucene.Net.QueryParsers.Classic;
using PDFSearch;
using PDFSearch.Utilities;
using Serilog; // Added for Serilog logging

namespace FindInPDFs;

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
        Log.Information("Initializing SearchInPDFs form for directory: {LaunchDirectory}", launchDirectory);
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
        Log.Information("Initializing DataGridView for search results.");
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
        Log.Information("SearchInPDFs form loaded.");
        txtSearchBox.Focus();
        var folderStructure = FolderManager.LoadFolderStructure(_launchDirectory);
        if (folderStructure is { Count: 0 })
        {
            Log.Information("No folder structure found, saving new structure for directory: {LaunchDirectory}", _launchDirectory);
            FolderManager.SaveFolderStructure(_launchDirectory);
            folderStructure = FolderManager.LoadFolderStructure(_launchDirectory);
        }
        Log.Information("Loaded folder structure with {Count} entries.", folderStructure?.Count ?? 0);

        AddDirectoriesToTreeView(_launchDirectory, folderStructure);
        if (TvSearchRange.Nodes.Count > 0)
        {
            TvSearchRange.Nodes[0].Expand();
            Log.Information("Expanded root node in TvSearchRange. Node count: {NodeCount}", TvSearchRange.Nodes.Count);
        }

        BindFromToCombos();
        ShowHideResultGroupBox();

        //acrobatWindowManager.FindOrLaunchAcrobatWindow();
        ArrangeWindows();
    }

    private void AddDirectoriesToTreeView(string launchDirectory, List<string>? folderStructure)
    {
        if (folderStructure == null || folderStructure.Count == 0)
        {
            Log.Information("Folder structure is empty or null for directory: {LaunchDirectory}", launchDirectory);
            return;
        }

        Log.Information("Adding directories to TreeView for directory: {LaunchDirectory}", launchDirectory);
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
        Log.Information("Finished adding directories to TreeView. Total nodes: {NodeCount}", nodes.Count);
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

        Log.Information("Retrieved {Count} selected directories from TreeView.", selectedDirectories.Count);
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
            Log.Information("Showing search result group box and hiding search group box.");
            grpBoxSearchResult.Location = new Point(12, 12);
            grpBoxSearchResult.Visible = true;
            grpBoxSearch.Visible = false;
        }
        else
        {
            Log.Information("Hiding search result group box and showing search group box.");
            grpBoxSearchResult.Visible = false;
            grpBoxSearch.Visible = true;
        }
    }

    #region This is not in use for range based search
    // (Commented-out code remains unchanged, no logging added here as it's not in use)
    #endregion

    private void BtnSearchText_Click(object sender, EventArgs e)
    {
        Log.Information("User clicked search button.");
        var matchWord = chkWholeWord.Checked;
        var matchCase = chkCaseSensitive.Checked;
        string? filePath = null;

        try
        {
            // Clear previous results
            dataGridViewResults.Rows.Clear();
            Log.Information("Cleared previous search results from DataGridView.");

            var searchTerm = txtSearchBox.Text.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                Log.Information("Search term is empty.");
                MessageBox.Show("Please enter a search term.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Log.Information("Search term entered: {SearchTerm}", searchTerm);

            // Get selected directories
            var selectedDirectories = GetSelectedDirectories(TvSearchRange.Nodes);
            if (!selectedDirectories.Any())
            {
                Log.Information("No directories selected in search range.");
                MessageBox.Show("Please select at least one node or child node in the Search Range.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Log.Information("Selected directories for search: {Directories}", string.Join(", ", selectedDirectories));

            // Perform search across all subfolder indexes
            UpdateStatus("Searching...");
            var results = _luceneSearcher.SearchInDirectory(searchTerm, _launchDirectory, matchWord, matchCase, filePath);
            Log.Information("Search completed with {ResultCount} results.", results.Count);

            // Filter by selected directories
            var filteredResults = results
                .Where(r => selectedDirectories.Any(d => r.FilePath.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            Log.Information("Filtered results to {FilteredCount} based on selected directories.", filteredResults.Count);

            if (filteredResults.Count == 0)
            {
                Log.Information("No search results found after filtering.");
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
            Log.Information("Populated DataGridView with {RowCount} rows.", dataGridViewResults.Rows.Count);
            UpdateStatus($"Search completed: {filteredResults.Count} results found.");
        }
        catch (DirectoryNotFoundException ex)
        {
            Log.Error(ex, "Index not found during search.");
            UpdateStatus("Search failed: Index not found.");
            MessageBox.Show($"Index not found: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (ParseException ex)
        {
            Log.Error(ex, "Invalid search term.");
            UpdateStatus("Search failed: Invalid search term.");
            MessageBox.Show($"Invalid search term: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error during search.");
            UpdateStatus("Search failed: An error occurred.");
            MessageBox.Show($"Search error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnSearchText_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar != (char)Keys.Enter) return;
        Log.Information("User pressed Enter key to initiate search.");
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
        Log.Information("User double-clicked a node in treeVwResult.");
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
                Log.Information("Opening PDF file: {FilePath} at page {PageNumber}", filePath, pageNumber);

                // Open the PDF at the specific page
                PdfOpener.OpenPdfAtPage(filePath, pageNumber, txtSearchBox.Text, _launchDirectory);
            }
            else
            {
                Log.Information("Invalid node selected in treeVwResult.");
                MessageBox.Show(@"Invalid node selected.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while handling node double-click in treeVwResult.");
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
            Log.Information("Starting background indexing for directory: {LaunchDirectory}", _launchDirectory);
            UpdateStatus($"Indexing started for directory: {_launchDirectory}");
            await Task.Run(() => LuceneIndexer.IndexDirectory(_launchDirectory));
            Log.Information("Background indexing completed for directory: {LaunchDirectory}", _launchDirectory);
            UpdateStatus($"Indexing completed for directory: {_launchDirectory}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during background indexing.");
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
        Log.Information("Updated status: {Message}", message);
    }

    private void SearchInPDFs_FormClosing(object sender, FormClosingEventArgs e)
    {
        Log.Information("SearchInPDFs form closing.");
        PopupForm popupForm = new(_launchDirectory);
        popupForm.Close();
        Log.Information("Closed PopupForm during SearchInPDFs form closing.");
        // AcrobatWindowManager.EnsureAcrobatClosed();
    }

    private void ArrangeWindows()
    {
        Log.Information("Arranging windows for SearchInPDFs and Acrobat.");
        try
        {
            // Find the Acrobat window
            IntPtr acrobatHandle = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                StringBuilder windowText = new StringBuilder(256);
                GetWindowText(hWnd, windowText, windowText.Capacity);

                if (windowText.ToString().Contains("Adobe Acrobat") || windowText.ToString().Contains("Acrobat"))
                {
                    acrobatHandle = hWnd;
                    Log.Information("Found Acrobat window with handle: {Handle}", acrobatHandle);
                    return false; // Stop further enumeration
                }

                return true;
            }, IntPtr.Zero);

            if (acrobatHandle != IntPtr.Zero)
            {
                // Get screen dimensions
                int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
                Log.Information("Screen dimensions: Width={Width}, Height={Height}", screenWidth, screenHeight);

                // Calculate window sizes (30/70 split)
                int yourAppWidth = (int)(screenWidth * 0.25);
                int acrobatWidth = screenWidth - yourAppWidth; // Use remaining space

                const uint SWP_NOZORDER = 0x0004;
                const uint SWP_SHOWWINDOW = 0x0040;
                const uint flags = SWP_SHOWWINDOW | SWP_NOZORDER;

                // Position your app first (left side)
                AcrobatWindowManager.SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, yourAppWidth, screenHeight, flags);
                Log.Information("Positioned SearchInPDFs window: Width={Width}, Height={Height}", yourAppWidth, screenHeight);

                // Position Acrobat (right side)
                AcrobatWindowManager.SetWindowPos(acrobatHandle, IntPtr.Zero, yourAppWidth, 0, acrobatWidth,
                    screenHeight, flags);
                Log.Information("Positioned Acrobat window: X={X}, Width={Width}, Height={Height}", yourAppWidth, acrobatWidth, screenHeight);
            }
            else
            {
                Log.Information("Adobe Acrobat window not found.");
                MessageBox.Show("Adobe Acrobat window not found.", "Warning", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while arranging windows.");
            MessageBox.Show($"Error arranging windows: {ex.Message}", "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void BindFromToCombos()
    {
        Log.Information("Binding directories to From/To combos.");
        var directories = FolderManager.LoadFolderStructure(_launchDirectory);
        if (directories.Count == 0)
        {
            Log.Information("No directories found to bind to combos.");
            return;
        }
        foreach (var directory in directories)
        {
            if (!string.IsNullOrEmpty(directory))
            {
                var newPath = PDFSearch.Helpers.Helpers.GetShortenedDirectoryPath(directory);
            }
        }
        Log.Information("Finished binding {Count} directories to combos.", directories.Count);
    }

    private void BtnRetSearch_Click(object sender, EventArgs e)
    {
        Log.Information("User clicked return to search button.");
        ShowHideResultGroupBox();
    }

    private void TvSearchRange_AfterCheck(object sender, TreeViewEventArgs e)
    {
        Log.Information("User changed check state for node: {NodeText}, Checked: {IsChecked}", e.Node.Text, e.Node.Checked);
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
        Log.Information("User double-clicked a cell in DataGridView at row: {RowIndex}", e.RowIndex);
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
                    Log.Information("Opening PDF file: {FilePath} at page {PageNumber}", fullPath, pageNumber);

                    // Open the PDF at the specific page
                    PdfOpener.OpenPdfAtPage(fullPath, pageNumber, txtSearchBox.Text, _launchDirectory);
                }
                else
                {
                    Log.Information("Invalid file path selected in DataGridView.");
                    MessageBox.Show("Invalid file path selected.", "Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            else
            {
                Log.Information("No valid row selected in DataGridView.");
                MessageBox.Show("Please select a valid row.", "No Selection", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while handling cell double-click in DataGridView.");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SearchInPDFs_Resize(object sender, EventArgs e)
    {
        if (this.WindowState != FormWindowState.Minimized) return;
        Log.Information("SearchInPDFs form minimized.");
        // Restore the existing PopupForm if it's minimized
        if (PopupForm.Instance.WindowState != FormWindowState.Minimized) return;
        Log.Information("Restoring PopupForm from minimized state.");
        PopupForm.Instance.WindowState = FormWindowState.Normal;
        PopupForm.Instance.BringToFront();
        acrobatWindowManager.FindOrLaunchAcrobatWindow();
        Log.Information("Launched or found Acrobat window after restoring PopupForm.");
    }
}