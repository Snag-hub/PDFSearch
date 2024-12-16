using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PDFSearch;

public static class LuceneSearcher
{
    // Generate a unique index folder name for each directory
    private static string GetIndexFolderName(string folderPath)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(folderPath));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public static List<SearchResult> SearchInDirectory(string queryText, string folderPath)
    {
        // Use the same base path as in LuceneIndexer
        string baseIndexPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "No1Knows", "Index");
        string uniqueIndexPath = Path.Combine(baseIndexPath, GetIndexFolderName(folderPath));

        // Check if the index directory exists
        if (!System.IO.Directory.Exists(uniqueIndexPath))
            throw new DirectoryNotFoundException($"No index found for directory: {folderPath}");

        using var dir = FSDirectory.Open(uniqueIndexPath);
        using var analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
        var parser = new QueryParser(Lucene.Net.Util.LuceneVersion.LUCENE_48, "Content", analyzer);
        var query = parser.Parse($"\"{queryText}\"");

        using var reader = DirectoryReader.Open(dir);
        var searcher = new IndexSearcher(reader);

        var hits = searcher.Search(query, int.MaxValue).ScoreDocs;
        var results = new List<SearchResult>();

        foreach (var hit in hits)
        {
            var doc = searcher.Doc(hit.Doc);
            results.Add(new SearchResult
            {
                FilePath = doc.Get("FilePath"),
                RelativePath = doc.Get("RelativePath"),
                PageNumber = int.Parse(doc.Get("PageNumber"))
            });
        }

        return results;
    }
}

public class SearchResult
{
    public string FilePath { get; set; }
    public string RelativePath { get; set; }
    public int PageNumber { get; set; }
}
