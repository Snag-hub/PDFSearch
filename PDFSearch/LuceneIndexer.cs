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

namespace PDFSearch;

public static class LuceneIndexer
{
    // Metadata file path to store information about indexed files
    private static readonly string MetadataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "indexedFiles.json");

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

        // Create an index folder based on the folder path
        string baseIndexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index");
        string uniqueIndexPath = Path.Combine(baseIndexPath, GetIndexFolderName(folderPath));
        Directory.CreateDirectory(uniqueIndexPath);

        var metadata = LoadMetadata(); // Load existing metadata
        bool metadataUpdated = false;

        using var dir = FSDirectory.Open(uniqueIndexPath);
        using var analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
        using var writer = new IndexWriter(dir, new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer));

        var pdfFiles = Directory.GetFiles(folderPath, "*.pdf", SearchOption.AllDirectories);

        foreach (var pdfFile in pdfFiles)
        {
            var lastModified = File.GetLastWriteTimeUtc(pdfFile);

            // Skip indexing if the file has already been indexed and not modified
            if (metadata.TryGetValue(pdfFile, out var indexedTime) && indexedTime >= lastModified)
            {
                Console.WriteLine($"Skipping already indexed file (no changes): {pdfFile}");
                continue; // File is already indexed and hasn't been modified
            }

            try
            {
                // Extract text from PDF pages
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

                // Update metadata for the indexed file
                metadata[pdfFile] = lastModified;
                metadataUpdated = true;

                // Debug message indicating successful indexing of the file
                Console.WriteLine($"Successfully indexed file: {pdfFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file '{pdfFile}': {ex.Message}");
            }
        }

        writer.Flush(triggerMerge: false, applyAllDeletes: false);

        // Save updated metadata if any files were indexed
        if (metadataUpdated)
        {
            SaveMetadata(metadata);
            Console.WriteLine("Metadata updated.");
        }

        Console.WriteLine($"Indexing completed for directory: {folderPath}. Index stored at: {uniqueIndexPath}");
    }

    // Clean all existing indexes and metadata
    public static void CleanAllIndexes()
    {
        string baseIndexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index");
        if (Directory.Exists(baseIndexPath))
        {
            Directory.Delete(baseIndexPath, recursive: true);
            Console.WriteLine("All indexes have been cleaned.");
        }

        if (File.Exists(MetadataFilePath))
        {
            File.Delete(MetadataFilePath);
            Console.WriteLine("Metadata has been cleaned.");
        }
    }
}
