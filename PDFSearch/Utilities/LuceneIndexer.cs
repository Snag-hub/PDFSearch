﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using PDFSearch.Helpers;
using PDFSearch.Utilities;
using Directory = System.IO.Directory;

namespace FindInPDFs.Utilities;

public static class LuceneIndexer
{
    private const int RamBufferSizeMb = 256; // RAM buffer for Lucene indexing
    private const long SmallFileSizeThreshold = 10 * 1024 * 1024; // 10 MB
    private const int DocumentsPerBatch = 100; // Process 100 documents per batch
    private const int CommitIntervalPages = 2000; // Commit every 2000 pages for large files
    private const int MaxDegreeOfParallelism = 2; // Limit parallel tasks

    private class IndexState
    {
        public required string LastIndexedFile { get; set; }
    }

    /// <summary>
    /// Indexes all PDF files in the specified directory with progress and cancellation support.
    /// </summary>
    /// <param name="folderPath">The directory to index.</param>
    /// <param name="progressCallback">Callback to report progress (current, total files).</param>
    /// <param name="cancellationToken">Token to cancel indexing.</param>
    public static void IndexDirectory(string folderPath, Action<int, int> progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");
        }

        Console.WriteLine($"Starting indexing for folder: {folderPath}");
        var stopwatch = Stopwatch.StartNew();

        var foldersToIndex = FolderIndexerHelper.GetFoldersToIndex(folderPath);
        var baseIndexPath = FolderUtility.GetFolderForPath(folderPath);
        Directory.CreateDirectory(baseIndexPath);

        var metadata = LoadMetadata(folderPath);
        var state = LoadIndexState(baseIndexPath);

        int totalFiles = foldersToIndex
            .SelectMany(folder => Directory.GetFiles(folder, "*.pdf", SearchOption.AllDirectories))
            .Select(f => (Path: f, Size: new FileInfo(f).Length))
            .OrderBy(f => f.Size)
            .Select(f => f.Path)
            .Count(file => !metadata.TryGetValue(file, out var indexedTime) || File.GetLastWriteTimeUtc(file) > indexedTime);
        int processedFiles = 0;

        bool skipFiles = state?.LastIndexedFile != null;
        foreach (var folder in foldersToIndex)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var indexPath = Path.Combine(baseIndexPath, $"index_folder_{Path.GetFileName(folder)}");
            Directory.CreateDirectory(indexPath);

            Console.WriteLine($"Indexing folder: {folder}");
            IndexFolder(folder, indexPath, folderPath, metadata, baseIndexPath, state, skipFiles, () =>
            {
                Interlocked.Increment(ref processedFiles);
                progressCallback?.Invoke(processedFiles, totalFiles);
            }, cancellationToken);

            // After first folder, stop skipping files
            skipFiles = false;
        }

        SaveMetadata(folderPath, metadata);
        Console.WriteLine($"All folders indexed successfully. Total time: {stopwatch.ElapsedMilliseconds} ms");
        stopwatch.Stop();
    }

    private static void IndexFolder(string folderToIndex, string indexPath, string rootFolderPath,
        Dictionary<string, DateTime> metadata, string baseIndexPath, IndexState state, bool skipFiles,
        Action onFileProcessed, CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(folderToIndex, "*.pdf", SearchOption.AllDirectories)
            .Select(f => (Path: f, Size: new FileInfo(f).Length))
            .OrderBy(f => f.Size)
            .Select(f => f.Path)
            .ToList();

        var newOrUpdatedFiles = GetNewOrUpdatedFiles(files, metadata);
        if (newOrUpdatedFiles.Count == 0)
        {
            Console.WriteLine($"No new or updated files in folder: {folderToIndex}");
            return;
        }

        // Skip files up to LastIndexedFile
        if (skipFiles && state?.LastIndexedFile != null)
        {
            newOrUpdatedFiles = [.. newOrUpdatedFiles
                .SkipWhile(f => f != state.LastIndexedFile)
                .Skip(1)];
        }

        if (newOrUpdatedFiles.Count == 0)
        {
            Console.WriteLine($"All files in folder {folderToIndex} already indexed up to state.");
            return;
        }

        using var dir = FSDirectory.Open(indexPath);
        using var analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
        var config = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND,
            RAMBufferSizeMB = RamBufferSizeMb,
            UseCompoundFile = false
        };

        using var writer = new IndexWriter(dir, config);
        var stopwatch = Stopwatch.StartNew();
        var metadataUpdates = new ConcurrentBag<(string FilePath, DateTime LastModified)>();

        var smallFiles = newOrUpdatedFiles
            .Where(f => new FileInfo(f).Length < SmallFileSizeThreshold)
            .ToList();
        var largeFiles = newOrUpdatedFiles
            .Where(f => new FileInfo(f).Length >= SmallFileSizeThreshold)
            .ToList();

        if (smallFiles.Count != 0)
        {
            Console.WriteLine($"Processing {smallFiles.Count} small files (< 10MB) in parallel...");
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            };
            Parallel.ForEach(smallFiles, parallelOptions, file =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var fileSize = new FileInfo(file).Length;
                    ProcessFile(file, rootFolderPath, fileSize, writer, true, cancellationToken);
                    metadataUpdates.Add((file, File.GetLastWriteTimeUtc(file)));
                    SaveIndexState(baseIndexPath, file);
                    onFileProcessed();
                }
                catch (OperationCanceledException)
                {
                    SaveIndexState(baseIndexPath, file); // Save state before cancelling
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file '{file}': {ex.Message}");
                }
            });
        }

        foreach (var file in largeFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"Processing large file: {file}");
            try
            {
                var fileSize = new FileInfo(file).Length;
                ProcessFile(file, rootFolderPath, fileSize, writer, false, cancellationToken);
                metadata[file] = File.GetLastWriteTimeUtc(file);
                writer.Commit(); // Commit after each large file
                SaveIndexState(baseIndexPath, file);
                onFileProcessed();
            }
            catch (OperationCanceledException)
            {
                SaveIndexState(baseIndexPath, file);
                writer.Commit();
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file '{file}': {ex.Message}");
            }
        }

        // Merge metadata updates
        foreach (var update in metadataUpdates)
        {
            metadata[update.FilePath] = update.LastModified;
        }

        // Final commit
        writer.Commit();
        Console.WriteLine($"Finished indexing folder: {folderToIndex}. Time: {stopwatch.ElapsedMilliseconds} ms");
        stopwatch.Stop();
    }

    private static void ProcessFile(string filePath, string rootFolderPath, long fileSize, IndexWriter writer,
        bool isSmallFile, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var pages = PdfHelper.ExtractTextFromLargePdf(filePath, fileSize);
        if (pages.Count == 0)
        {
            Console.WriteLine($"No text found in file: {filePath}");
            return;
        }

        Console.WriteLine($"Extracted {pages.Count} pages from file: {filePath} in {stopwatch.ElapsedMilliseconds} ms");

        var pageBatch = new List<KeyValuePair<int, string>>();
        int batchCount = 0;
        int pagesProcessed = 0;

        foreach (var page in pages.OrderBy(p => p.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();

            pageBatch.Add(page);

            if (pageBatch.Count >= DocumentsPerBatch || page.Key == pages.Keys.Max())
            {
                batchCount++;
                var batchStopwatch = Stopwatch.StartNew();
                var docs = pageBatch.Select(p => new Document
                {
                    new StringField("FilePath", filePath, Field.Store.YES),
                    new StringField("RelativePath", Path.GetRelativePath(rootFolderPath, filePath), Field.Store.YES),
                    new Int32Field("PageNumber", p.Key, Field.Store.YES),
                    new TextField("Content", p.Value, Field.Store.YES)
                }).ToList();

                if (isSmallFile)
                {
                    lock (writer)
                    {
                        writer.AddDocuments(docs);
                    }
                }
                else
                {
                    writer.AddDocuments(docs);
                }

                pagesProcessed += docs.Count;
                pageBatch.Clear();

                Console.WriteLine($"Indexed batch {batchCount} for '{filePath}' (pages {page.Key - docs.Count + 1} to {page.Key}, {docs.Count} pages) in {batchStopwatch.ElapsedMilliseconds} ms");

                if (!isSmallFile && pagesProcessed >= CommitIntervalPages)
                {
                    writer.Commit();
                    Console.WriteLine($"Committed {pagesProcessed} pages for '{filePath}'");
                    pagesProcessed = 0;
                }
            }
        }

        stopwatch.Stop();
        Console.WriteLine($"Completed processing file: {filePath}. Total time: {stopwatch.ElapsedMilliseconds} ms");
    }

    private static Dictionary<string, DateTime> LoadMetadata(string folderPath)
    {
        var metadataFilePath = Path.Combine(FolderUtility.GetFolderForPath(folderPath), "indexedFiles.json");
        return File.Exists(metadataFilePath) ?
            JsonSerializer.Deserialize<Dictionary<string, DateTime>>(File.ReadAllText(metadataFilePath)) ?? new Dictionary<string, DateTime>() :
            new Dictionary<string, DateTime>();
    }

    private static void SaveMetadata(string folderPath, Dictionary<string, DateTime> metadata)
    {
        var metadataFilePath = Path.Combine(FolderUtility.GetFolderForPath(folderPath), "indexedFiles.json");
        File.WriteAllText(metadataFilePath, JsonSerializer.Serialize(metadata));
    }

    private static IndexState? LoadIndexState(string baseIndexPath)
    {
        var stateFilePath = Path.Combine(baseIndexPath, "indexState.json");
        return File.Exists(stateFilePath) ?
            JsonSerializer.Deserialize<IndexState>(File.ReadAllText(stateFilePath)) :
            null;
    }

    private static void SaveIndexState(string baseIndexPath, string lastIndexedFile)
    {
        var stateFilePath = Path.Combine(baseIndexPath, "indexState.json");
        var state = new IndexState { LastIndexedFile = lastIndexedFile };
        File.WriteAllText(stateFilePath, JsonSerializer.Serialize(state));
    }

    private static List<string> GetNewOrUpdatedFiles(List<string> files, Dictionary<string, DateTime> metadata)
    {
        return [.. files.Where(file =>
        {
            var lastModified = File.GetLastWriteTimeUtc(file);
            return !metadata.TryGetValue(file, out var indexedTime) || lastModified > indexedTime;
        })];
    }
}