using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Serilog;
using Microsoft.VisualBasic.Devices;

namespace PDFSearch.Utilities
{
    public static class PdfHelper
    {
        public const int LargeFilePageThreshold = 1000; // Pages (for 3000-page files)
        public const long LargeFileSizeThreshold = 200 * 1024 * 1024; // 200 MB (for 500 MB–1.5 GB files)

        public static Dictionary<string, Dictionary<int, string>> ExtractTextFromMultipleDirectories(IEnumerable<string> directories)
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
                    .Select<string, (string Path, long Size, int? PageCount)>(file => (Path: file, Size: new FileInfo(file).Length, PageCount: null)));
            }

            // Classify files
            var largeFiles = new Queue<(string Path, long Size, int? PageCount)>();
            var smallFiles = new Queue<(string Path, long Size, int? PageCount)>();
            foreach (var file in pdfFiles)
            {
                if (file.Size > LargeFileSizeThreshold || GetPageCount(file.Path) > LargeFilePageThreshold)
                    largeFiles.Enqueue(file);
                else
                    smallFiles.Enqueue(file);
            }

            // Process large files sequentially
            while (largeFiles.Count > 0)
            {
                var largeFile = largeFiles.Dequeue();
                Log.Information("Processing large file: {FilePath} (Size: {Size} MB, Pages: {Pages})", largeFile.Path, largeFile.Size / (1024 * 1024), largeFile.PageCount ?? 0);
                try
                {
                    var textByPage = (ExtractTextFromPdfPages(largeFile.Path).Result)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    if (textByPage.Count != 0)
                    {
                        extractedData[largeFile.Path] = textByPage;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing large file '{FilePath}'", largeFile.Path);
                }

                // Process small files in parallel while large file is done
                if (smallFiles.Count > 0 && IsMemorySafe())
                {
                    Parallel.ForEach(smallFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 }, smallFile =>
                    {
                        try
                        {
                            var textByPage = ExtractTextFromPdfPages(smallFile.Path).Result
                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                            if (textByPage.Count != 0)
                            {
                                lock (new object()) // Local lock object
                                {
                                    extractedData[smallFile.Path] = textByPage;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error processing small file '{FilePath}'", smallFile.Path);
                        }
                    });
                    smallFiles.Clear(); // Clear processed small files
                }
            }

            // Process any remaining small files
            if (smallFiles.Count > 0)
            {
                Parallel.ForEach(smallFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 }, smallFile =>
                {
                    if (IsMemorySafe())
                    {
                        try
                        {
                            var textByPage = ExtractTextFromPdfPages(smallFile.Path).Result
                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                            if (textByPage.Count != 0)
                            {
                                lock (new object()) // Local lock object
                                {
                                    extractedData[smallFile.Path] = textByPage;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error processing small file '{FilePath}'", smallFile.Path);
                        }
                    }
                });
            }

            return extractedData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static int? GetPageCount(string pdfFilePath)
        {
            try
            {
                using var pdfDoc = PdfDocument.Open(pdfFilePath, new ParsingOptions { UseLenientParsing = true });
                return pdfDoc.NumberOfPages;
            }
            catch
            {
                return null; // Fallback to size-based classification if page count fails
            }
        }

        public static bool IsMemorySafe()
        {
            var computerInfo = new ComputerInfo();
            long totalMemory = (long)(computerInfo.TotalPhysicalMemory / (1024 * 1024)); // MB
            var process = System.Diagnostics.Process.GetCurrentProcess();
            double ramUsagePercent = (double)process.WorkingSet64 / computerInfo.TotalPhysicalMemory;
            return ramUsagePercent < 0.7; // Pause if >70% RAM used
        }

        public static async Task<IEnumerable<KeyValuePair<int, string>>> ExtractTextFromPdfPages(string pdfFilePath)
        {
            var result = new List<KeyValuePair<int, string>>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using (var pdfStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.Read))
                using (var pdfDoc = PdfDocument.Open(pdfStream, new ParsingOptions { UseLenientParsing = true }))
                {
                    int pageCount = pdfDoc.NumberOfPages;
                    Log.Information("Extracting text from '{PdfFile}' with {PageCount} pages", pdfFilePath, pageCount);

                    const int batchSize = 50;
                    var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30)); // 30 minutes for 3000-page files
                    var token = cts.Token;

                    for (int startPage = 1; startPage <= pageCount; startPage += batchSize)
                    {
                        var batchStopwatch = System.Diagnostics.Stopwatch.StartNew();
                        int endPage = Math.Min(startPage + batchSize - 1, pageCount);

                        for (int i = startPage; i <= endPage; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                var page = pdfDoc.GetPage(i);
                                string text = page.Text ?? "";
                                result.Add(new KeyValuePair<int, string>(i, text));
                            }
                            catch (Exception ex)
                            {
                                Log.Warning("Failed to extract page {PageNumber} from '{PdfFile}': {Message}", i, pdfFilePath, ex.Message);
                                result.Add(new KeyValuePair<int, string>(i, "")); // Empty text for failed pages
                            }
                        }

                        Log.Information("Extracted pages {StartPage}–{EndPage} from '{PdfFile}' in {ElapsedSeconds:F2}s", startPage, endPage, pdfFilePath, batchStopwatch.Elapsed.TotalSeconds);
                        await Task.Delay(1, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Extraction for '{PdfFile}' timed out after 30 minutes", pdfFilePath);
                return result; // Return partial results
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open '{PdfFile}'", pdfFilePath);
                return Enumerable.Empty<KeyValuePair<int, string>>();
            }
            finally
            {
                Log.Information("Completed extraction for '{PdfFile}' in {ElapsedSeconds:F2}s", pdfFilePath, stopwatch.Elapsed.TotalSeconds);
            }

            return result;
        }
    }
}
