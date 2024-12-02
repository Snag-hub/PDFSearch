using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PDFSearch;

internal class PDFHelper
{
    /// <summary>
    /// Extracts text from a PDF file, page by page.
    /// </summary>
    /// <param name="pdfFilePath">The path to the PDF file.</param>
    /// <returns>A dictionary where the key is the page number and the value is the extracted text for that page.</returns>
    public static Dictionary<int, string> ExtractTextFromPdf(string pdfFilePath)
    {
        var textByPage = new Dictionary<int, string>();

        try
        {
            using PdfDocument pdfDoc = PdfDocument.Open(pdfFilePath);
            int pageNum = 1;

            foreach (Page page in pdfDoc.GetPages())
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
