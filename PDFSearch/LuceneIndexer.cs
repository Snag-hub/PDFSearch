using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Directory = System.IO.Directory;

namespace PDFSearch;

public static class LuceneIndexer
{
    /// <summary>
    /// Generates a unique folder name for a given directory path.
    /// </summary>
    private static string GetIndexFolderName(string folderPath)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(folderPath));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Indexes all PDF files in a directory, including its subdirectories.
    /// </summary>
    public static void IndexDirectory(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");

        string baseIndexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index");
        string uniqueIndexPath = Path.Combine(baseIndexPath, GetIndexFolderName(folderPath));
        Directory.CreateDirectory(uniqueIndexPath);

        using var dir = FSDirectory.Open(uniqueIndexPath);
        using var analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
        using var writer = new IndexWriter(dir, new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer));

        var pdfFiles = Directory.GetFiles(folderPath, "*.pdf", SearchOption.AllDirectories);

        foreach (var pdfFile in pdfFiles)
        {
            try
            {
                var textByPage = PdfHelper.ExtractTextFromPdfPages(pdfFile);
                foreach (var (page, text) in textByPage)
                {
                    var doc = new Document
                    {
                        new StringField("FilePath", pdfFile, Field.Store.YES),
                        new Int32Field("PageNumber", page, Field.Store.YES),
                        new TextField("Content", text, Field.Store.NO)
                    };
                    writer.AddDocument(doc);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file '{pdfFile}': {ex.Message}");
            }
        }

        writer.Flush(triggerMerge: false, applyAllDeletes: false);
        Console.WriteLine($"Indexing completed for directory: {folderPath}. Index stored at: {uniqueIndexPath}");
    }

    /// <summary>
    /// Deletes all indexes.
    /// </summary>
    public static void CleanAllIndexes()
    {
        string baseIndexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index");
        if (Directory.Exists(baseIndexPath))
        {
            Directory.Delete(baseIndexPath, recursive: true);
            Console.WriteLine("All indexes have been cleaned.");
        }
    }
}
