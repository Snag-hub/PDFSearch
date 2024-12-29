﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using PDFSearch.BackgroundPathFinder;
using Microsoft.Extensions.Logging;

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

        // Initialize the logger (you can configure it as per your requirements)
        ILogger<PdfPathBackgroundService> logger = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        }).CreateLogger<PdfPathBackgroundService>();

        // Initialize the PdfPathBackgroundService
        _pdfPathBackgroundService = new PdfPathBackgroundService(logger);

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

    private void FindAcrobatWindow()
    {
        EnumWindows((hWnd, lParam) =>
        {
            StringBuilder windowText = new StringBuilder(256);
            GetWindowText(hWnd, windowText, windowText.Capacity);

            // Print out the window title to the console or debug output to see what it's named
            Console.WriteLine("Window Title: " + windowText.ToString());

            // Search for any Acrobat window by partial title
            if (windowText.ToString().Contains("Adobe Acrobat") || windowText.ToString().Contains("Acrobat"))
            {
                // Set acrobat window to the found handle
                acrobatHandle = hWnd;
                return false; // Stop further enumeration once we find it
            }

            return true; // Continue searching other windows
        }, IntPtr.Zero);
    }

    private void ArrangeWindows()
    {
        try
        {
            FindAcrobatWindow();  // This method will now set the acrobatHandle

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

    private void BtnSearchText_Click(object sender, EventArgs e)
    {
        string? filePath = _pdfPathBackgroundService.FilePath;

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
                foreach (var result in results)
                {
                    // Create the root node for each search result (just the snippet)
                    TreeNode rootNode = new($"{result.Snippet} - FilePath: {result.RelativePath}")
                    {
                        Tag = result  // Store the full result object in Tag property for reference
                    };

                    // Add the root node to the TreeView
                    treeVwResult.Nodes.Add(rootNode);
                }
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
        string[] parts = text.Split(new[] { "<b>", "</b>" }, StringSplitOptions.None);
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
            if (selectedNode != null)
            {
                // Retrieve the SearchResult (or equivalent object) stored in the Tag property
                if (selectedNode.Tag is SearchResult selectedResult)
                {
                    var filePath = selectedResult.FilePath;
                    var pageNumber = selectedResult.PageNumber;

                    // Open the PDF at the specific page (or implement your own logic)
                    PdfOpener.OpenPdfAtPage(filePath, pageNumber);
                }
                else
                {
                    MessageBox.Show(@"Invalid search result.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(@"Please select a valid search result.", @"No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($@"Error: {ex.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void treeVwResult_DrawNode(object sender, DrawTreeNodeEventArgs e)
    {
        // Set the default font and brush
        Font regularFont = e.Node.TreeView.Font;
        Font boldFont = new Font(e.Node.TreeView.Font, FontStyle.Bold);

        // Check if the current node is selected (using the selected node from TreeView)
        bool isSelected = e.Node == e.Node.TreeView.SelectedNode;

        // Set the background color based on selection
        if (isSelected)
        {
            e.Graphics.FillRectangle(Brushes.LightBlue, e.Bounds); // Selected node background
        }
        else
        {
            e.Graphics.FillRectangle(Brushes.White, e.Bounds); // Default background for non-selected nodes
        }

        // Get the node text
        string text = e.Node.Text;

        // Variables for drawing
        float x = e.Bounds.Left;
        float y = e.Bounds.Top;

        // Split text by <b> and </b> tags
        string[] parts = text.Split(new[] { "<b>", "</b>" }, StringSplitOptions.None);
        bool isBold = false;

        // Draw each part of the text
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

        // Draw the focus rectangle if the node is selected
        if (isSelected)
        {
            // Define the focus rectangle bounds based on the node's bounds
            Rectangle focusRect = e.Bounds;
            focusRect.Inflate(1, 1);  // Optional: Add a little padding for focus rectangle
            e.Graphics.DrawRectangle(Pens.Blue, focusRect);  // Draw the focus rectangle
        }


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
    }
}
