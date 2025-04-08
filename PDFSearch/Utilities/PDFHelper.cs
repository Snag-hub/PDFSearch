using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PDFSearch.Utilities
{
    public static class PdfHelper
    {
        public static Dictionary<string, Dictionary<int, string>> ExtractTextFromMultipleDirectories(IEnumerable<string> directories)
        {
            var extractedData = new ConcurrentDictionary<string, Dictionary<int, string>>();
            var pdfFiles = new List<string>();

            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    Console.WriteLine($"Directory does not exist: {directory}");
                    continue;
                }

                pdfFiles.AddRange(Directory.GetFiles(directory, "*.pdf", SearchOption.AllDirectories));
            }

            Parallel.ForEach(pdfFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 }, pdfFile =>
            {
                try
                {
                    // Convert streaming result to dictionary for compatibility
                    var textByPage = ExtractTextFromPdfPages(pdfFile)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    if (textByPage.Count != 0)
                    {
                        extractedData[pdfFile] = textByPage;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file '{pdfFile}': {ex.Message}");
                }
            });

            return extractedData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static IEnumerable<KeyValuePair<int, string>> ExtractTextFromPdfPages(string pdfFilePath)
        {
            IEnumerable<KeyValuePair<int, string>> ExtractText()
            {
                using var pdfDoc = PdfDocument.Open(pdfFilePath, new ParsingOptions { UseLenientParsing = true });
                foreach (var page in pdfDoc.GetPages())
                {
                    yield return new KeyValuePair<int, string>(page.Number, page.Text);
                }
            }

            try
            {
                return ExtractText();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting text from '{pdfFilePath}': {ex.Message}");
                return Enumerable.Empty<KeyValuePair<int, string>>();
            }
        }
    }
}
