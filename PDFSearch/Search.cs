
namespace PDFSearch;

public partial class Search : Form
{
    public Search()
    {
        InitializeComponent();
        this.Load += async (s, e) => await ProcessIndexingInBackground();
    }

    /// <summary>
    /// This is for Load PDF
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void button1_Click(object sender, EventArgs e)
    {
        try
        {
            using FolderBrowserDialog folderDialog = new();

            if (folderDialog.ShowDialog() != DialogResult.OK) return;
            var selectedPath = folderDialog.SelectedPath;
            ProcessPdfFilesInFolder(selectedPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($@"Error {ex.Message}", @"Error Box", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void ProcessPdfFilesInFolder(string folderPath)
    {
        try
        {
            LuceneIndexer.IndexDirectory(folderPath); // Call the directory-based indexer
            MessageBox.Show(@"PDFs indexed successfully!", @"Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($@"Error while indexing: {ex.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private void BtnSearch_Click(object sender, EventArgs e)
    {
        try
        {
            var searchTerm = TxtSearch.Text.Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show(@"Please enter a search term.", @"Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Perform search
            var results = LuceneSearcher.SearchIndexWithPage(searchTerm);

            // Clear any existing data in the DataGridView
            dgvSearchResult.DataSource = null;

            // Bind results to DataGridView
            if (results.Count > 0)
            {
                dgvSearchResult.DataSource = results;
                dgvSearchResult.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                var count = results.Count;
                lblResult.Text = $@"{count} results found for ""{TxtSearch.Text}""";
            }
            else
            {
                MessageBox.Show(@"No results found.", @"Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            
        }
        catch (Exception ex)
        {
            MessageBox.Show($@"Error: {ex.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            // Show a progress message
            UpdateStatus("Indexing started...");

            // Run the indexing in a background task
            await Task.Run(() =>
            {
                const string directoryPath = @"E:\Freelance Work\Farohar\Farohar E-Document Library_Sample"; // Replace with the actual path
                LuceneIndexer.IndexDirectory(directoryPath);
            });

            // Update the status when done
            UpdateStatus("Indexing completed successfully.");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error during indexing: {ex.Message}");
        }
    }
    
    private void UpdateStatus(string message)
    {
        // Safely update the UI
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
            LuceneIndexer.CleanIndexDirectory();
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
