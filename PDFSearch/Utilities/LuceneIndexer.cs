using System;
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
using Serilog; // Added for Serilog logging
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
        public string LastIndexedFile { get; set; }
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
            Log.Error("Directory does not exist: {FolderPath}", folderPath);
            throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");
        }

        Log.Information("Starting indexing for folder: {FolderPath}", folderPath);
        Console.WriteLine($"Starting indexing for folder: {folderPath}");
        var stopwatch = Stopwatch.StartNew();

        var foldersToIndex = FolderIndexerHelper.GetFoldersToIndex(folderPath);
        var baseIndexPath = FolderUtility.GetFolderForPath(folderPath);
        Directory.CreateDirectory(baseIndexPath);
        Log.Information("Created base index directory: {BaseIndexPath}", baseIndexPath);

        var metadata = LoadMetadata(folderPath);
        Log.Information("Loaded metadata with {Count} entries.", metadata.Count);
        var state = LoadIndexState(baseIndexPath);
        Log.Information("Loaded index state. LastIndexedFile: {LastIndexedFile}", state?.LastIndexedFile ?? "None");

        // Count all PDF files for totalFiles
        var allFiles = foldersToIndex
            .SelectMany(folder => Directory.GetFiles(folder, "*.pdf", SearchOption.AllDirectories))
            .Select(f => (Path: f, Size: new FileInfo(f).Length))
            .OrderBy(f => f.Size)
            .Select(f => f.Path)
            .ToList();
        int totalFiles = allFiles.Count;
        Log.Information("Found {TotalFiles} PDF files to index.", totalFiles);

        // Count already indexed files
        int processedFiles = metadata.Count; // Files in indexedFiles.json
        if (state?.LastIndexedFile != null)
        {
            // Adjust processedFiles to include files up to LastIndexedFile
            processedFiles = allFiles.TakeWhile(f => f != state.LastIndexedFile).Count() + 1;
            Log.Information("Adjusted processed files to {ProcessedFiles} based on LastIndexedFile: {LastIndexedFile}", processedFiles, state.LastIndexedFile);
        }

        bool skipFiles = state?.LastIndexedFile != null;
        Log.Information("SkipFiles set to {SkipFiles} based on index state.", skipFiles);
        foreach (var folder in foldersToIndex)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var indexPath = Path.Combine(baseIndexPath, $"index_folder_{Path.GetFileName(folder)}");
            Directory.CreateDirectory(indexPath);
            Log.Information("Created index directory for folder: {IndexPath}", indexPath);

            Console.WriteLine($"Indexing folder: {folder}");
            Log.Information("Indexing folder: {Folder}", folder);
            IndexFolder(folder, indexPath, folderPath, metadata, baseIndexPath, state, skipFiles, () =>
            {
                Interlocked.Increment(ref processedFiles);
                Log.Information("Processed file {ProcessedFiles}/{TotalFiles}", processedFiles, totalFiles);
                progressCallback?.Invoke(processedFiles, totalFiles);
            }, cancellationToken);

            // After first folder, stop skipping files
            skipFiles = false;
            Log.Information("Finished indexing folder {Folder}, setting SkipFiles to false.", folder);
        }

        SaveMetadata(folderPath, metadata);
        Log.Information("Saved metadata for folder: {FolderPath}", folderPath);
        Console.WriteLine($"All folders indexed successfully. Total time: {stopwatch.ElapsedMilliseconds} ms");
        Log.Information("All folders indexed successfully. Total time: {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
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
        Log.Information("Found {FileCount} PDF files in folder: {FolderToIndex}", files.Count, folderToIndex);

        var newOrUpdatedFiles = GetNewOrUpdatedFiles(files, metadata);
        Log.Information("Identified {NewOrUpdatedCount} new or updated files in folder: {FolderToIndex}", newOrUpdatedFiles.Count, folderToIndex);
        if (newOrUpdatedFiles.Count == 0)
        {
            Console.WriteLine($"No new or updated files in folder: {folderToIndex}");
            Log.Information("No new or updated files in folder: {FolderToIndex}", folderToIndex);
            return;
        }

        // Skip files up to LastIndexedFile
        if (skipFiles && state?.LastIndexedFile != null)
        {
            newOrUpdatedFiles = [.. newOrUpdatedFiles
                .SkipWhile(f => f != state.LastIndexedFile)
                .Skip(1)];
            Log.Information("Skipped files up to LastIndexedFile: {LastIndexedFile}. Remaining files: {RemainingCount}", state.LastIndexedFile, newOrUpdatedFiles.Count);
        }

        if (newOrUpdatedFiles.Count == 0)
        {
            Console.WriteLine($"All files in folder {folderToIndex} already indexed up to state.");
            Log.Information("All files in folder {FolderToIndex} already indexed up to state.", folderToIndex);
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
        Log.Information("Initialized Lucene IndexWriter for index path: {IndexPath}", indexPath);

        using var writer = new IndexWriter(dir, config);
        var stopwatch = Stopwatch.StartNew();
        var metadataUpdates = new ConcurrentBag<(string FilePath, DateTime LastModified)>();

        var smallFiles = newOrUpdatedFiles
            .Where(f => new FileInfo(f).Length < SmallFileSizeThreshold)
            .ToList();
        var largeFiles = newOrUpdatedFiles
            .Where(f => new FileInfo(f).Length >= SmallFileSizeThreshold)
            .ToList();
        Log.Information("Categorized files: {SmallFilesCount} small files (< 10MB), {LargeFilesCount} large files.", smallFiles.Count, largeFiles.Count);

        if (smallFiles.Count != 0)
        {
            Console.WriteLine($"Processing {smallFiles.Count} small files (< 10MB) in parallel...");
            Log.Information("Processing {SmallFilesCount} small files (< 10MB) in parallel.", smallFiles.Count);
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
                    Log.Information("Processing small file: {FilePath}, Size: {FileSize} bytes", file, fileSize);
                    ProcessFile(file, rootFolderPath, fileSize, writer, true, cancellationToken);
                    metadataUpdates.Add((file, File.GetLastWriteTimeUtc(file)));
                    SaveIndexState(baseIndexPath, file);
                    Log.Information("Saved index state for small file: {FilePath}", file);
                    onFileProcessed();
                }
                catch (OperationCanceledException)
                {
                    SaveIndexState(baseIndexPath, file); // Save state before cancelling
                    Log.Information("Indexing cancelled during small file processing: {FilePath}", file);
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file '{file}': {ex.Message}");
                    Log.Error(ex, "Error processing small file '{FilePath}': {Message}", file, ex.Message);
                }
            });
        }

        foreach (var file in largeFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"Processing large file: {file}");
            Log.Information("Processing large file: {FilePath}", file);
            try
            {
                var fileSize = new FileInfo(file).Length;
                ProcessFile(file, rootFolderPath, fileSize, writer, false, cancellationToken);
                metadata[file] = File.GetLastWriteTimeUtc(file);
                writer.Commit(); // Commit after each large file
                Log.Information("Committed index after processing large file: {FilePath}", file);
                SaveIndexState(baseIndexPath, file);
                Log.Information("Saved index state for large file: {FilePath}", file);
                onFileProcessed();
            }
            catch (OperationCanceledException)
            {
                SaveIndexState(baseIndexPath, file);
                writer.Commit();
                Log.Information("Indexing cancelled during large file processing: {FilePath}", file);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file '{file}': {ex.Message}");
                Log.Error(ex, "Error processing large file '{FilePath}': {Message}", file, ex.Message);
            }
        }

        // Merge metadata updates
        foreach (var update in metadataUpdates)
        {
            metadata[update.FilePath] = update.LastModified;
        }
        Log.Information("Merged {UpdateCount} metadata updates.", metadataUpdates.Count);

        // Final commit
        writer.Commit();
        Log.Information("Final commit for folder: {FolderToIndex}", folderToIndex);
        Console.WriteLine($"Finished indexing folder: {folderToIndex}. Time: {stopwatch.ElapsedMilliseconds} ms");
        Log.Information("Finished indexing folder: {FolderToIndex}. Time: {ElapsedMs} ms", folderToIndex, stopwatch.ElapsedMilliseconds);
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
            Log.Information("No text found in file: {FilePath}", filePath);
            return;
        }

        Console.WriteLine($"Extracted {pages.Count} pages from file: {filePath} in {stopwatch.ElapsedMilliseconds} ms");
        Log.Information("Extracted {PageCount} pages from file: {FilePath} in {ElapsedMs} ms", pages.Count, filePath, stopwatch.ElapsedMilliseconds);

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
                    Log.Information("Added {DocCount} documents for small file batch {BatchCount} (pages {StartPage} to {EndPage})", docs.Count, batchCount, page.Key - docs.Count + 1, page.Key);
                }
                else
                {
                    writer.AddDocuments(docs);
                    Log.Information("Added {DocCount} documents for large file batch {BatchCount} (pages {StartPage} to {EndPage})", docs.Count, batchCount, page.Key - docs.Count + 1, page.Key);
                }

                pagesProcessed += docs.Count;
                pageBatch.Clear();

                Console.WriteLine($"Indexed batch {batchCount} for '{filePath}' (pages {page.Key - docs.Count + 1} to {page.Key}, {docs.Count} pages) in {batchStopwatch.ElapsedMilliseconds} ms");
                Log.Information("Indexed batch {BatchCount} for '{FilePath}' (pages {StartPage} to {EndPage}, {DocCount} pages) in {ElapsedMs} ms", batchCount, filePath, page.Key - docs.Count + 1, page.Key, docs.Count, batchStopwatch.ElapsedMilliseconds);

                if (!isSmallFile && pagesProcessed >= CommitIntervalPages)
                {
                    writer.Commit();
                    Console.WriteLine($"Committed {pagesProcessed} pages for '{filePath}'");
                    Log.Information("Committed {PagesProcessed} pages for '{FilePath}'", pagesProcessed, filePath);
                    pagesProcessed = 0;
                }
            }
        }

        stopwatch.Stop();
        Console.WriteLine($"Completed processing file: {filePath}. Total time: {stopwatch.ElapsedMilliseconds} ms");
        Log.Information("Completed processing file: {FilePath}. Total time: {ElapsedMs} ms", filePath, stopwatch.ElapsedMilliseconds);
    }

    private static Dictionary<string, DateTime> LoadMetadata(string folderPath)
    {
        var metadataFilePath = Path.Combine(FolderUtility.GetFolderForPath(folderPath), "indexedFiles.json");
        if (File.Exists(metadataFilePath))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(File.ReadAllText(metadataFilePath)) ?? [];
                Log.Information("Loaded metadata from {MetadataFilePath} with {Count} entries.", metadataFilePath, metadata.Count);
                return metadata;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load metadata from {MetadataFilePath}", metadataFilePath);
                return [];
            }
        }
        Log.Information("No metadata file found at {MetadataFilePath}, returning empty dictionary.", metadataFilePath);
        return [];
    }

    private static void SaveMetadata(string folderPath, Dictionary<string, DateTime> metadata)
    {
        var metadataFilePath = Path.Combine(FolderUtility.GetFolderForPath(folderPath), "indexedFiles.json");
        try
        {
            File.WriteAllText(metadataFilePath, JsonSerializer.Serialize(metadata));
            Log.Information("Saved metadata to {MetadataFilePath} with {Count} entries.", metadataFilePath, metadata.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save metadata to {MetadataFilePath}", metadataFilePath);
        }
    }

    private static IndexState? LoadIndexState(string baseIndexPath)
    {
        var stateFilePath = Path.Combine(baseIndexPath, "indexState.json");
        if (File.Exists(stateFilePath))
        {
            try
            {
                var state = JsonSerializer.Deserialize<IndexState>(File.ReadAllText(stateFilePath));
                Log.Information("Loaded index state from {StateFilePath}. LastIndexedFile: {LastIndexedFile}", stateFilePath, state?.LastIndexedFile ?? "None");
                return state;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load index state from {StateFilePath}", stateFilePath);
                return null;
            }
        }
        Log.Information("No index state file found at {StateFilePath}, returning null.", stateFilePath);
        return null;
    }

    private static void SaveIndexState(string baseIndexPath, string lastIndexedFile)
    {
        var stateFilePath = Path.Combine(baseIndexPath, "indexState.json");
        var state = new IndexState { LastIndexedFile = lastIndexedFile };
        try
        {
            File.WriteAllText(stateFilePath, JsonSerializer.Serialize(state));
            Log.Information("Saved index state to {StateFilePath}. LastIndexedFile: {LastIndexedFile}", stateFilePath, lastIndexedFile);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save index state to {StateFilePath}", stateFilePath);
        }
    }

    private static List<string> GetNewOrUpdatedFiles(List<string> files, Dictionary<string, DateTime> metadata)
    {
        var newOrUpdatedFiles = files.Where(file =>
        {
            var lastModified = File.GetLastWriteTimeUtc(file);
            return !metadata.TryGetValue(file, out var indexedTime) || lastModified > indexedTime;
        }).ToList();
        Log.Information("Identified {NewOrUpdatedCount} new or updated files from {TotalFiles} files.", newOrUpdatedFiles.Count, files.Count);
        return newOrUpdatedFiles;
    }
}