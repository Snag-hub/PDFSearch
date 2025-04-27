using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Microsoft.VisualBasic.Devices;
using PDFSearch.Helpers;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

        string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);

        string logPath = Path.Combine(logDir, "log-.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("LuceneIndexer initialized.");
        Log.Information("Log path resolved to: {LogPath}", logPath);
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
            return JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json) ?? [];
        }

        return [];
    }

    private static void SaveMetadata(string folderPath, Dictionary<string, DateTime> metadata)
    {
        var json = JsonSerializer.Serialize(metadata);
        string metadataFilePath = GetMetadataFilePath(folderPath);
        File.WriteAllText(metadataFilePath, json);
    }

    private static List<string> GetNewOrUpdatedFiles(List<string> files, Dictionary<string, DateTime> metadata)
    {
        var newOrUpdatedFiles = new List<string>();
        foreach (var file in files)
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
        {
            Log.Error("Directory not found: {FolderPath}", folderPath);
            throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");
        }

        var foldersToIndex = FolderIndexerHelper.GetFoldersToIndex(folderPath);
        string baseIndexPath = FolderUtility.GetFolderForPath(folderPath);
        Directory.CreateDirectory(baseIndexPath);

        var metadata = LoadMetadata(folderPath);

        for (int i = 0; i < foldersToIndex.Count; i++)
        {
            string folder = foldersToIndex[i];
            string indexPath = Path.Combine(baseIndexPath, $"index_folder_{i}");
            Directory.CreateDirectory(indexPath);
            Log.Information("Indexing folder {CurrentFolder}/{TotalFolders}: '{Folder}'", i + 1, foldersToIndex.Count,
                folder);

            IndexFolder(folder, indexPath, folderPath, metadata);
        }

        SaveMetadata(folderPath, metadata);
        Log.Information("Metadata updated for all folders.");
        UpdateFolderStructure(folderPath);
        Log.Information("All folders indexed successfully.");
    }

    private static void IndexFolder(string folderToIndex, string indexPath, string rootFolderPath,
        Dictionary<string, DateTime> metadata)
    {
        var updatedMetadata = new ConcurrentDictionary<string, DateTime>();
        var stopwatch = Stopwatch.StartNew();
        var lockObject = new object(); // Define lockObject locally

        using var dir = FSDirectory.Open(indexPath);
        using var analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);

        double ramBufferMB = GetOptimalRamBuffer();
        Log.Information("Using RAM Buffer: {RamBufferMB}MB for folder '{Folder}'", ramBufferMB, folderToIndex);

        var config = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer)
        {
            OpenMode = OpenMode.CREATE,
            RAMBufferSizeMB = ramBufferMB,
            MaxBufferedDocs = 2000,
            MergeScheduler = new ConcurrentMergeScheduler()
        };

        var mergePolicy = new TieredMergePolicy
        {
            MaxMergeAtOnce = 5,
            SegmentsPerTier = 10,
            NoCFSRatio = 1.0
        };
        config.SetMergePolicy(mergePolicy);

        using var writer = new IndexWriter(dir, config);

        var files = Directory.GetFiles(folderToIndex, "*.pdf", SearchOption.AllDirectories).ToList();
        var newOrUpdatedFiles = GetNewOrUpdatedFiles(files, metadata);

        if (newOrUpdatedFiles.Count != 0)
        {
            // Classify files
            var largeFiles = new Queue<string>();
            var smallFiles = new Queue<string>();
            foreach (var file in newOrUpdatedFiles)
            {
                var pageCount = PdfHelper.GetPageCount(file);
                var fileSize = new FileInfo(file).Length;
                if (fileSize > PdfHelper.LargeFileSizeThreshold ||
                    (pageCount.HasValue && pageCount.Value > PdfHelper.LargeFilePageThreshold))
                    largeFiles.Enqueue(file);
                else
                    smallFiles.Enqueue(file);
            }

            Log.Information("Starting processing: {LargeCount} large files, {SmallCount} small files", largeFiles.Count,
                smallFiles.Count);

            // Process large files sequentially
            while (largeFiles.Count > 0)
            {
                var largeFile = largeFiles.Dequeue();
                Log.Information("Processing large file: {FilePath} (Size: {Size} MB, Pages: {Pages})", largeFile,
                    new FileInfo(largeFile).Length / (1024 * 1024), PdfHelper.GetPageCount(largeFile) ?? 0);
                try
                {
                    var docs = new List<Document>();
                    int docCount = 0;
                    var batchStopwatch = Stopwatch.StartNew();

                    var pages = PdfHelper.ExtractTextFromPdfPages(largeFile).Result;
                    foreach (var page in pages)
                    {
                        var doc = new Document
                        {
                            new StringField("FilePath", largeFile, Field.Store.YES),
                            new StringField("RelativePath", Path.GetRelativePath(rootFolderPath, largeFile),
                                Field.Store.YES),
                            new Int32Field("PageNumber", page.Key, Field.Store.YES),
                            new TextField("Content", page.Value ?? "", Field.Store.YES)
                        };
                        docs.Add(doc);
                        docCount++;

                        if (docs.Count >= 100)
                        {
                            lock (writer)
                            {
                                writer.AddDocuments(docs);
                                writer.Flush(triggerMerge: false, applyAllDeletes: false);
                            }

                            Log.Information("Flushed batch of 100 documents for '{FilePath}' in {ElapsedSeconds:F2}s",
                                largeFile, batchStopwatch.Elapsed.TotalSeconds);
                            docs.Clear();
                            GC.Collect();
                            batchStopwatch.Restart();

                            if (batchStopwatch.Elapsed.TotalMinutes > 5)
                            {
                                Log.Warning(
                                    "Batch processing for '{FilePath}' took over 5 minutes—potential stall detected.",
                                    largeFile);
                            }

                            // Process small files in parallel during large file pauses
                            if (smallFiles.Count > 0 && PdfHelper.IsMemorySafe())
                            {
                                Parallel.ForEach(smallFiles,
                                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 },
                                    smallFile =>
                                    {
                                        try
                                        {
                                            var smallDocs = new List<Document>();
                                            int smallDocCount = 0;
                                            var smallBatchStopwatch = Stopwatch.StartNew();

                                            var smallPages = PdfHelper.ExtractTextFromPdfPages(smallFile).Result;
                                            foreach (var smallPage in smallPages)
                                            {
                                                var doc = new Document
                                                {
                                                    new StringField("FilePath", smallFile, Field.Store.YES),
                                                    new StringField("RelativePath",
                                                        Path.GetRelativePath(rootFolderPath, smallFile),
                                                        Field.Store.YES),
                                                    new Int32Field("PageNumber", smallPage.Key, Field.Store.YES),
                                                    new TextField("Content", smallPage.Value ?? "", Field.Store.YES)
                                                };
                                                smallDocs.Add(doc);
                                                smallDocCount++;

                                                if (smallDocs.Count >= 100)
                                                {
                                                    lock (writer)
                                                    {
                                                        writer.AddDocuments(smallDocs);
                                                        writer.Flush(triggerMerge: false, applyAllDeletes: false);
                                                    }

                                                    Log.Information(
                                                        "Flushed batch of 100 documents for '{FilePath}' in {ElapsedSeconds:F2}s",
                                                        smallFile, smallBatchStopwatch.Elapsed.TotalSeconds);
                                                    smallDocs.Clear();
                                                    GC.Collect();
                                                    smallBatchStopwatch.Restart();
                                                }
                                            }

                                            if (smallDocs.Count != 0)
                                            {
                                                lock (writer)
                                                {
                                                    writer.AddDocuments(smallDocs);
                                                    writer.Flush(triggerMerge: false, applyAllDeletes: false);
                                                }

                                                Log.Information(
                                                    "Flushed final batch of {DocCount} documents for '{FilePath}' in {ElapsedSeconds:F2}s",
                                                    smallDocs.Count, smallFile,
                                                    smallBatchStopwatch.Elapsed.TotalSeconds);
                                                smallDocs.Clear();
                                                GC.Collect();
                                            }

                                            lock (writer)
                                            {
                                                writer.Commit();
                                            }

                                            Log.Information("Committed '{FilePath}' with {DocCount} documents.",
                                                smallFile, smallDocCount);
                                            lock (lockObject)
                                            {
                                                updatedMetadata[smallFile] = File.GetLastWriteTimeUtc(smallFile);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error(ex, "Error processing small file '{FilePath}'", smallFile);
                                        }
                                    });
                                smallFiles.Clear(); // Clear processed small files
                            }
                        }
                    }

                    if (docs.Count != 0)
                    {
                        lock (writer)
                        {
                            writer.AddDocuments(docs);
                            writer.Flush(triggerMerge: false, applyAllDeletes: false);
                        }

                        Log.Information(
                            "Flushed final batch of {DocCount} documents for '{FilePath}' in {ElapsedSeconds:F2}s",
                            docs.Count, largeFile, batchStopwatch.Elapsed.TotalSeconds);
                        docs.Clear();
                        GC.Collect();
                    }

                    lock (writer)
                    {
                        writer.Commit();
                    }

                    Log.Information("Committed '{FilePath}' with {DocCount} documents.", largeFile, docCount);
                    lock (lockObject)
                    {
                        updatedMetadata[largeFile] = File.GetLastWriteTimeUtc(largeFile);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing large file '{FilePath}'", largeFile);
                }
            }

            // Process remaining small files
            if (smallFiles.Count > 0)
            {
                Parallel.ForEach(smallFiles,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 }, smallFile =>
                    {
                        if (PdfHelper.IsMemorySafe())
                        {
                            try
                            {
                                var smallDocs = new List<Document>();
                                int smallDocCount = 0;
                                var smallBatchStopwatch = Stopwatch.StartNew();

                                var smallPages = PdfHelper.ExtractTextFromPdfPages(smallFile).Result;
                                foreach (var smallPage in smallPages)
                                {
                                    var doc = new Document
                                    {
                                        new StringField("FilePath", smallFile, Field.Store.YES),
                                        new StringField("RelativePath", Path.GetRelativePath(rootFolderPath, smallFile),
                                            Field.Store.YES),
                                        new Int32Field("PageNumber", smallPage.Key, Field.Store.YES),
                                        new TextField("Content", smallPage.Value ?? "", Field.Store.YES)
                                    };
                                    smallDocs.Add(doc);
                                    smallDocCount++;

                                    if (smallDocs.Count >= 100)
                                    {
                                        lock (writer)
                                        {
                                            writer.AddDocuments(smallDocs);
                                            writer.Flush(triggerMerge: false, applyAllDeletes: false);
                                        }

                                        Log.Information(
                                            "Flushed batch of 100 documents for '{FilePath}' in {ElapsedSeconds:F2}s",
                                            smallFile, smallBatchStopwatch.Elapsed.TotalSeconds);
                                        smallDocs.Clear();
                                        GC.Collect();
                                        smallBatchStopwatch.Restart();
                                    }
                                }

                                if (smallDocs.Count != 0)
                                {
                                    lock (writer)
                                    {
                                        writer.AddDocuments(smallDocs);
                                        writer.Flush(triggerMerge: false, applyAllDeletes: false);
                                    }

                                    Log.Information(
                                        "Flushed final batch of {DocCount} documents for '{FilePath}' in {ElapsedSeconds:F2}s",
                                        smallDocs.Count, smallFile, smallBatchStopwatch.Elapsed.TotalSeconds);
                                    smallDocs.Clear();
                                    GC.Collect();
                                }

                                lock (writer)
                                {
                                    writer.Commit();
                                }

                                Log.Information("Committed '{FilePath}' with {DocCount} documents.", smallFile,
                                    smallDocCount);
                                lock (lockObject)
                                {
                                    updatedMetadata[smallFile] = File.GetLastWriteTimeUtc(smallFile);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Error processing small file '{FilePath}'", smallFile);
                            }
                        }
                    });
            }

            Log.Information("Parallel processing complete. Finalizing index...");
            writer.Flush(triggerMerge: true, applyAllDeletes: false);
            writer.Commit();
            Log.Information("Committed final changes to index.");

            // Calculate resource usage
            var process = Process.GetCurrentProcess();
            long totalMemory = (long)(new ComputerInfo().TotalPhysicalMemory);
            double ramUsagePercent = (double)process.WorkingSet64 / totalMemory;
            double cpuUsagePercent =
                Helpers.Helpers.GetCpuUsage(process); // Assuming Helpers.GetCpuUsage is defined elsewhere

            lock (metadata)
            {
                foreach (var entry in updatedMetadata)
                {
                    metadata[entry.Key] = entry.Value;
                }
            }

            Log.Information(
                "Folder '{Folder}' indexed: {FileCount} files in {ElapsedSeconds:F2}s (RAM: {RamUsagePercent:P0}, CPU: {CpuUsagePercent:F1}%)",
                folderToIndex, newOrUpdatedFiles.Count, stopwatch.Elapsed.TotalSeconds, ramUsagePercent,
                cpuUsagePercent);
        }
        else
        {
            Log.Information("No new or updated files in folder '{Folder}'.", folderToIndex);
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

    private static double GetOptimalRamBuffer()
    {
        long totalMemoryMB = (long)(new ComputerInfo().TotalPhysicalMemory / (1024 * 1024));
        double baseBuffer = totalMemoryMB switch
        {
            >= 32000 => 2048,
            >= 16000 => 1024,
            >= 9000 => 768,
            >= 7000 => 512, // Your 7.5 GB PC
            >= 4000 => 256,
            _ => 128
        };
        return baseBuffer * 2; // 1024 MB for your PC
    }
}