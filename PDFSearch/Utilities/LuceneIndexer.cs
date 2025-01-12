using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Directory = System.IO.Directory;
using System.Collections.Concurrent;

namespace PDFSearch.Utilities;

public static class LuceneIndexer
{
    // Define a base path for storing application-specific data in AppData
    private static readonly string AppDataBasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "No1Knows",
        "Index"
    );

    // Metadata file path to store information about indexed files
    private static readonly string MetadataFilePath = Path.Combine(AppDataBasePath, "indexedFiles.json");

    // Ensure the base path exists when the application starts
    static LuceneIndexer()
    {
        Directory.CreateDirectory(AppDataBasePath);
    }

    // Load metadata from the file to track indexed files
    private static Dictionary<string, DateTime> LoadMetadata()
    {
        if (File.Exists(MetadataFilePath))
        {
            var json = File.ReadAllText(MetadataFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json) ?? new Dictionary<string, DateTime>();
        }
        return new Dictionary<string, DateTime>();
    }

    // Save metadata to the file after updating
    private static void SaveMetadata(Dictionary<string, DateTime> metadata)
    {
        var json = JsonSerializer.Serialize(metadata);

        // Ensure the directory exists before saving
        Directory.CreateDirectory(AppDataBasePath);
        File.WriteAllText(MetadataFilePath, json);
    }

    // Generate a unique index folder name for each directory
    private static string GetIndexFolderName(string folderPath)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(folderPath));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    // Index files in a specific folder
    public static void IndexDirectory(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");

        // Create an index folder based on the folder path within AppData
        string uniqueIndexPath = Path.Combine(AppDataBasePath, GetIndexFolderName(folderPath));
        Directory.CreateDirectory(uniqueIndexPath);

        var metadata = LoadMetadata();
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

                // Batch Lucene documents
                var docs = new List<Document>();
                foreach (var (page, text) in textByPage)
                {
                    var doc = new Document
                    {
                        new StringField("FilePath", pdfFile, Field.Store.YES),
                        new Int32Field("PageNumber", page, Field.Store.YES),
                        new TextField("Content", text, Field.Store.YES)
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

            SaveMetadata(metadata);
            Console.WriteLine("Metadata updated.");
        }

        Console.WriteLine($"Indexing completed for directory: {folderPath}. Index stored at: {uniqueIndexPath}");
    }

    // Clean all existing indexes and metadata
    public static void CleanAllIndexes()
    {
        if (Directory.Exists(AppDataBasePath))
        {
            Directory.Delete(AppDataBasePath, recursive: true);
            Console.WriteLine("All indexes have been cleaned.");
        }
    }
}
