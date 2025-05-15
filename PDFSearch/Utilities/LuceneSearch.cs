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

namespace PDFSearch.Utilities;

public class LuceneSearcher
{
    public List<SearchResult> SearchInDirectory(string queryText, string folderPath, bool matchWord = false, bool matchCase = false, string? filePath = null)
    {
        try
        {
            // Get the unique index path using FolderUtility
            FolderUtility.EnsureBasePathExists();
            var uniqueIndexPath = FolderUtility.GetFolderForPath(folderPath);

            // Check if the index directory exists
            if (!Directory.Exists(uniqueIndexPath))
                throw new DirectoryNotFoundException($"No index found for directory: {folderPath}");

            // Get all subfolder index directories (e.g., index_folder_{name})
            var indexFolders = Directory.GetDirectories(uniqueIndexPath, "index_folder_*", SearchOption.TopDirectoryOnly);
            if (indexFolders.Length == 0)
                throw new DirectoryNotFoundException($"No subfolder indexes found in: {uniqueIndexPath}");

            // Create analyzer based on case sensitivity
            Analyzer analyzer = matchCase ? new CaseSensitiveStandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48)
                                         : new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);

            // Prepare query parser
            var parser = new QueryParser(Lucene.Net.Util.LuceneVersion.LUCENE_48, "Content", analyzer);
            if (matchWord)
            {
                queryText = $"\"{queryText}\"";
            }
            var query = parser.Parse(queryText);

            // Open readers for all subfolder indexes
            var readers = new List<IndexReader>();
            foreach (var indexFolder in indexFolders)
            {
                try
                {
                    var dir = FSDirectory.Open(indexFolder);
                    var reader = DirectoryReader.Open(dir);
                    readers.Add(reader);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] Failed to open index {indexFolder}: {ex.Message}");
                    // Continue with other indexes
                }
            }

            if (readers.Count == 0)
                throw new Exception("No valid index readers could be opened.");

            // Use MultiReader to search across all indexes
            using var multiReader = new MultiReader(readers.ToArray(), true); // true to close readers
            var searcher = new IndexSearcher(multiReader);

            // Search with a limit of 1000 hits
            var hits = searcher.Search(query, 1000).ScoreDocs;
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

            // Remove duplicates (same FilePath and PageNumber) and sort by score
            results = results
                .GroupBy(r => (r.FilePath, r.PageNumber))
                .Select(g => g.OrderByDescending(r => r.Score).First())
                .OrderByDescending(r => r.Score)
                .ToList();

            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Search failed: {ex.Message}");
            throw;
        }
    }

    private static string ExtractSnippet(string content, string searchTerm, bool matchCase)
    {
        var termIndex = matchCase ? content.IndexOf(searchTerm)
                                 : content.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);

        if (termIndex == -1)
            return "";

        var snippetLength = 100;
        var start = Math.Max(0, termIndex - 50);
        var end = Math.Min(termIndex + searchTerm.Length + snippetLength, content.Length);
        var snippet = content.Substring(start, end - start);

        // Highlight search term
        snippet = snippet.Replace(searchTerm, $"<b>{searchTerm}</b>", matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
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
        _matchVersion = matchVersion;
    }

    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        var source = new StandardTokenizer(_matchVersion, reader);
        TokenStream result = new StandardFilter(_matchVersion, source);
        return new TokenStreamComponents(source, result);
    }
}