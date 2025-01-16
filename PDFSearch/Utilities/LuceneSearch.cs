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
using Directory = System.IO.Directory;

namespace PDFSearch.Utilities;

public class LuceneSearcher
{
    public static List<SearchResult> SearchInDirectory(string queryText, string folderPath, bool matchWord = false, bool matchCase = false, string? filePath = null)
    {
        try
        {
            // Get the unique index path using FolderUtility
            FolderUtility.EnsureBasePathExists();
            string uniqueIndexPath = FolderUtility.GetFolderForPath(folderPath);

            // Check if the index directory exists
            if (!Directory.Exists(uniqueIndexPath))
                throw new DirectoryNotFoundException($"No index found for directory: {folderPath}");

            using var dir = FSDirectory.Open(uniqueIndexPath);

            // Use a custom analyzer for case-sensitive search
            Analyzer analyzer = matchCase ? new CaseSensitiveStandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48)
                                          : new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);

            var parser = new QueryParser(Lucene.Net.Util.LuceneVersion.LUCENE_48, "Content", analyzer);

            // Modify the query based on MatchWord option
            if (matchWord)
            {
                // Wrap the query in double quotes for whole-word matching
                queryText = $"\"{queryText}\"";
            }

            var query = parser.Parse(queryText);

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
                var snippet = ExtractSnippet(content, queryText, matchCase);

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

    private static string ExtractSnippet(string content, string searchTerm, bool matchCase)
    {
        // Look for the search term in the content and extract a snippet
        int termIndex = matchCase ? content.IndexOf(searchTerm)
                                  : content.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);

        if (termIndex == -1)
            return string.Empty;

        // Define a snippet length (characters after the term)
        int snippetLength = 100;

        // Start the snippet from the position of the search term
        int start = Math.Max(0, termIndex - 50); // Include some context before the term
        // End the snippet at the position of the term + 100 characters (or the end of content)
        int end = Math.Min(termIndex + searchTerm.Length + snippetLength, content.Length);

        // Extract the snippet from the content
        string snippet = content.Substring(start, end - start);

        // Optionally, highlight the search term in the snippet
        snippet = snippet.Replace(searchTerm, $"<b>{searchTerm}</b>", matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

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

// Custom analyzer for case-sensitive search
public class CaseSensitiveStandardAnalyzer(Lucene.Net.Util.LuceneVersion matchVersion) : Analyzer
{
    private readonly Lucene.Net.Util.LuceneVersion _matchVersion = matchVersion;

    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        // Use StandardTokenizer for tokenization
        var source = new StandardTokenizer(_matchVersion, reader);

        // Apply StandardFilter for basic normalization
        TokenStream result = new StandardFilter(_matchVersion, source);

        // Skip LowerCaseFilter to preserve case sensitivity
        return new TokenStreamComponents(source, result);
    }
}