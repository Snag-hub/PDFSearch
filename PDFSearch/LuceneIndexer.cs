using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Directory = System.IO.Directory;

namespace PDFSearch;

public static class LuceneIndexer
{
    private const string IndexPath = "LuceneIndex"; // Directory to store index

    /// <summary>
    /// Indexes PDF content, storing each page as a separate document in Lucene.
    /// </summary>
    /// <param name="pdfFilePath">Path to the PDF file.</param>
    /// <param name="textByPage">Dictionary with page numbers and their corresponding text.</param>
    public static void IndexPdfContent(string pdfFilePath, Dictionary<int, string> textByPage)
    {
        try
        {
            // Ensure the index directory exists
            Directory.CreateDirectory(IndexPath);

            using var directory = FSDirectory.Open(IndexPath);
            var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);

            using var writer = new IndexWriter(directory, config);

            foreach (var page in textByPage)
            {
                // Create a new document for each page
                var doc = new Document
                {
                    new StringField("FilePath", pdfFilePath, Field.Store.YES),
                    new Int32Field("PageNumber", page.Key, Field.Store.YES),
                    new TextField("Content", page.Value, Field.Store.YES) // Full page text
                };

                writer.AddDocument(doc);
            }

            writer.Commit();
            Console.WriteLine($"Indexing complete for file: {pdfFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error indexing PDF {pdfFilePath}: {ex.Message}");
        }
    }
}
