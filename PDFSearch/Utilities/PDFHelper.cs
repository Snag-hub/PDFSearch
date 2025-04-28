using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;
using Serilog;
using UglyToad.PdfPig;

namespace FindInPDFs.Utilities;

public static class PdfHelper
{
    public const int LargeFilePageThreshold = 400; // Pages
    private const long LargeFileSizeThreshold = 200 * 1024 * 1024; // 200 MB

    public static Dictionary<string, Dictionary<int, string>> ExtractTextFromMultipleDirectories(
        IEnumerable<string> directories)
    {
        var extractedData = new ConcurrentDictionary<string, Dictionary<int, string>>();
        var pdfFiles = new List<(string Path, long Size, int? PageCount)>();

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                Log.Error("Directory does not exist: {Directory}", directory);
                continue;
            }

            pdfFiles.AddRange(Directory.GetFiles(directory, "*.pdf", SearchOption.AllDirectories)
                .Select<string, (string Path, long Size, int? PageCount)>(file =>
                    (Path: file, Size: new FileInfo(file).Length, PageCount: null)));
        }

        // Classify files
        var largeFiles = pdfFiles.Where(file => file.Size > LargeFileSizeThreshold).ToList();
        var smallFiles = pdfFiles.Except(largeFiles).ToList();

        // Process small files in parallel
        Parallel.ForEach(smallFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 }, file =>
        {
            try
            {
                var textByPage = ExtractTextFromPdfWithBatching(file.Path).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (textByPage.Count > 0)
                {
                    extractedData[file.Path] = textByPage;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing small file '{FilePath}'", file.Path);
            }
        });

        // Process large files sequentially with page range parallelism
        foreach (var largeFile in largeFiles)
        {
            try
            {
                Log.Information("Processing large file: {FilePath} (Size: {Size} MB)", largeFile.Path,
                    largeFile.Size / (1024 * 1024));

                var textByPage = ExtractTextFromPdfWithBatching(largeFile.Path);
                if (textByPage.Count > 0)
                {
                    extractedData[largeFile.Path] = textByPage.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing large file '{FilePath}'", largeFile.Path);
            }
        }

        return extractedData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static Dictionary<int, string> ExtractTextFromPdfWithBatching(string pdfFilePath)
    {
        var result = new ConcurrentDictionary<int, string>();
        const int batchSize = 100; // Commit every 50 pages
        var currentBatch = new List<KeyValuePair<int, string>>();

        try
        {
            using var pdfDoc = PdfDocument.Open(pdfFilePath);
            var totalPages = pdfDoc.NumberOfPages;

            Log.Information("Extracting text from '{PdfFile}' with {PageCount} pages", pdfFilePath, totalPages);

            for (var pageNum = 1; pageNum <= totalPages; pageNum++)
            {
                try
                {
                    var page = pdfDoc.GetPage(pageNum);
                    var extractedText = page.Text;

                    currentBatch.Add(new KeyValuePair<int, string>(pageNum, extractedText));

                    // Commit the batch if it reaches the batch size
                    if (currentBatch.Count >= batchSize)
                    {
                        CommitBatch(currentBatch, result);
                        currentBatch.Clear(); // Clear the batch
                    }

                    Log.Debug("Extracted text from page {PageNumber} of '{PdfFile}'", pageNum, pdfFilePath);
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed to extract page {PageNumber} of '{PdfFile}': {Message}", pageNum, pdfFilePath, ex.Message);
                }
            }

            // Commit any remaining pages in the batch
            if (currentBatch.Count != 0)
            {
                CommitBatch(currentBatch, result);
            }

            Log.Information("Completed extraction for '{PdfFile}' in {ElapsedSeconds:F2} seconds",
                pdfFilePath, Stopwatch.StartNew().Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process '{PdfFile}'", pdfFilePath);
        }

        return result.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static void CommitBatch(List<KeyValuePair<int, string>> batch, ConcurrentDictionary<int, string> result)
    {
        lock (result)
        {
            foreach (var kvp in batch)
            {
                result.TryAdd(kvp.Key, kvp.Value);
            }
            Log.Information("Committed batch of {BatchSize} pages to memory.", batch.Count);
        }
    }

    public static bool IsMemorySafe()
    {
        var computerInfo = new ComputerInfo();
        var process = Process.GetCurrentProcess();
        var ramUsagePercent = (double)process.WorkingSet64 / computerInfo.TotalPhysicalMemory;
        return ramUsagePercent < 0.8; // Adjust if needed
    }
}