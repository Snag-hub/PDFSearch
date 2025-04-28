using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.VisualBasic.Devices;
using PDFSearch;
using PDFSearch.Helpers;
using PDFSearch.Utilities;
using Serilog;
using Directory = System.IO.Directory;

namespace FindInPDFs.Utilities;

public static class LuceneIndexer
{
    static LuceneIndexer()
    {
        FolderUtility.EnsureBasePathExists();

        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);

        var logPath = Path.Combine(logDir, "log-.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("OptimizedLuceneIndexerBatchCommit initialized.");
        Log.Information("Log path resolved to: {LogPath}", logPath);
    }

    public static void IndexDirectory(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Log.Error("Directory not found: {FolderPath}", folderPath);
            throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");
        }

        Log.Information("Starting indexing for folder: {FolderPath}", folderPath);

        var foldersToIndex = FolderIndexerHelper.GetFoldersToIndex(folderPath);
        var baseIndexPath = FolderUtility.GetFolderForPath(folderPath);
        Directory.CreateDirectory(baseIndexPath);

        var metadata = LoadMetadata(folderPath);

        Parallel.ForEach(foldersToIndex, new ParallelOptions { MaxDegreeOfParallelism = CalculateOptimalParallelism() }, folder =>
        {
            var indexPath = Path.Combine(baseIndexPath, $"index_folder_{Path.GetFileName(folder)}");
            Directory.CreateDirectory(indexPath);

            Log.Information("Indexing folder: '{Folder}'", folder);

            IndexFolder(folder, indexPath, folderPath, metadata);
        });

        SaveMetadata(folderPath, metadata);
        Log.Information("Metadata updated for all folders.");
        UpdateFolderStructure(folderPath);
        Log.Information("All folders indexed successfully.");
    }

    private static void IndexFolder(string folderToIndex, string indexPath, string rootFolderPath,
        Dictionary<string, DateTime> metadata)
    {
        var files = Directory.GetFiles(folderToIndex, "*.pdf", SearchOption.AllDirectories)
            .OrderBy(f => new FileInfo(f).Length)
            .ToList();

        var newOrUpdatedFiles = GetNewOrUpdatedFiles(files, metadata);
        if (!newOrUpdatedFiles.Any())
        {
            Log.Information("No new or updated files in folder: {Folder}", folderToIndex);
            return;
        }

        Log.Information("Found {FileCount} new/updated files in folder: {Folder}", newOrUpdatedFiles.Count, folderToIndex);

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            using var dir = FSDirectory.Open(indexPath);
            using var analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
            var config = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND,
                RAMBufferSizeMB = GetOptimalRamBuffer(),
                MaxBufferedDocs = IndexWriterConfig.DISABLE_AUTO_FLUSH,
                UseCompoundFile = false
            };

            using var writer = new IndexWriter(dir, config);

            int processedCount = 0;
            int batchSize = 50; // Define batch size for committing
            var currentBatchDocs = new List<Document>();

            foreach (var file in newOrUpdatedFiles)
            {
                Log.Information("Processing file: {FilePath} ({Current}/{Total})", file, ++processedCount, newOrUpdatedFiles.Count);

                try
                {
                    var docs = ProcessFile(file, rootFolderPath);
                    currentBatchDocs.AddRange(docs);

                    // Commit the batch if it reaches the batch size
                    if (currentBatchDocs.Count >= batchSize)
                    {
                        CommitBatch(writer, currentBatchDocs);
                        currentBatchDocs.Clear(); // Clear the batch
                    }

                    // Update metadata
                    lock (metadata)
                    {
                        metadata[file] = File.GetLastWriteTimeUtc(file);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing file '{FilePath}'", file);
                }

                Log.Information("Completed processing file: {FilePath} ({Current}/{Total})", file, processedCount, newOrUpdatedFiles.Count);
            }

            // Commit any remaining documents in the batch
            if (currentBatchDocs.Any())
            {
                CommitBatch(writer, currentBatchDocs);
            }

            writer.Commit(); // Final commit
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }

        Log.Information("Finished indexing folder: {Folder}", folderToIndex);
    }

    private static List<Document> ProcessFile(string filePath, string rootFolderPath)
    {
        Log.Information("Starting text extraction for file: {FilePath}", filePath);

        var pages = PdfHelper.ExtractTextFromPdfWithBatching(filePath);
        if (pages.Count == 0)
        {
            Log.Warning("No text found in file: {FilePath}", filePath);
            return [];
        }

        var docs = pages.Select(page =>
        {
            var doc = new Document
            {
                new StringField("FilePath", filePath, Field.Store.YES),
                new StringField("RelativePath", Path.GetRelativePath(rootFolderPath, filePath), Field.Store.YES),
                new Int32Field("PageNumber", page.Key, Field.Store.YES),
                new TextField("Content", page.Value, Field.Store.YES)
            };
            return doc;
        }).ToList();

        Log.Information("Extracted {PageCount} pages from file: {FilePath}", docs.Count, filePath);
        return docs;
    }

    private static void CommitBatch(IndexWriter writer, List<Document> docs)
    {
        lock (writer)
        {
            Log.Information("Committing batch of {BatchSize} documents to index...", docs.Count);
            writer.AddDocuments(docs);
            writer.Commit();
            Log.Information("Batch committed successfully.");
        }
    }

    private static void UpdateFolderStructure(string folderPath)
    {
        var currentFolders = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories).ToList();
        var folderStructure = FolderManager.LoadFolderStructure(folderPath);
        var newFolders = currentFolders.Except(folderStructure).ToList();

        if (newFolders.Count != 0)
        {
            folderStructure.AddRange(newFolders);
            FolderManager.SaveFolderStructure(folderPath);
            Log.Information("Added {NewFolderCount} new folders to the structure.", newFolders.Count);
        }
        else
        {
            Log.Information("No new folders to add.");
        }
    }

    private static int CalculateOptimalParallelism()
    {
        return Math.Max(1, Environment.ProcessorCount / 2);
    }

    private static double GetOptimalRamBuffer()
    {
        var totalMemoryMB = new ComputerInfo().TotalPhysicalMemory / (1024 * 1024);
        return totalMemoryMB switch
        {
            >= 32000 => 2048,
            >= 16000 => 1024,
            >= 9000 => 768,
            >= 7000 => 512,
            >= 4000 => 256,
            _ => 128
        };
    }

    private static Dictionary<string, DateTime> LoadMetadata(string folderPath)
    {
        var metadataFilePath = Path.Combine(FolderUtility.GetFolderForPath(folderPath), "indexedFiles.json");
        return File.Exists(metadataFilePath) ?
            JsonSerializer.Deserialize<Dictionary<string, DateTime>>(File.ReadAllText(metadataFilePath)) ?? new Dictionary<string, DateTime>() :
            new Dictionary<string, DateTime>();
    }

    private static List<string> GetNewOrUpdatedFiles(List<string> files, Dictionary<string, DateTime> metadata)
    {
        return files.Where(file =>
        {
            var lastModified = File.GetLastWriteTimeUtc(file);
            return !metadata.TryGetValue(file, out var indexedTime) || lastModified > indexedTime;
        }).ToList();
    }

    private static void SaveMetadata(string folderPath, Dictionary<string, DateTime> metadata)
    {
        var metadataFilePath = Path.Combine(FolderUtility.GetFolderForPath(folderPath), "indexedFiles.json");
        File.WriteAllText(metadataFilePath, JsonSerializer.Serialize(metadata));
    }
}