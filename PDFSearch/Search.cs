using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDFSearch;

public partial class Search : Form
{
    private readonly string _launchDirectory;

    public Search(string launchDirectory)
    {
        _launchDirectory = launchDirectory;
        InitializeComponent();
        this.Load += async (s, e) => await ProcessIndexingInBackground();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        try
        {
            using FolderBrowserDialog folderDialog = new();
            if (folderDialog.ShowDialog() != DialogResult.OK) return;

            string selectedPath = folderDialog.SelectedPath;
            ProcessPdfFilesInFolder(selectedPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void ProcessPdfFilesInFolder(string folderPath)
    {
        try
        {
            LuceneIndexer.IndexDirectory(folderPath); // Call the directory-based indexer
            MessageBox.Show("PDFs indexed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error while indexing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnSearch_Click(object sender, EventArgs e)
    {
        try
        {
            string searchTerm = TxtSearch.Text.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("Please enter a search term.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var results = LuceneSearcher.SearchInDirectory(searchTerm, _launchDirectory);

            dgvSearchResult.DataSource = null;
            if (results.Count > 0)
            {
                dgvSearchResult.DataSource = results;
                dgvSearchResult.Columns["FilePath"].Visible = false;
                dgvSearchResult.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                lblResult.Text = $"{results.Count} results found for \"{searchTerm}\"";
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

    private void dgvSearchResult_DoubleClick(object sender, EventArgs e)
    {
        try
        {
            // Check if a valid row is selected
            if (dgvSearchResult.CurrentRow is { DataBoundItem: SearchResult selectedResult })
            {
                var filePath = selectedResult.FilePath;
                var pageNumber = selectedResult.PageNumber;

                // Open the PDF and navigate to the specific page
                OpenPdfAtPage(filePath, pageNumber);
            }
            else
            {
                // Optional: Handle the case where no valid row is selected
                MessageBox.Show(@"Please select a valid search result.", @"No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($@"Error: {ex.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void OpenPdfAtPage(string filePath, int pageNumber)
    {
        try
        {
            // Define the path to Adobe Acrobat Reader
            const string adobeReaderPath = @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe";
            //string adobeReaderPath = @"Acrobat.exe";

            // Format the command to open the PDF with the correct page in Acrobat
            var arguments = $"/A \"page={pageNumber}\" \"{filePath}\"";

            // Check if Adobe Acrobat Reader is installed
            if (File.Exists(adobeReaderPath))
            {
                // Open the PDF in Adobe Acrobat at the specified page
                System.Diagnostics.Process.Start(adobeReaderPath, arguments);
                Console.WriteLine($@"Opening PDF: {filePath} at page {pageNumber}");
            }
            else
            {
                // If Acrobat is not installed, fallback to the default PDF viewer
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // Uses the default viewer for PDFs
                });
                Console.WriteLine($@"Opening PDF: {filePath} at page {pageNumber} in default viewer.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($@"Error opening PDF: {ex.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine($@"Error opening PDF: {ex.Message}");
        }
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
    private void BtnClean_Click(object sender, EventArgs e)
    {
        try
        {
            // Clean the existing index directory
            LuceneIndexer.CleanAllIndexes();
        }
        catch (Exception ex)
        {
            MessageBox.Show($@"An error occurred: {ex.Message}");
        }
    }

    private void TxtSearch_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar != (char)Keys.Enter) return;
        BtnSearch.PerformClick();
        e.Handled = true;
    }
}
