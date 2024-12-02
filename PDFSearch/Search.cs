using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDFSearch;

public partial class Search : Form
{
    public Search()
    {
        InitializeComponent();
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

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = folderDialog.SelectedPath;
                ProcessPdfFilesInFolder(selectedPath);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error {ex.Message}", "Error Box", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ProcessPdfFilesInFolder(string folderPath)
    {
        // Get all PDF files in the folder and subdirectories
        var pdfFiles = Directory.GetFiles(folderPath, "*.pdf", SearchOption.AllDirectories);

        foreach (var pdfFile in pdfFiles)
        {
            var textByPage = PDFHelper.ExtractTextFromPdf(pdfFile);
            LuceneIndexer.IndexPdfContent(pdfFile, textByPage);
        }

        MessageBox.Show("PDFs indexed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            // Perform search
            var results = LuceneSearcher.SearchIndexWithPage(searchTerm);

            // Clear any existing data in the DataGridView
            dgvSearchResult.DataSource = null;

            // Bind results to DataGridView
            if (results.Count > 0)
            {
                dgvSearchResult.DataSource = results;
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
            if (dgvSearchResult.CurrentRow != null && dgvSearchResult.CurrentRow.DataBoundItem is SearchResult selectedResult)
            {
                string filePath = selectedResult.FilePath;
                int pageNumber = selectedResult.PageNumber;

                // Open the PDF and navigate to the specific page
                OpenPdfAtPage(filePath, pageNumber);
            }
            else
            {
                // Optional: Handle the case where no valid row is selected
                MessageBox.Show("Please select a valid search result.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void OpenPdfAtPage(string filePath, int pageNumber)
    {
        try
        {
            // Define the path to Adobe Acrobat Reader
            string adobeReaderPath = @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe";
            //string adobeReaderPath = @"Acrobat.exe";

            // Format the command to open the PDF with the correct page in Acrobat
            string arguments = $"/A \"page={pageNumber}\" \"{filePath}\"";

            // Check if Adobe Acrobat Reader is installed
            if (File.Exists(adobeReaderPath))
            {
                // Open the PDF in Adobe Acrobat at the specified page
                System.Diagnostics.Process.Start(adobeReaderPath, arguments);
                Console.WriteLine($"Opening PDF: {filePath} at page {pageNumber}");
            }
            else
            {
                // If Acrobat is not installed, fallback to the default PDF viewer
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // Uses the default viewer for PDFs
                });
                Console.WriteLine($"Opening PDF: {filePath} at page {pageNumber} in default viewer.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine($"Error opening PDF: {ex.Message}");
        }
    }
}
