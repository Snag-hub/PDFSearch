using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Search;
using Directory = System.IO.Directory;

namespace PDFSearch;

public static class LuceneIndexer
{
    private const string IndexPath = "LuceneIndex"; // Directory to store index

    /// <summary>
    /// Indexes a directory of PDF files, appending new files to the existing index and ignoring already indexed ones.
    /// </summary>
    /// <param name="directoryPath">The directory containing PDF files.</param>
    public static void IndexDirectory(string directoryPath)
    {
        try
        {
            // Ensure the index directory exists
            Directory.CreateDirectory(IndexPath);

            using var directory = FSDirectory.Open(IndexPath);
            using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND // Append to the existing index
            };

            using var writer = new IndexWriter(directory, config);
            using var reader = DirectoryReader.Open(writer, applyAllDeletes: false);
            var searcher = new IndexSearcher(reader);

            foreach (var pdfFilePath in Directory.GetFiles(directoryPath, "*.pdf"))
            {
                var lastModified = File.GetLastWriteTime(pdfFilePath);

                if (!IsFileIndexed(pdfFilePath, lastModified, searcher))
                {
                    var textByPage = PdfHelper.ExtractTextFromPdf(pdfFilePath);
                    foreach (var doc in textByPage.Select(page => new Document
                         {
                             new StringField("FilePath", pdfFilePath, Field.Store.YES),
                             new StringField("LastModified", lastModified.ToString("o"),
                                 Field.Store.YES), // ISO 8601 format
                             new Int32Field("PageNumber", page.Key, Field.Store.YES),
                             new TextField("Content", page.Value, Field.Store.YES)
                         }))
                    {
                        writer.AddDocument(doc);
                    }

                    Console.WriteLine($@"Indexed file: {pdfFilePath}");
                }
                else
                {
                    Console.WriteLine($@"Skipped already indexed file: {pdfFilePath}");
                }
            }

            writer.Commit();
            Console.WriteLine($@"Indexing complete for directory: {directoryPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Error indexing directory {directoryPath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a file is already indexed and up-to-date in the Lucene index.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <param name="lastModified">The last modified timestamp of the file.</param>
    /// <param name="searcher">Lucene IndexSearcher to query the index.</param>
    /// <returns>True if the file is already indexed and up-to-date; otherwise, false.</returns>
    private static bool IsFileIndexed(string filePath, DateTime lastModified, IndexSearcher searcher)
    {
        var query = new TermQuery(new Term("FilePath", filePath));
        var hits = searcher.Search(query, 1).ScoreDocs;

        if (hits.Length == 0)
            return false; // File is not indexed

        var doc = searcher.Doc(hits[0].Doc);
        var indexedLastModified = DateTime.Parse(doc.Get("LastModified"));

        return indexedLastModified >= lastModified;
    }
}
