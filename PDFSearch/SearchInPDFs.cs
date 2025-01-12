using PDFSearch.BackgroundPathFinder;
using Microsoft.Extensions.Logging;
using PDFSearch.Utilities;
using PDFSearch.Acrobat;
using System.Configuration;

namespace PDFSearch;

public partial class SearchInPDFs : Form
{
    
    private readonly string _launchDirectory;
    private readonly AcrobatWindowManager acrobatWindowManager;

    // make object of lucene searcher
    private readonly LuceneSearcher LuceneSearcher = new();
    private readonly PdfPathBackgroundService _pdfPathBackgroundService;


    public SearchInPDFs(string launchDirectory)
    {
        _launchDirectory = launchDirectory;
        InitializeComponent();

        // Check if configuration exists
        ConfigManager config = ConfigManager.LoadConfig();

        if (config == null)
        {
            // If no config, show the first-time setup
            ShowFirstTimeSetup();
        }
        else
        {
            // Use existing configuration
            MessageBox.Show($"Start File: {config.StartFile}\nPDF Opener: {config.PdfOpener}");
        }

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

        acrobatWindowManager = new AcrobatWindowManager(_launchDirectory);

        // Add form load event handler
        this.Load += SearchInPDFs_Load;
        this.Load += async (s, e) => await ProcessIndexingInBackground();
    }

    private void ShowFirstTimeSetup()
    {
        // Show file dialog for index.pdf
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "PDF Files|*.pdf",
            Title = "Select the Start File (index.pdf)"
        };

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            string startFile = openFileDialog.FileName;

            // Ask user to choose PDF opener
            FolderBrowserDialog folderDialog = new();
            folderDialog.Description = "Select the folder where your PDF Opener is located (e.g., Adobe Acrobat Reader folder)";
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                string pdfOpener = Path.Combine(folderDialog.SelectedPath, "AcroRd32.exe");  // Default name for Adobe Reader executable

                // Save the config
                ConfigManager config = new ConfigManager
                {
                    StartFile = startFile,
                    PdfOpener = pdfOpener
                };

                config.SaveConfig();

                MessageBox.Show("Configuration saved successfully!");
            }
        }
    }

    private void SearchInPDFs_Load(object sender, EventArgs e)
    {
        // start filePath retriving service
        _pdfPathBackgroundService.Start();

        FolderManager.LoadFolderStructure();
        FolderManager.SaveFolderStructure(_launchDirectory);

        // enable
        GbRange.Height = 0;
        GbRange.Visible = false;

        // some import calling
        BindFromToCombos();



        treeVwResult.DrawMode = TreeViewDrawMode.OwnerDrawText;
        Thread.Sleep(1000); // Wait for Acrobat to initialize
        ArrangeWindows();
    }

    private void BtnSearchText_Click(object sender, EventArgs e)
    {
        string? filePath = _pdfPathBackgroundService.FilePath;
        filePath = DirectoryUtils.RemoveFileName(filePath);

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
                    string abbreviatedPath = DirectoryUtils.AbbreviateDirectoryPath(fullDirectoryPath);

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
        acrobatWindowManager.EnsureAcrobatClosed();
    }

    public void ArrangeWindows()
    {
        try
        {
            acrobatWindowManager.FindOrLaunchAcrobatWindow();  // This method will now set the acrobatHandle

            if (acrobatWindowManager.acrobatHandle != IntPtr.Zero)
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
                AcrobatWindowManager.SetWindowPos(acrobatWindowManager.acrobatHandle, IntPtr.Zero, yourAppWidth, 0, acrobatWidth, screenHeight, flags);
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

    private void RdBtnRamge_CheckedChanged(object sender, EventArgs e)
    {
        if (RdBtnRamge.Checked)
        {
            GbRange.Visible = true;
            GbRange.Height = 100;
        }

        if (RdBtnRamge.Checked == false)
        {
            GbRange.Visible = false;
            GbRange.Height = 0;
        }
    }

    private void BtnClose_Click(object sender, EventArgs e)
    {
        GbRange.Height = 0;
        GbRange.Visible = false;
        RdBtnRamge.Checked = false;
    }

    private void BindFromToCombos()
    {
        CmbTo.DataSource = null;
        CmbFrom.DataSource = null;

        List<string> directories = FolderManager.LoadFolderStructure();
        if (directories.Count != 0)
        {
            foreach (string directory in directories)
            {
                if (!string.IsNullOrEmpty(directory))
                {
                    string NewPath = Helpers.Helpers.GetShortenedDirectoryPath(directory);

                    CmbFrom.Items.Add(NewPath);
                    CmbTo.Items.Add(NewPath);
                }

            }
        }
    }
}
