using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using Directory = System.IO.Directory;
using System.Text;

namespace PDFSearch.Utilities;

public class LuceneSearcher
{    // Generate a unique index folder name for each directory
    private static string GetIndexFolderName(string folderPath)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(folderPath));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public List<SearchResult> SearchInDirectory(string queryText, string folderPath, string? filePath = null)
    {
        try
        {
            // Use folderPath for the base index path
            string baseIndexPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "No1Knows", "Index");
            string uniqueIndexPath = Path.Combine(baseIndexPath, GetIndexFolderName(folderPath));

            // Check if the index directory exists
            if (!Directory.Exists(uniqueIndexPath))
                throw new DirectoryNotFoundException($"No index found for directory: {folderPath}");

            using var dir = FSDirectory.Open(uniqueIndexPath);
            using var analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
            var parser = new QueryParser(Lucene.Net.Util.LuceneVersion.LUCENE_48, "Content", analyzer);
            var query = parser.Parse($"\"{queryText}\"");

            using var reader = DirectoryReader.Open(dir);
            var searcher = new IndexSearcher(reader);

            // Search for all matches first
            var hits = searcher.Search(query, int.MaxValue).ScoreDocs;
            var results = new List<SearchResult>();

            foreach (var hit in hits)
            {
                var doc = searcher.Doc(hit.Doc);

                // Get the content of the document and extract a snippet
                var content = doc.Get("Content");
                var snippet = ExtractSnippet(content, queryText);

                string documentFilePath = doc.Get("FilePath");

                // If filePath is provided, filter results based on it
                if (!string.IsNullOrEmpty(filePath) && documentFilePath.StartsWith(filePath, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new SearchResult
                    {
                        FilePath = documentFilePath,
                        RelativePath = doc.Get("RelativePath"),
                        PageNumber = int.Parse(doc.Get("PageNumber")),
                        Snippet = snippet
                    });
                }
                // If filePath is not provided, include all results from the folderPath index
                else if (string.IsNullOrEmpty(filePath))
                {
                    results.Add(new SearchResult
                    {
                        FilePath = documentFilePath,
                        RelativePath = doc.Get("RelativePath"),
                        PageNumber = int.Parse(doc.Get("PageNumber")),
                        Snippet = snippet
                    });
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            throw; // You might want to log the exception before rethrowing it
        }
    }

    private static string ExtractSnippet(string content, string searchTerm)
    {
        // Look for the search term in the content and extract a snippet
        int termIndex = content.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);

        if (termIndex == -1)
            return string.Empty;

        // Define a snippet length (characters after the term)
        int snippetLength = 100;

        // Start the snippet from the position of the search term
        int start = termIndex;
        // End the snippet at the position of the term + 100 characters (or the end of content)
        int end = Math.Min(termIndex + searchTerm.Length + snippetLength, content.Length);

        // Extract the snippet from the content
        string snippet = content.Substring(start, end - start);

        // Optionally, highlight the search term in the snippet
        snippet = snippet.Replace(searchTerm, $"<b>{searchTerm}</b>", StringComparison.OrdinalIgnoreCase);

        return snippet;
    }
}

public class SearchResult
{
    public string FilePath { get; set; }
    public string RelativePath { get; set; }
    public string Snippet { get; set; }
    public int PageNumber { get; set; }
}
