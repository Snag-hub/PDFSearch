using Microsoft.Extensions.Logging;
using PDFSearch.Utilities;
using PDFSearch.Acrobat;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Text;

namespace PDFSearch;

public partial class SearchInPDFs : Form
{

    private readonly string _launchDirectory;
    private readonly AcrobatWindowManager acrobatWindowManager;

    // make object of lucene searcher
    private readonly LuceneSearcher LuceneSearcher = new();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private const int SW_SHOWMAXIMIZED = 3;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;

    // Static reference to keep track of the instance
    private static SearchInPDFs instance;

    public SearchInPDFs(string launchDirectory)
    {

        _launchDirectory = launchDirectory;

        InitializeComponent();
        InitializeDataGridView();

        // Enable KeyPreview for the form
        this.KeyPreview = true;

        // Subscribe to the KeyPress event for the form
        this.KeyPress += BtnSearchText_KeyPress;

        acrobatWindowManager = new AcrobatWindowManager(_launchDirectory);

        // Add form load event handler
        this.Load += SearchInPDFs_Load;
        //this.Load += async (s, e) => await ProcessIndexingInBackground();
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
        List<string> folderStructure = FolderManager.LoadFolderStructure(); if (folderStructure.Count == 0)
        {
            FolderManager.SaveFolderStructure(_launchDirectory);
            folderStructure = FolderManager.LoadFolderStructure();
        }

        AddDirectoriesToTreeView(_launchDirectory, folderStructure);
        TvSearchRange.ExpandAll();

        // some import calling
        BindFromToCombos();
        ShowHideResultGroupBox();



        //treeVwResult.DrawMode = TreeViewDrawMode.OwnerDrawText;
        Thread.Sleep(1000); // Wait for Acrobat to initialize
        ArrangeWindows();
    }

    private void AddDirectoriesToTreeView(string launchDirectory, List<string> folderStructure)
    {
        if (folderStructure == null || folderStructure.Count == 0)
        {
            return;
        }

        // Create the root node based on the E-Library starting point
        string rootFolderName = new DirectoryInfo(launchDirectory).Name;
        TreeNode rootNode = new TreeNode(rootFolderName)
        {
            Tag = launchDirectory
        };
        TvSearchRange.Nodes.Add(rootNode);

        // Create a dictionary to store nodes by their full paths
        Dictionary<string, TreeNode> nodes = new Dictionary<string, TreeNode>
        {
            { launchDirectory, rootNode }
        };

        foreach (string path in folderStructure)
        {
            // Only add paths that are under the _launchDirectory
            if (path.StartsWith(launchDirectory, StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = path.Substring(launchDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
                string[] parts = relativePath.Split(Path.DirectorySeparatorChar);
                string currentPath = launchDirectory;
                TreeNode parentNode = rootNode;

                foreach (string part in parts)
                {
                    currentPath = Path.Combine(currentPath, part);

                    if (!nodes.ContainsKey(currentPath))
                    {
                        TreeNode newNode = new TreeNode(part)
                        {
                            Tag = currentPath
                        };

                        if (parentNode != null)
                        {
                            parentNode.Nodes.Add(newNode);
                        }

                        nodes[currentPath] = newNode;
                    }

                    parentNode = nodes[currentPath];
                }
            }
        }
    }


    private void CheckAllChildNodes(TreeNode parentNode, bool isChecked)
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
        List<string> selectedDirectories = new List<string>();

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

    private bool AreAllChildNodesChecked(TreeNode parentNode)
    {
        foreach (TreeNode childNode in parentNode.Nodes)
        {
            if (!childNode.Checked || !AreAllChildNodesChecked(childNode))
            {
                return false;
            }
        }
        return true;
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
        bool MatchWord = false;
        bool MatchCase = false;
        string? filePath = null;

        if (chkCaseSensitive.Checked)
        {
            MatchCase = true;
        }
        if (chkWholeWord.Checked)
        {
            MatchWord = true;
        }

        try
        {
            // Clear previous results in the DataGridView
            dataGridViewResults.Rows.Clear();

            string searchTerm = txtSearchBox.Text.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("Please enter a search term.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Get selected directories from the TreeView
            List<string> selectedDirectories = GetSelectedDirectories(TvSearchRange.Nodes);

            // Check if at least one node or child node is selected
            if (selectedDirectories.Count == 0)
            {
                MessageBox.Show("Please select at least one node or child node in the Search Range.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Perform the search in the _launchDirectory
            var results = LuceneSearcher.SearchInDirectory(searchTerm, _launchDirectory, MatchWord, MatchCase, filePath);

            // Filter the results based on the selected directories
            var filteredResults = results.Where(r => selectedDirectories.Any(d => r.FilePath.StartsWith(d, StringComparison.OrdinalIgnoreCase))).ToList();

            if (filteredResults.Count <= 0)
            {
                MessageBox.Show("No results found.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            ShowHideResultGroupBox();
            foreach (var result in filteredResults)
            {
                // Get the full file path and relative path
                string fullPath = result.FilePath;
                string relativePath = result.RelativePath;

                // Split the full path and relative path into segments
                string[] splitFullPath = fullPath.Split(Path.DirectorySeparatorChar);
                string[] splitRelativePath = relativePath.Split(@"\");

                // Find the start index of the relative path in the full path
                // The fleet name is the last segment of the full path before the vessel starts
                int vesselIndexInFullPath = Array.IndexOf(splitFullPath, splitRelativePath[0]);

                // Extract fleet as the last segment before the vessel starts in the relative path
                string fleet = vesselIndexInFullPath >= 0
                    ? splitFullPath[vesselIndexInFullPath - 1]
                    : string.Empty;

                // Extract vessel, part, and manual from the relative path
                string vessel = splitRelativePath.Length > 0 ? splitRelativePath[0] : string.Empty; // First part of the relative path
                string part = splitRelativePath.Length > 1 ? splitRelativePath[1] : string.Empty;   // Second part of the relative path
                string manual = Path.GetFileNameWithoutExtension(relativePath); // The file name without extension

                // Format the page number and snippet
                string pageNoWithContent = result.PageNumber > 0 ? $"Page {result.PageNumber}: {result.Snippet}" : result.Snippet;

                // Add the data to the DataGridView
                dataGridViewResults.Rows.Add(fleet, string.Empty, vessel, part, manual, pageNoWithContent, fullPath, result.PageNumber);
            }

            // Auto resize the columns to fit the content
            dataGridViewResults.AutoResizeColumns();


        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        PopupForm popupForm = new(_launchDirectory);
        popupForm.Close();
        acrobatWindowManager.EnsureAcrobatClosed();
    }

    public void ArrangeWindows()
    {
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
                    return false; // Stop further enumeration
                }

                return true;
            }, IntPtr.Zero);

            if (acrobatHandle != IntPtr.Zero)
            {
                // Get screen dimensions
                int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;

                // Calculate window sizes (30/70 split)
                int yourAppWidth = (int)(screenWidth * 0.25);
                int acrobatWidth = screenWidth - yourAppWidth;  // Use remaining space

                const uint SWP_NOZORDER = 0x0004;
                const uint SWP_SHOWWINDOW = 0x0040;
                const uint flags = SWP_SHOWWINDOW | SWP_NOZORDER;

                // Position your app first (left side)
                AcrobatWindowManager.SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, yourAppWidth, screenHeight, flags);

                // Position Acrobat (right side)
                AcrobatWindowManager.SetWindowPos(acrobatHandle, IntPtr.Zero, yourAppWidth, 0, acrobatWidth, screenHeight, flags);
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



    private void BindFromToCombos()
    {

        List<string> directories = FolderManager.LoadFolderStructure();
        if (directories.Count != 0)
        {
            foreach (string directory in directories)
            {
                if (!string.IsNullOrEmpty(directory))
                {
                    string NewPath = Helpers.Helpers.GetShortenedDirectoryPath(directory);


                }

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
        if (e.Node.Nodes.Count > 0)
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

        Font regularFont = e.CellStyle.Font;
        Font boldFont = new Font(e.CellStyle.Font, FontStyle.Bold);

        // Get the text to draw
        string text = e.FormattedValue?.ToString() ?? string.Empty;

        // Split text by <b> and </b> tags
        string[] parts = text.Split(new[] { "<b>", "</b>" }, StringSplitOptions.None);
        bool isBold = false;

        // Variables for drawing
        float x = e.CellBounds.Left + 5;
        float y = e.CellBounds.Top + (e.CellBounds.Height - e.CellStyle.Font.Height) / 2;

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

        // Draw the focus rectangle if selected
        if (e.State.HasFlag(DataGridViewElementStates.Selected))
            e.Graphics.DrawRectangle(Pens.Black, e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width - 1, e.CellBounds.Height - 1);
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
                var PageNo = selectedRow.Cells["PageNo"].Value?.ToString();


                if (!string.IsNullOrEmpty(fullPath))
                {
                    int pageNumber = Convert.ToInt32(PageNo);

                    // Open the PDF at the specific page
                    PdfOpener.OpenPdfAtPage(fullPath, pageNumber);
                }
                else
                {
                    MessageBox.Show("Invalid file path selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Please select a valid row.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SearchInPDFs_Resize(object sender, EventArgs e)
    {
        if (this.WindowState == FormWindowState.Minimized)
        {
            // Restore the existing PopupForm if it's minimized
            if (PopupForm.Instance != null && PopupForm.Instance.WindowState == FormWindowState.Minimized)
            {
                PopupForm.Instance.WindowState = FormWindowState.Normal;
                PopupForm.Instance.BringToFront();
                acrobatWindowManager.FindOrLaunchAcrobatWindow();
            }
        }
    }
}
