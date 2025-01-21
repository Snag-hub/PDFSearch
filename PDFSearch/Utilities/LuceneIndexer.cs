using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using System.Text.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Directory = System.IO.Directory;

namespace PDFSearch.Utilities;

public static class LuceneIndexer
{
    // Ensure the base path exists when the application starts
    static LuceneIndexer()
    {
        FolderUtility.EnsureBasePathExists();
    }

    // Get the path to the metadata file for a given folder path
    private static string GetMetadataFilePath(string folderPath)
    {
        // Get the hashed folder for the folder path
        string hashedFolderPath = FolderUtility.GetFolderForPath(folderPath);

        // Ensure the hashed folder exists
        Directory.CreateDirectory(hashedFolderPath);

        // Return the path to the metadata file inside the hashed folder
        return Path.Combine(hashedFolderPath, "indexedFiles.json");
    }

    // Load metadata from the file to track indexed files
    private static Dictionary<string, DateTime> LoadMetadata(string folderPath)
    {
        string metadataFilePath = GetMetadataFilePath(folderPath);

        if (File.Exists(metadataFilePath))
        {
            var json = File.ReadAllText(metadataFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json) ?? new Dictionary<string, DateTime>();
        }
        return new Dictionary<string, DateTime>();
    }

    // Save metadata to the file after updating
    private static void SaveMetadata(string folderPath, Dictionary<string, DateTime> metadata)
    {
        var json = JsonSerializer.Serialize(metadata);

        // Get the path to the metadata file inside the hashed folder
        string metadataFilePath = GetMetadataFilePath(folderPath);

        // Write the JSON to the file
        File.WriteAllText(metadataFilePath, json);
    }

    // Index files in a specific folder
    public static void IndexDirectory(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");

        // Use FolderUtility to get a unique index folder path
        string uniqueIndexPath = FolderUtility.GetFolderForPath(folderPath);
        Directory.CreateDirectory(uniqueIndexPath);

        var metadata = LoadMetadata(folderPath);
        var updatedMetadata = new ConcurrentDictionary<string, DateTime>();

        using var dir = FSDirectory.Open(uniqueIndexPath);
        using var analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
        var config = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND
        };

        using var writer = new IndexWriter(dir, config);
        var pdfFiles = Directory.GetFiles(folderPath, "*.pdf", SearchOption.AllDirectories);

        // Process files in parallel
        Parallel.ForEach(pdfFiles, pdfFile =>
        {
            try
            {
                var lastModified = File.GetLastWriteTimeUtc(pdfFile);
                if (metadata.TryGetValue(pdfFile, out var indexedTime) && indexedTime >= lastModified)
                {
                    Console.WriteLine($"Skipping already indexed file (no changes): {pdfFile}");
                    return;
                }

                var textByPage = PdfHelper.ExtractTextFromPdfPages(pdfFile);

                // Calculate the relative path
                string relativePath = Path.GetRelativePath(folderPath, pdfFile);

                // Batch Lucene documents
                var docs = new List<Document>();
                foreach (var (page, text) in textByPage)
                {
                    var doc = new Document
                    {
                        new StringField("FilePath", pdfFile, Field.Store.YES),      // Absolute file path
                        new StringField("RelativePath", relativePath, Field.Store.YES), // Relative file path
                        new Int32Field("PageNumber", page, Field.Store.YES),       // Page number
                        new TextField("Content", text, Field.Store.YES)           // PDF content
                    };
                    docs.Add(doc);
                }

                lock (writer) // Synchronize access to the index writer
                {
                    writer.AddDocuments(docs);
                }

                updatedMetadata[pdfFile] = lastModified;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file '{pdfFile}': {ex.Message}");
            }
        });

        writer.Flush(triggerMerge: false, applyAllDeletes: false);

        // Update metadata after processing
        lock (metadata)
        {
            foreach (var entry in updatedMetadata)
            {
                metadata[entry.Key] = entry.Value;
            }

            SaveMetadata(folderPath, metadata);
            Console.WriteLine("Metadata updated.");
        }

        Console.WriteLine($"Indexing completed for directory: {folderPath}. Index stored at: {uniqueIndexPath}");
    }

    // Clean all existing indexes and metadata
    public static void CleanAllIndexes()
    {
        FolderUtility.CleanAllFolders();
        Console.WriteLine("All indexes have been cleaned.");
    }
}