using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Devices;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Writer;

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
        var extractedData = new ConcurrentDictionary<string, Dictionary<int, string>>();
        var skippedFiles = new List<string>();

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"Directory does not exist: {directory}");
                continue;
            }

            var pdfFiles = Directory.GetFiles(directory, "*.pdf", SearchOption.AllDirectories)
                .Select(file => (Path: file, Size: new FileInfo(file).Length))
                .ToList();

            var smallFiles = pdfFiles
                .Where(f => f.Size < SmallFileSizeThreshold)
                .ToList();
            var largeFiles = pdfFiles
                .Where(f => f.Size >= SmallFileSizeThreshold)
                .ToList();

            if (smallFiles.Any())
            {
                Console.WriteLine($"Processing {smallFiles.Count} small files (< 10MB) in parallel...");
                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism };
                Parallel.ForEach(smallFiles, parallelOptions, file =>
                {
                    try
                    {
                        Console.WriteLine($"Processing small file: {file.Path}");
                        var textByPage = ExtractTextFromPdf(file.Path, file.Size);

                        if (textByPage.Any())
                        {
                            extractedData[file.Path] = textByPage;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file '{file.Path}': {ex.Message}");
                    }
                });
            }

            if (largeFiles.Any())
            {
                Console.WriteLine($"Processing {largeFiles.Count} large files (≥ 10MB) sequentially...");
                foreach (var file in largeFiles)
                {
                    try
                    {
                        Console.WriteLine($"Processing large file: {file.Path}");
                        var textByPage = ExtractTextFromLargePdf(file.Path, file.Size);

                        if (textByPage.Any())
                        {
                            extractedData[file.Path] = textByPage;
                        }
                        else
                        {
                            Console.WriteLine($"No text extracted from '{file.Path}'. File may have been skipped due to timeout.");
                            skippedFiles.Add(file.Path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file '{file.Path}': {ex.Message}");
                        skippedFiles.Add(file.Path);
                    }
                }
            }
        }

        if (skippedFiles.Any())
        {
            Console.WriteLine("The following files were skipped due to opening timeout or errors and require external splitting (e.g., with pdftk):");
            foreach (var file in skippedFiles)
            {
                Console.WriteLine($"- {file}");
            }
            Console.WriteLine("Example pdftk command: pdftk input.pdf cat 1-200 output chunk_01.pdf");
        }

        return extractedData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static Dictionary<int, string> ExtractTextFromLargePdf(string pdfFilePath, long fileSize)
    {
        var stopwatch = Stopwatch.StartNew();
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
                using (pdfDoc)
                {
                    int totalPages = pdfDoc.NumberOfPages;
                    Console.WriteLine($"Estimated {totalPages} pages for '{pdfFilePath}'");

                    if (totalPages > PagesPerChunk && fileSize > 100 * 1024 * 1024) // Split if > 200 pages and > 100 MB
                    {
                        Console.WriteLine($"Large PDF '{pdfFilePath}' exceeds {PagesPerChunk} pages. Splitting into chunks...");

                        // Check temp directory space
                        var tempPath = Path.GetTempPath();
                        var driveInfo = new DriveInfo(Path.GetPathRoot(tempPath));
                        if (driveInfo.AvailableFreeSpace < MinTempDiskSpace)
                        {
                            Console.WriteLine($"Insufficient disk space in temp directory ({tempPath}): {driveInfo.AvailableFreeSpace / (1024 * 1024)} MB available, {MinTempDiskSpace / (1024 * 1024)} MB required. Processing without splitting.");
                            return ExtractTextFromPdf(pdfFilePath, fileSize, PagesPerBatchLarge);
                        }

                        var chunkFiles = SplitPdf(pdfDoc, pdfFilePath, PagesPerChunk);
                        if (!chunkFiles.Any())
                        {
                            Console.WriteLine($"Splitting failed for '{pdfFilePath}'. Processing without splitting.");
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

                                // Force garbage collection
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                            finally
                            {
                                // Clean up chunk file
                                if (File.Exists(chunkFile))
                                {
                                    try
                                    {
                                        File.Delete(chunkFile);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Failed to delete chunk '{chunkFile}': {ex.Message}");
                                    }
                                }
                            }
                        }

                        Console.WriteLine($"Completed processing chunks for '{pdfFilePath}'. Total pages: {result.Count}, Time: {stopwatch.ElapsedMilliseconds} ms");
                        return result.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    }
                }
            }
            else
            {
                // Opening timed out after 2 seconds
                cts.Cancel();
                Console.WriteLine($"Opening '{pdfFilePath}' exceeded {OpenTimeoutMs} ms. Skipping processing. Please pre-split into 200-page chunks using pdftk.");
                return new Dictionary<int, string>(); // Return empty to skip
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to process '{pdfFilePath}' for opening or splitting: {ex.Message}. Skipping processing.");
            return new Dictionary<int, string>(); // Return empty to skip
        }

        // Fallback (only if explicitly needed, currently skipped)
        return ExtractTextFromPdf(pdfFilePath, fileSize, PagesPerBatchLarge);
    }

    private static Dictionary<int, string> ExtractTextFromPdf(string pdfFilePath, long fileSize = -1, int pagesPerBatch = PagesPerBatch)
    {
        var result = new ConcurrentDictionary<int, string>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Open PDF with lenient parsing
            var openStopwatch = Stopwatch.StartNew();
            using var pdfDoc = PdfDocument.Open(pdfFilePath, new ParsingOptions
            {
                UseLenientParsing = true // Allow lenient parsing
            });
            Console.WriteLine($"Opened '{pdfFilePath}' in {openStopwatch.ElapsedMilliseconds} ms");

            // Process pages in batches
            bool useParallel = fileSize >= 0 && fileSize < SmallFileSizeThreshold && IsMemorySafe();
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
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to access page {pageNum} of '{pdfFilePath}': {ex.Message}");
                        }
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    // End of pages reached
                    break;
                }

                if (!batchPages.Any())
                {
                    break; // No more pages
                }

                Console.WriteLine($"Processing batch: pages {batchStart} to {pageNum - 1} of '{pdfFilePath}' ({batchPages.Count} pages)");

                // Extract text from batch
                if (useParallel)
                {
                    Console.WriteLine($"Using parallel batch processing for small file: {pdfFilePath}");
                    var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism };
                    Parallel.ForEach(batchPages, parallelOptions, pageInfo =>
                    {
                        try
                        {
                            result[pageInfo.PageNum] = pageInfo.Page.Text;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to extract text from page {pageInfo.PageNum} of '{pdfFilePath}': {ex.Message}");
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"Using sequential batch processing for large file: {pdfFilePath}");
                    foreach (var pageInfo in batchPages)
                    {
                        try
                        {
                            result[pageInfo.PageNum] = pageInfo.Page.Text;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to extract text from page {pageInfo.PageNum} of '{pdfFilePath}': {ex.Message}");
                        }
                    }
                }

                Console.WriteLine($"Processed batch {batchStart} to {pageNum - 1} in {batchStopwatch.ElapsedMilliseconds} ms");

                // Memory check for large files
                if (!useParallel && !IsMemorySafe())
                {
                    Console.WriteLine($"Memory usage high for '{pdfFilePath}'. Forcing garbage collection...");
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            Console.WriteLine($"Completed extraction for '{pdfFilePath}'. Total pages extracted: {result.Count}, Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to process '{pdfFilePath}': {ex.Message}");
        }

        stopwatch.Stop();
        return result.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static List<string> SplitPdf(PdfDocument pdfDoc, string pdfFilePath, int pagesPerChunk)
    {
        var chunkFiles = new List<string>();
        try
        {
            int totalPages = pdfDoc.NumberOfPages;
            int chunkCount = (int)Math.Ceiling((double)totalPages / pagesPerChunk);

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

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to split '{pdfFilePath}': {ex.Message}");
            foreach (var chunkFile in chunkFiles)
            {
                if (File.Exists(chunkFile))
                {
                    try
                    {
                        File.Delete(chunkFile);
                    }
                    catch (Exception deleteEx)
                    {
                        Console.WriteLine($"Failed to delete chunk '{chunkFile}': {deleteEx.Message}");
                    }
                }
            }
            chunkFiles.Clear();
        }

        return chunkFiles;
    }

    public static bool IsMemorySafe()
    {
        var computerInfo = new ComputerInfo();
        var process = Process.GetCurrentProcess();
        var ramUsagePercent = (double)process.WorkingSet64 / computerInfo.TotalPhysicalMemory;
        return ramUsagePercent < 0.85; // Less conservative threshold
    }
}