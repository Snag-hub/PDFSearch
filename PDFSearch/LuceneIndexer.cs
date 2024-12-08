using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Search;
using Directory = System.IO.Directory;

namespace PDFSearch;

public static class LuceneIndexer
{
    private const string IndexPath = "Indexed Docs"; // Directory to store index

    /// <summary>
    /// Indexes a directory and all of its subdirectories of PDF files, appending new files to the existing index and ignoring already indexed ones.
    /// </summary>
    /// <param name="directoryPath">The root directory containing PDF files.</param>
    public static void IndexDirectory(string directoryPath)
    {
        try
        {
            // Ensure the index directory exists
            Directory.CreateDirectory(IndexPath);

            // Initialize the Lucene directory and analyzer
            using var directory = FSDirectory.Open(IndexPath);
            using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND // Append to the existing index
            };

            // Initialize IndexWriter once outside the loop
            using var writer = new IndexWriter(directory, config);

            // Get all the PDF files in the directory and subdirectories
            var pdfFiles = Directory.GetFiles(directoryPath, "*.pdf", SearchOption.AllDirectories);

            // Lock object to synchronize the access to the index writing operations
            var lockObject = new object();

            // Parallel processing of PDF files
            Parallel.ForEach(pdfFiles, pdfFilePath =>
            {
                try
                {
                    var lastModified = File.GetLastWriteTime(pdfFilePath);

                    // Ensure file is indexed if not already
                    lock (lockObject)
                    {
                        // Use the IndexWriter to check if the file is already indexed
                        using var reader = DirectoryReader.Open(writer, applyAllDeletes: false);
                        var searcher = new IndexSearcher(reader);

                        // Check if the file is indexed and if the index is up-to-date
                        if (!IsFileIndexed(pdfFilePath, lastModified, searcher))
                        {
                            // Extract text from PDF file
                            var textByPage = PdfHelper.ExtractTextFromPdf(pdfFilePath);

                            // Add each page to the index
                            foreach (var page in textByPage.Select(page => new Document
                                     {
                                         new StringField("FilePath", pdfFilePath, Field.Store.YES),
                                         new StringField("LastModified", lastModified.ToString("o"),
                                             Field.Store.YES), // ISO 8601 format
                                         new Int32Field("PageNumber", page.Key, Field.Store.YES),
                                         new TextField("Content", page.Value, Field.Store.YES)
                                     }))
                            {
                                writer.AddDocument(page);
                            }

                            Console.WriteLine($@"Indexed file: {pdfFilePath}");
                        }
                        else
                        {
                            Console.WriteLine($@"Skipped already indexed file: {pdfFilePath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"Error processing {pdfFilePath}: {ex.Message}");
                }
            });

            // Commit changes after all files are processed
            writer.Commit();
            Console.WriteLine($@"Indexing complete for directory: {directoryPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Error indexing directory {directoryPath}: {ex.Message}");
        }
    }


    /// <summary>
    /// Cleans the Lucene index directory by deleting all files in it.
    /// </summary>
    public static void CleanIndexDirectory()
    {
        try
        {
            // If the index directory exists, delete all files inside it
            if (!Directory.Exists(IndexPath))
            {
                Console.WriteLine($@"Index directory does not exist: {IndexPath}");
            }

            foreach (var file in Directory.GetFiles(IndexPath))
            {
                File.Delete(file); // Delete each file in the index folder
            }

            Console.WriteLine($@"Index directory cleaned: {IndexPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Error cleaning index directory: {ex.Message}");
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
