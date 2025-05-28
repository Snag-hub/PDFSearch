using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Devices;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Writer;
using Serilog; // Added for Serilog logging

namespace FindInPDFs.Utilities;

public static class PdfHelper
{
    private const long SmallFileSizeThreshold = 10 * 1024 * 1024; // 10 MB, files < this use parallel batch processing
    private const int MaxDegreeOfParallelism = 4; // Limit parallel tasks
    private const int PagesPerBatch = 100; // Process 100 pages per batch for small files
    private const int PagesPerBatchLarge = 20; // Smaller batches for large files if no splitting
    private const int PagesPerChunk = 200; // Pages per chunk for splitting
    private const long MinTempDiskSpace = 1L * 1024 * 1024 * 1024; // 1 GB required for temp files
    private const int OpenTimeoutMs = 2000; // 2-second timeout for opening

    public static Dictionary<string, Dictionary<int, string>> ExtractTextFromMultipleDirectories(IEnumerable<string> directories)
    {
        Log.Information("Starting text extraction from multiple directories. Directory count: {DirectoryCount}", directories.Count());
        var extractedData = new ConcurrentDictionary<string, Dictionary<int, string>>();
        var skippedFiles = new List<string>();

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"Directory does not exist: {directory}");
                Log.Warning("Directory does not exist: {Directory}", directory);
                continue;
            }

            var pdfFiles = Directory.GetFiles(directory, "*.pdf", SearchOption.AllDirectories)
                .Select(file => (Path: file, Size: new FileInfo(file).Length))
                .ToList();
            Log.Information("Found {FileCount} PDF files in directory: {Directory}", pdfFiles.Count, directory);

            var smallFiles = pdfFiles
                .Where(f => f.Size < SmallFileSizeThreshold)
                .ToList();
            var largeFiles = pdfFiles
                .Where(f => f.Size >= SmallFileSizeThreshold)
                .ToList();
            Log.Information("Categorized files in directory {Directory}: {SmallFilesCount} small files (< 10MB), {LargeFilesCount} large files.",
                directory, smallFiles.Count, largeFiles.Count);

            if (smallFiles.Count != 0)
            {
                Console.WriteLine($"Processing {smallFiles.Count} small files (< 10MB) in parallel...");
                Log.Information("Processing {SmallFilesCount} small files (< 10MB) in parallel for directory: {Directory}", smallFiles.Count, directory);
                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism };
                Parallel.ForEach(smallFiles, parallelOptions, file =>
                {
                    try
                    {
                        Console.WriteLine($"Processing small file: {file.Path}");
                        Log.Information("Processing small file: {FilePath}", file.Path);
                        var textByPage = ExtractTextFromPdf(file.Path, file.Size);

                        if (textByPage.Count != 0)
                        {
                            extractedData[file.Path] = textByPage;
                            Log.Information("Extracted {PageCount} pages from small file: {FilePath}", textByPage.Count, file.Path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file '{file.Path}': {ex.Message}");
                        Log.Error(ex, "Error processing small file '{FilePath}': {Message}", file.Path, ex.Message);
                    }
                });
            }

            if (largeFiles.Count != 0)
            {
                Console.WriteLine($"Processing {largeFiles.Count} large files (≥ 10MB) sequentially...");
                Log.Information("Processing {LargeFilesCount} large files (≥ 10MB) sequentially for directory: {Directory}", largeFiles.Count, directory);
                foreach (var file in largeFiles)
                {
                    try
                    {
                        Console.WriteLine($"Processing large file: {file.Path}");
                        Log.Information("Processing large file: {FilePath}", file.Path);
                        var textByPage = ExtractTextFromLargePdf(file.Path, file.Size);

                        if (textByPage.Count != 0)
                        {
                            extractedData[file.Path] = textByPage;
                            Log.Information("Extracted {PageCount} pages from large file: {FilePath}", textByPage.Count, file.Path);
                        }
                        else
                        {
                            Console.WriteLine($"No text extracted from '{file.Path}'. File may have been skipped due to timeout.");
                            Log.Warning("No text extracted from '{FilePath}'. File may have been skipped due to timeout.", file.Path);
                            skippedFiles.Add(file.Path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file '{file.Path}': {ex.Message}");
                        Log.Error(ex, "Error processing large file '{FilePath}': {Message}", file.Path, ex.Message);
                        skippedFiles.Add(file.Path);
                    }
                }
            }
        }

        if (skippedFiles.Count != 0)
        {
            Console.WriteLine("The following files were skipped due to opening timeout or errors and require external splitting (e.g., with pdftk):");
            Log.Warning("The following {SkippedCount} files were skipped due to opening timeout or errors and require external splitting (e.g., with pdftk):", skippedFiles.Count);
            foreach (var file in skippedFiles)
            {
                Console.WriteLine($"- {file}");
                Log.Warning("- {FilePath}", file);
            }
            Console.WriteLine("Example pdftk command: pdftk input.pdf cat 1-200 output chunk_01.pdf");
            Log.Information("Example pdftk command for splitting: pdftk input.pdf cat 1-200 output chunk_01.pdf");
        }

        Log.Information("Completed text extraction from directories. Total files processed: {FileCount}", extractedData.Count);
        return extractedData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static Dictionary<int, string> ExtractTextFromLargePdf(string pdfFilePath, long fileSize)
    {
        var stopwatch = Stopwatch.StartNew();
        Log.Information("Starting text extraction from large PDF: {FilePath}, Size: {FileSize} bytes", pdfFilePath, fileSize);
        try
        {
            // Try opening with a 2-second timeout
            PdfDocument pdfDoc = null;
            var cts = new CancellationTokenSource();
            var openTask = Task.Run(() =>
            {
                pdfDoc = PdfDocument.Open(pdfFilePath, new ParsingOptions { UseLenientParsing = true });
            }, cts.Token);

            if (openTask.Wait(OpenTimeoutMs))
            {
                // Opening succeeded within 2 seconds
                Console.WriteLine($"Opened '{pdfFilePath}' in {stopwatch.ElapsedMilliseconds} ms");
                Log.Information("Opened '{FilePath}' in {ElapsedMs} ms", pdfFilePath, stopwatch.ElapsedMilliseconds);
                using (pdfDoc)
                {
                    int totalPages = pdfDoc.NumberOfPages;
                    Console.WriteLine($"Estimated {totalPages} pages for '{pdfFilePath}'");
                    Log.Information("Estimated {TotalPages} pages for '{FilePath}'", totalPages, pdfFilePath);

                    if (totalPages > PagesPerChunk && fileSize > 100 * 1024 * 1024) // Split if > 200 pages and > 100 MB
                    {
                        Console.WriteLine($"Large PDF '{pdfFilePath}' exceeds {PagesPerChunk} pages. Splitting into chunks...");
                        Log.Information("Large PDF '{FilePath}' exceeds {PagesPerChunk} pages. Splitting into chunks...", pdfFilePath, PagesPerChunk);

                        // Check temp directory space
                        var tempPath = Path.GetTempPath();
                        var driveInfo = new DriveInfo(Path.GetPathRoot(tempPath));
                        if (driveInfo.AvailableFreeSpace < MinTempDiskSpace)
                        {
                            Console.WriteLine($"Insufficient disk space in temp directory ({tempPath}): {driveInfo.AvailableFreeSpace / (1024 * 1024)} MB available, {MinTempDiskSpace / (1024 * 1024)} MB required. Processing without splitting.");
                            Log.Warning("Insufficient disk space in temp directory ({TempPath}): {AvailableSpace} MB available, {RequiredSpace} MB required. Processing without splitting.",
                                tempPath, driveInfo.AvailableFreeSpace / (1024 * 1024), MinTempDiskSpace / (1024 * 1024));
                            return ExtractTextFromPdf(pdfFilePath, fileSize, PagesPerBatchLarge);
                        }
                        Log.Information("Temp directory space check passed: {AvailableSpace} MB available in {TempPath}", driveInfo.AvailableFreeSpace / (1024 * 1024), tempPath);

                        var chunkFiles = SplitPdf(pdfDoc, pdfFilePath, PagesPerChunk);
                        if (chunkFiles.Count == 0)
                        {
                            Console.WriteLine($"Splitting failed for '{pdfFilePath}'. Processing without splitting.");
                            Log.Warning("Splitting failed for '{FilePath}'. Processing without splitting.", pdfFilePath);
                            return ExtractTextFromPdf(pdfFilePath, fileSize, PagesPerBatchLarge);
                        }

                        var result = new ConcurrentDictionary<int, string>();
                        int globalPageOffset = 0;

                        foreach (var chunkFile in chunkFiles)
                        {
                            try
                            {
                                var chunkPages = ExtractTextFromPdf(chunkFile, new FileInfo(chunkFile).Length);
                                foreach (var page in chunkPages)
                                {
                                    result[page.Key + globalPageOffset] = page.Value;
                                }
                                globalPageOffset += chunkPages.Count;
                                Log.Information("Processed chunk '{ChunkFile}' with {PageCount} pages. Global page offset: {GlobalPageOffset}", chunkFile, chunkPages.Count, globalPageOffset);

                                // Force garbage collection
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                Log.Information("Forced garbage collection after processing chunk: {ChunkFile}", chunkFile);
                            }
                            finally
                            {
                                // Clean up chunk file
                                if (File.Exists(chunkFile))
                                {
                                    try
                                    {
                                        File.Delete(chunkFile);
                                        Log.Information("Deleted chunk file: {ChunkFile}", chunkFile);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Failed to delete chunk '{chunkFile}': {ex.Message}");
                                        Log.Error(ex, "Failed to delete chunk '{ChunkFile}': {Message}", chunkFile, ex.Message);
                                    }
                                }
                            }
                        }

                        Console.WriteLine($"Completed processing chunks for '{pdfFilePath}'. Total pages: {result.Count}, Time: {stopwatch.ElapsedMilliseconds} ms");
                        Log.Information("Completed processing chunks for '{FilePath}'. Total pages: {PageCount}, Time: {ElapsedMs} ms", pdfFilePath, result.Count, stopwatch.ElapsedMilliseconds);
                        return result.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    }
                }
            }
            else
            {
                // Opening timed out after 2 seconds
                cts.Cancel();
                Console.WriteLine($"Opening '{pdfFilePath}' exceeded {OpenTimeoutMs} ms. Skipping processing. Please pre-split into 200-page chunks using pdftk.");
                Log.Warning("Opening '{FilePath}' exceeded {TimeoutMs} ms. Skipping processing. Please pre-split into 200-page chunks using pdftk.", pdfFilePath, OpenTimeoutMs);
                return []; // Return empty to skip
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to process '{pdfFilePath}' for opening or splitting: {ex.Message}. Skipping processing.");
            Log.Error(ex, "Failed to process '{FilePath}' for opening or splitting: {Message}. Skipping processing.", pdfFilePath, ex.Message);
            return []; // Return empty to skip
        }

        // Fallback (only if explicitly needed, currently skipped)
        Log.Information("Falling back to ExtractTextFromPdf for '{FilePath}'", pdfFilePath);
        return ExtractTextFromPdf(pdfFilePath, fileSize, PagesPerBatchLarge);
    }

    private static Dictionary<int, string> ExtractTextFromPdf(string pdfFilePath, long fileSize = -1, int pagesPerBatch = PagesPerBatch)
    {
        var result = new ConcurrentDictionary<int, string>();
        var stopwatch = Stopwatch.StartNew();
        Log.Information("Starting text extraction from PDF: {FilePath}, FileSize: {FileSize} bytes, PagesPerBatch: {PagesPerBatch}", pdfFilePath, fileSize, pagesPerBatch);

        try
        {
            // Open PDF with lenient parsing
            var openStopwatch = Stopwatch.StartNew();
            using var pdfDoc = PdfDocument.Open(pdfFilePath, new ParsingOptions
            {
                UseLenientParsing = true // Allow lenient parsing
            });
            Console.WriteLine($"Opened '{pdfFilePath}' in {openStopwatch.ElapsedMilliseconds} ms");
            Log.Information("Opened '{FilePath}' in {ElapsedMs} ms", pdfFilePath, openStopwatch.ElapsedMilliseconds);

            // Process pages in batches
            bool useParallel = fileSize >= 0 && fileSize < SmallFileSizeThreshold && IsMemorySafe();
            Log.Information("Using parallel processing: {UseParallel}, FileSize: {FileSize}", useParallel, fileSize);
            int pageNum = 1;

            while (true)
            {
                // Collect up to pagesPerBatch pages
                var batchPages = new List<(int PageNum, Page Page)>();
                int batchStart = pageNum;
                int batchEnd = batchStart + pagesPerBatch - 1;

                var batchStopwatch = Stopwatch.StartNew();
                try
                {
                    for (; pageNum <= batchEnd && pageNum <= int.MaxValue; pageNum++)
                    {
                        try
                        {
                            var page = pdfDoc.GetPage(pageNum);
                            batchPages.Add((pageNum, page));
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // End of pages reached
                            Log.Information("Reached end of pages at page {PageNum} for '{FilePath}'", pageNum, pdfFilePath);
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to access page {pageNum} of '{pdfFilePath}': {ex.Message}");
                            Log.Error(ex, "Failed to access page {PageNum} of '{FilePath}': {Message}", pageNum, pdfFilePath, ex.Message);
                        }
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    // End of pages reached
                    Log.Information("Reached end of pages at batch for '{FilePath}'", pdfFilePath);
                    break;
                }

                if (batchPages.Count == 0)
                {
                    Log.Information("No more pages to process in batch for '{FilePath}'", pdfFilePath);
                    break; // No more pages
                }

                Console.WriteLine($"Processing batch: pages {batchStart} to {pageNum - 1} of '{pdfFilePath}' ({batchPages.Count} pages)");
                Log.Information("Processing batch: pages {BatchStart} to {BatchEnd} of '{FilePath}' ({PageCount} pages)", batchStart, pageNum - 1, pdfFilePath, batchPages.Count);

                // Extract text from batch
                if (useParallel)
                {
                    Console.WriteLine($"Using parallel batch processing for small file: {pdfFilePath}");
                    Log.Information("Using parallel batch processing for small file: {FilePath}", pdfFilePath);
                    var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism };
                    Parallel.ForEach(batchPages, parallelOptions, pageInfo =>
                    {
                        try
                        {
                            result[pageInfo.PageNum] = pageInfo.Page.Text;
                            Log.Information("Extracted text from page {PageNum} of '{FilePath}'", pageInfo.PageNum, pdfFilePath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to extract text from page {pageInfo.PageNum} of '{pdfFilePath}': {ex.Message}");
                            Log.Error(ex, "Failed to extract text from page {PageNum} of '{FilePath}': {Message}", pageInfo.PageNum, pdfFilePath, ex.Message);
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"Using sequential batch processing for large file: {pdfFilePath}");
                    Log.Information("Using sequential batch processing for large file: {FilePath}", pdfFilePath);
                    foreach (var pageInfo in batchPages)
                    {
                        try
                        {
                            result[pageInfo.PageNum] = pageInfo.Page.Text;
                            Log.Information("Extracted text from page {PageNum} of '{FilePath}'", pageInfo.PageNum, pdfFilePath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to extract text from page {pageInfo.PageNum} of '{pdfFilePath}': {ex.Message}");
                            Log.Error(ex, "Failed to extract text from page {PageNum} of '{FilePath}': {Message}", pageInfo.PageNum, pdfFilePath, ex.Message);
                        }
                    }
                }

                Console.WriteLine($"Processed batch {batchStart} to {pageNum - 1} in {batchStopwatch.ElapsedMilliseconds} ms");
                Log.Information("Processed batch {BatchStart} to {BatchEnd} in {ElapsedMs} ms", batchStart, pageNum - 1, batchStopwatch.ElapsedMilliseconds);

                // Memory check for large files
                if (!useParallel && !IsMemorySafe())
                {
                    Console.WriteLine($"Memory usage high for '{pdfFilePath}'. Forcing garbage collection...");
                    Log.Warning("Memory usage high for '{FilePath}'. Forcing garbage collection.", pdfFilePath);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Log.Information("Forced garbage collection due to high memory usage for '{FilePath}'", pdfFilePath);
                }
            }

            Console.WriteLine($"Completed extraction for '{pdfFilePath}'. Total pages extracted: {result.Count}, Time: {stopwatch.ElapsedMilliseconds} ms");
            Log.Information("Completed extraction for '{FilePath}'. Total pages extracted: {PageCount}, Time: {ElapsedMs} ms", pdfFilePath, result.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to process '{pdfFilePath}': {ex.Message}");
            Log.Error(ex, "Failed to process '{FilePath}': {Message}", pdfFilePath, ex.Message);
        }

        stopwatch.Stop();
        return result.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static List<string> SplitPdf(PdfDocument pdfDoc, string pdfFilePath, int pagesPerChunk)
    {
        var chunkFiles = new List<string>();
        Log.Information("Starting PDF splitting for '{FilePath}'. Pages per chunk: {PagesPerChunk}", pdfFilePath, pagesPerChunk);
        try
        {
            int totalPages = pdfDoc.NumberOfPages;
            int chunkCount = (int)Math.Ceiling((double)totalPages / pagesPerChunk);
            Log.Information("Total pages: {TotalPages}, Number of chunks: {ChunkCount}", totalPages, chunkCount);

            for (int chunk = 0; chunk < chunkCount; chunk++)
            {
                int startPage = chunk * pagesPerChunk + 1;
                int endPage = Math.Min(startPage + pagesPerChunk - 1, totalPages);
                var chunkFile = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(pdfFilePath)}_chunk_{chunk + 1}.pdf");

                using var builder = new PdfDocumentBuilder();
                for (int pageNum = startPage; pageNum <= endPage; pageNum++)
                {
                    builder.AddPage(pdfDoc, pageNum);
                }

                File.WriteAllBytes(chunkFile, builder.Build());
                chunkFiles.Add(chunkFile);
                Console.WriteLine($"Created chunk '{chunkFile}' (pages {startPage} to {endPage})");
                Log.Information("Created chunk '{ChunkFile}' (pages {StartPage} to {EndPage})", chunkFile, startPage, endPage);

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Log.Information("Forced garbage collection after creating chunk: {ChunkFile}", chunkFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to split '{pdfFilePath}': {ex.Message}");
            Log.Error(ex, "Failed to split '{FilePath}': {Message}", pdfFilePath, ex.Message);
            foreach (var chunkFile in chunkFiles)
            {
                if (File.Exists(chunkFile))
                {
                    try
                    {
                        File.Delete(chunkFile);
                        Log.Information("Deleted chunk file during cleanup: {ChunkFile}", chunkFile);
                    }
                    catch (Exception deleteEx)
                    {
                        Console.WriteLine($"Failed to delete chunk '{chunkFile}': {deleteEx.Message}");
                        Log.Error(deleteEx, "Failed to delete chunk '{ChunkFile}' during cleanup: {Message}", chunkFile, deleteEx.Message);
                    }
                }
            }
            chunkFiles.Clear();
        }

        Log.Information("Completed PDF splitting for '{FilePath}'. Created {ChunkCount} chunks.", pdfFilePath, chunkFiles.Count);
        return chunkFiles;
    }

    public static bool IsMemorySafe()
    {
        var computerInfo = new ComputerInfo();
        var process = Process.GetCurrentProcess();
        var ramUsagePercent = (double)process.WorkingSet64 / computerInfo.TotalPhysicalMemory;
        Log.Information("Memory usage check: RAM Usage Percent: {RamUsagePercent:P2}, WorkingSet: {WorkingSetMB} MB, TotalPhysicalMemory: {TotalPhysicalMemoryMB} MB",
            ramUsagePercent, process.WorkingSet64 / (1024 * 1024), computerInfo.TotalPhysicalMemory / (1024 * 1024));
        return ramUsagePercent < 0.85; // Less conservative threshold
    }
}