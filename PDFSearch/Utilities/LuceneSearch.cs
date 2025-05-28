using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Documents;
using Directory = System.IO.Directory;
using Serilog; // Added for Serilog logging

namespace PDFSearch.Utilities;

public class LuceneSearcher
{
    public List<SearchResult> SearchInDirectory(string queryText, string folderPath, bool matchWord = false, bool matchCase = false, string? filePath = null)
    {
        Log.Information("Starting search in directory: {FolderPath}, Query: {QueryText}, MatchWord: {MatchWord}, MatchCase: {MatchCase}, FilePath: {FilePath}",
            folderPath, queryText, matchWord, matchCase, filePath ?? "None");
        try
        {
            // Get the unique index path using FolderUtility
            FolderUtility.EnsureBasePathExists();
            var uniqueIndexPath = FolderUtility.GetFolderForPath(folderPath);
            Log.Information("Resolved unique index path: {UniqueIndexPath}", uniqueIndexPath);

            // Check if the index directory exists
            if (!Directory.Exists(uniqueIndexPath))
            {
                Log.Error("No index found for directory: {FolderPath}", folderPath);
                throw new DirectoryNotFoundException($"No index found for directory: {folderPath}");
            }

            // Get all subfolder index directories (e.g., index_folder_{name})
            var indexFolders = Directory.GetDirectories(uniqueIndexPath, "index_folder_*", SearchOption.TopDirectoryOnly);
            if (indexFolders.Length == 0)
            {
                Log.Error("No subfolder indexes found in: {UniqueIndexPath}", uniqueIndexPath);
                throw new DirectoryNotFoundException($"No subfolder indexes found in: {uniqueIndexPath}");
            }
            Log.Information("Found {IndexFolderCount} subfolder indexes in: {UniqueIndexPath}", indexFolders.Length, uniqueIndexPath);

            // Create analyzer based on case sensitivity
            Analyzer analyzer = matchCase ? new CaseSensitiveStandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48)
                                         : new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
            Log.Information("Initialized analyzer. CaseSensitive: {MatchCase}", matchCase);

            // Prepare query parser
            var parser = new QueryParser(Lucene.Net.Util.LuceneVersion.LUCENE_48, "Content", analyzer);
            if (matchWord)
            {
                queryText = $"\"{queryText}\"";
                Log.Information("Modified query for whole word match: {QueryText}", queryText);
            }
            var query = parser.Parse(queryText);
            Log.Information("Parsed query: {QueryText}", queryText);

            // Open readers for all subfolder indexes
            var readers = new List<IndexReader>();
            foreach (var indexFolder in indexFolders)
            {
                try
                {
                    var dir = FSDirectory.Open(indexFolder);
                    var reader = DirectoryReader.Open(dir);
                    readers.Add(reader);
                    Log.Information("Opened IndexReader for index folder: {IndexFolder}", indexFolder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] Failed to open index {indexFolder}: {ex.Message}");
                    Log.Warning(ex, "Failed to open index {IndexFolder}: {Message}", indexFolder, ex.Message);
                    // Continue with other indexes
                }
            }

            if (readers.Count == 0)
            {
                Log.Error("No valid index readers could be opened.");
                throw new Exception("No valid index readers could be opened.");
            }
            Log.Information("Successfully opened {ReaderCount} IndexReaders.", readers.Count);

            // Use MultiReader to search across all indexes
            using var multiReader = new MultiReader(readers.ToArray(), true); // true to close readers
            var searcher = new IndexSearcher(multiReader);
            Log.Information("Initialized IndexSearcher with MultiReader.");

            // Search with a limit of 1000 hits
            var hits = searcher.Search(query, 1000).ScoreDocs;
            Log.Information("Search returned {HitCount} hits.", hits.Length);

            var results = (from hit in hits
                           let doc = searcher.Doc(hit.Doc)
                           let content = doc.Get("Content") ?? ""
                           let snippet = ExtractSnippet(content, queryText, matchCase)
                           let documentFilePath = doc.Get("FilePath") ?? ""
                           where string.IsNullOrEmpty(filePath) || documentFilePath.StartsWith(filePath, StringComparison.OrdinalIgnoreCase)
                           select new SearchResult
                           {
                               FilePath = documentFilePath,
                               RelativePath = doc.Get("RelativePath") ?? "",
                               PageNumber = int.TryParse(doc.Get("PageNumber"), out var page) ? page : 0,
                               Snippet = snippet,
                               Score = hit.Score
                           }).ToList();
            Log.Information("Processed {ResultCount} search results.", results.Count);

            // Remove duplicates (same FilePath and PageNumber) and sort by score
            results = results
                .GroupBy(r => (r.FilePath, r.PageNumber))
                .Select(g => g.OrderByDescending(r => r.Score).First())
                .OrderByDescending(r => r.Score)
                .ToList();
            Log.Information("After removing duplicates and sorting, final result count: {FinalCount}", results.Count);

            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Search failed: {ex.Message}");
            Log.Error(ex, "Search failed: {Message}", ex.Message);
            throw;
        }
    }

    private static string ExtractSnippet(string content, string searchTerm, bool matchCase)
    {
        Log.Information("Extracting snippet for search term: {SearchTerm}, MatchCase: {MatchCase}", searchTerm, matchCase);
        var termIndex = matchCase ? content.IndexOf(searchTerm)
                                 : content.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);

        if (termIndex == -1)
        {
            Log.Information("Search term not found in content.");
            return "";
        }

        var snippetLength = 100;
        var start = Math.Max(0, termIndex - 50);
        var end = Math.Min(termIndex + searchTerm.Length + snippetLength, content.Length);
        var snippet = content.Substring(start, end - start);

        // Highlight search term
        snippet = snippet.Replace(searchTerm, $"<b>{searchTerm}</b>", matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        Log.Information("Snippet extracted: {Snippet}", snippet);
        return snippet;
    }
}

public class SearchResult
{
    public string FilePath { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string Snippet { get; set; } = "";
    public int PageNumber { get; set; }
    public float Score { get; set; }
}

public class CaseSensitiveStandardAnalyzer : Analyzer
{
    private readonly Lucene.Net.Util.LuceneVersion _matchVersion;

    public CaseSensitiveStandardAnalyzer(Lucene.Net.Util.LuceneVersion matchVersion)
    {
        Log.Information("Initializing CaseSensitiveStandardAnalyzer with Lucene version: {MatchVersion}", matchVersion);
        _matchVersion = matchVersion;
    }

    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        Log.Information("Creating TokenStreamComponents for field: {FieldName}", fieldName);
        var source = new StandardTokenizer(_matchVersion, reader);
        TokenStream result = new StandardFilter(_matchVersion, source);
        Log.Information("Created TokenStreamComponents for field: {FieldName}", fieldName);
        return new TokenStreamComponents(source, result);
    }
}