using System;
using System.Collections.Generic;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PDFSearch;

public static class PDFHelper
{
    /// <summary>
    /// Extracts text from a PDF file, page by page.
    /// </summary>
    /// <param name="pdfFilePath">Path to the PDF file.</param>
    /// <returns>A dictionary where the key is the page number and the value is the text on that page.</returns>
    public static Dictionary<int, string> ExtractTextFromPdf(string pdfFilePath)
    {
        var textByPage = new Dictionary<int, string>();

        try
        {
            using var pdfDoc = PdfDocument.Open(pdfFilePath);
            int pageNum = 1;

            foreach (var page in pdfDoc.GetPages())
            {
                textByPage[pageNum] = page.Text;
                pageNum++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting text from {pdfFilePath}: {ex.Message}");
        }

        return textByPage;
    }
}
