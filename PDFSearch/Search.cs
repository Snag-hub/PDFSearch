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

            string currentDirectory = _launchDirectory;
            var results = LuceneSearcher.SearchInDirectory(searchTerm, currentDirectory);

            dgvSearchResult.DataSource = null;
            if (results.Count > 0)
            {
                dgvSearchResult.DataSource = results;
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
}
