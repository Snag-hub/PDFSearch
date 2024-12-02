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
}
