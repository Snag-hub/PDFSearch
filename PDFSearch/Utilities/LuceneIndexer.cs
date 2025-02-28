using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Directory = System.IO.Directory;

namespace PDFSearch.Utilities;

public static class LuceneIndexer
{
    static LuceneIndexer()
    {
        FolderUtility.EnsureBasePathExists();
    }

    private static string GetMetadataFilePath(string folderPath)
    {
        string hashedFolderPath = FolderUtility.GetFolderForPath(folderPath);
        Directory.CreateDirectory(hashedFolderPath);
        return Path.Combine(hashedFolderPath, "indexedFiles.json");
    }

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

    private static void SaveMetadata(string folderPath, Dictionary<string, DateTime> metadata)
    {
        var json = JsonSerializer.Serialize(metadata);
        string metadataFilePath = GetMetadataFilePath(folderPath);
        File.WriteAllText(metadataFilePath, json);
    }

    private static List<string> GetNewOrUpdatedFiles(string folderPath, Dictionary<string, DateTime> metadata)
    {
        var allFiles = Directory.GetFiles(folderPath, "*.pdf", SearchOption.AllDirectories).ToList();
        var newOrUpdatedFiles = new List<string>();

        foreach (var file in allFiles)
        {
            var lastModified = File.GetLastWriteTimeUtc(file);
            if (!metadata.TryGetValue(file, out var indexedTime) || indexedTime < lastModified)
            {
                newOrUpdatedFiles.Add(file);
            }
        }

        return newOrUpdatedFiles;
    }

    public static void IndexDirectory(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");

        string uniqueIndexPath = FolderUtility.GetFolderForPath(folderPath);
        Directory.CreateDirectory(uniqueIndexPath);

        var metadata = LoadMetadata(folderPath);
        var updatedMetadata = new ConcurrentDictionary<string, DateTime>();

        using var dir = FSDirectory.Open(uniqueIndexPath);
        using var analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);

        // Dynamically set RAM buffer size based on system RAM
        double ramBufferMB = GetOptimalRamBuffer();
        Console.WriteLine($"Using RAM Buffer: {ramBufferMB}MB");

        var config = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND,
            RAMBufferSizeMB = ramBufferMB,
            MaxBufferedDocs = 2000,  // Increase buffered docs to improve indexing efficiency
            MergeScheduler = new ConcurrentMergeScheduler() // Allow merging to run in parallel
        };

        // 🔥 Optimized Merge Policy
        var mergePolicy = new TieredMergePolicy
        {
            MaxMergeAtOnce = 20,     // Merge more segments in one go
            SegmentsPerTier = 15,
            NoCFSRatio = 1.0 // Increase the tier size to reduce frequent merges
        };
        config.SetMergePolicy(mergePolicy);

        using var writer = new IndexWriter(dir, config);

        // Check for new or updated files
        var newOrUpdatedFiles = GetNewOrUpdatedFiles(folderPath, metadata);

        if (newOrUpdatedFiles.Any())
        {
            // Process files in parallel (Adjust degree of parallelism if needed)
            Parallel.ForEach(newOrUpdatedFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, pdfFile =>
            {
                try
                {
                    var textByPage = PdfHelper.ExtractTextFromPdfPages(pdfFile);
                    string relativePath = Path.GetRelativePath(folderPath, pdfFile);

                    var docs = new List<Document>();
                    foreach (var (page, text) in textByPage)
                    {
                        var doc = new Document
                        {
                            new StringField("FilePath", pdfFile, Field.Store.YES),
                            new StringField("RelativePath", relativePath, Field.Store.YES),
                            new Int32Field("PageNumber", page, Field.Store.YES),
                            new TextField("Content", text, Field.Store.YES)
                        };
                        docs.Add(doc);
                    }

                    lock (writer)
                    {
                        writer.AddDocuments(docs);
                    }

                    updatedMetadata[pdfFile] = File.GetLastWriteTimeUtc(pdfFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file '{pdfFile}': {ex.Message}");
                }
            });

            // Merge indexes after processing
            writer.Flush(triggerMerge: true, applyAllDeletes: false);
            writer.Commit(); // Commit the changes

            // Update the metadata with the new or updated files
            lock (metadata)
            {
                foreach (var entry in updatedMetadata)
                {
                    metadata[entry.Key] = entry.Value;
                }
                SaveMetadata(folderPath, metadata);
                Console.WriteLine("Metadata updated.");
            }

            Console.WriteLine($"Indexed {newOrUpdatedFiles.Count} new or updated files.");  // For debugging
        }
        else
        {
            Console.WriteLine("No new or updated files to index.");  // For debugging
        }

        // Update the folder structure with any new folders
        UpdateFolderStructure(folderPath);
    }

    private static void UpdateFolderStructure(string folderPath)
    {
        // Get the current folder structure
        var currentFolders = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories).ToList();

        // Load the existing folder structure from the JSON file
        var folderStructure = FolderManager.LoadFolderStructure(folderPath);

        // Check for new folders
        var newFolders = currentFolders.Except(folderStructure).ToList();

        if (newFolders.Any())
        {
            // Add new folders to the folder structure
            folderStructure.AddRange(newFolders);

            // Save the updated folder structure
            FolderManager.SaveFolderStructure(folderPath);

            Console.WriteLine($"Added {newFolders.Count} new folders to the folder structure.");
        }
        else
        {
            Console.WriteLine("No new folders to add.");
        }
    }

    // Function to determine optimal RAM buffer based on system RAM
    private static double GetOptimalRamBuffer()
    {
        long totalMemoryMB = (long)(new ComputerInfo().TotalPhysicalMemory / (1024 * 1024));
        return totalMemoryMB switch
        {
            >= 16000 => 1024,  // 16GB+ → 1GB Buffer
            >= 8000 => 512,   // 8GB+ → 512MB Buffer
            _ => 256    // Below 8GB → 256MB Buffer
        };
    }
}