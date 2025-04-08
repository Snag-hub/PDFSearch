using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Microsoft.VisualBasic.Devices;
using PDFSearch.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Directory = System.IO.Directory;

namespace PDFSearch.Utilities
{
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
                throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");

            // Get folders to index based on size constraint
            var foldersToIndex = FolderIndexerHelper.GetFoldersToIndex(folderPath);
            string baseIndexPath = FolderUtility.GetFolderForPath(folderPath);
            Directory.CreateDirectory(baseIndexPath);

            // Load metadata once for the entire operation
            var metadata = LoadMetadata(folderPath);

            // Process each folder sequentially
            for (int i = 0; i < foldersToIndex.Count; i++)
            {
                string folder = foldersToIndex[i];
                string indexPath = Path.Combine(baseIndexPath, $"index_folder_{i}");
                Directory.CreateDirectory(indexPath);
                Console.WriteLine($"Indexing folder {i + 1}/{foldersToIndex.Count}: '{folder}'");

                IndexFolder(folder, indexPath, folderPath, metadata);
            }

            // Save metadata once after all folders are indexed
            SaveMetadata(folderPath, metadata);
            Console.WriteLine("Metadata updated for all folders.");
            UpdateFolderStructure(folderPath);
            Console.WriteLine("All folders indexed successfully.");
        }

        private static void IndexFolder(string folderToIndex, string indexPath, string rootFolderPath, Dictionary<string, DateTime> metadata)
        {
            var updatedMetadata = new ConcurrentDictionary<string, DateTime>();

            using var dir = FSDirectory.Open(indexPath);
            using var analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);

            double ramBufferMB = GetOptimalRamBuffer();
            Console.WriteLine($"Using RAM Buffer: {ramBufferMB}MB for folder '{folderToIndex}'");

            var config = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer)
            {
                OpenMode = OpenMode.CREATE, // Fresh index for each folder
                RAMBufferSizeMB = ramBufferMB,
                MaxBufferedDocs = 2000,
                MergeScheduler = new ConcurrentMergeScheduler()
            };

            var mergePolicy = new TieredMergePolicy
            {
                MaxMergeAtOnce = 20,
                SegmentsPerTier = 15,
                NoCFSRatio = 1.0
            };
            config.SetMergePolicy(mergePolicy);

            using var writer = new IndexWriter(dir, config);

            // Get files in this folder (and subfolders if it’s a small folder)
            var files = Directory.GetFiles(folderToIndex, "*.pdf", SearchOption.AllDirectories).ToList();
            var newOrUpdatedFiles = GetNewOrUpdatedFiles(files, metadata);

            if (newOrUpdatedFiles.Count != 0)
            {
                // Use a thread-safe collection to gather documents from all threads
                var allDocs = new ConcurrentBag<List<Document>>();

                // Process files in parallel without locking the writer
               Parallel.ForEach(newOrUpdatedFiles, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 }, pdfFile =>
                {
                    try
                    {
                        var docs = new List<Document>();
                        foreach (var page in PdfHelper.ExtractTextFromPdfPages(pdfFile)) // Updated to use IEnumerable
                        {
                            var doc = new Document
                            {
                                new StringField("FilePath", pdfFile, Field.Store.YES),
                                new StringField("RelativePath", Path.GetRelativePath(rootFolderPath, pdfFile), Field.Store.YES),
                                new Int32Field("PageNumber", page.Key, Field.Store.YES),
                                new TextField("Content", page.Value, Field.Store.YES)
                            };
                            docs.Add(doc);
                        }

                        if (docs.Count != 0)
                        {
                            allDocs.Add(docs);
                            updatedMetadata[pdfFile] = File.GetLastWriteTimeUtc(pdfFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file '{pdfFile}': {ex.Message}");
                    }
                });
                // Write all documents in a single batch outside the parallel loop
                foreach (var docs in allDocs)
                {
                    writer.AddDocuments(docs);
                }

                // Flush and commit the changes
                writer.Flush(triggerMerge: true, applyAllDeletes: false);
                writer.Commit();

                // Update the shared metadata dictionary
                lock (metadata)
                {
                    foreach (var entry in updatedMetadata)
                    {
                        metadata[entry.Key] = entry.Value;
                    }
                }

                Console.WriteLine($"Folder '{folderToIndex}' indexed: {newOrUpdatedFiles.Count} files.");
            }
            else
            {
                Console.WriteLine($"No new or updated files in folder '{folderToIndex}'.");
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
                Console.WriteLine($"Added {newFolders.Count} new folders to the folder structure.");
            }
            else
            {
                Console.WriteLine("No new folders to add.");
            }
        }

        private static double GetOptimalRamBuffer()
        {
            long totalMemoryMB = (long)(new ComputerInfo().TotalPhysicalMemory / (1024 * 1024));
            return totalMemoryMB switch
            {
                >= 16000 => 1024,  // 16GB+ → 1GB Buffer
                >= 8000 => 512,    // 8GB+ → 512MB Buffer
                _ => 256           // Below 8GB → 256MB Buffer
            };
        }
    }
}