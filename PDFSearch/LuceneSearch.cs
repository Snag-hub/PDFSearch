using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace PDFSearch;

public static class LuceneSearcher
{
    private const string IndexPath = "LuceneIndex"; // Directory for Lucene index

    /// <summary>
    /// Searches the Lucene index for a given term and returns all matches with their details.
    /// </summary>
    /// <param name="searchTerm">The term to search for.</param>
    /// <returns>List of search results with file path, page number, and preview text.</returns>
    public static List<SearchResult> SearchIndexWithPage(string searchTerm)
    {
        var results = new List<SearchResult>();

        try
        {
            using var directory = FSDirectory.Open(IndexPath);
            using var reader = DirectoryReader.Open(directory);
            var searcher = new IndexSearcher(reader);

            var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            var parser = new QueryParser(LuceneVersion.LUCENE_48, "Content", analyzer);
            // var query = parser.Parse(searchTerm);
            var query = parser.Parse($"\"{searchTerm}\"");

            // Search for all matching results
            var hits = searcher.Search(query, int.MaxValue).ScoreDocs;

            foreach (var hit in hits)
            {
                var doc = searcher.Doc(hit.Doc);

                results.Add(new SearchResult(
                    filePath: doc.Get("FilePath"),
                    pageNumber: int.Parse(doc.Get("PageNumber")),
                    previewText: doc.Get("Content")
                ));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Error searching for term {searchTerm}: {ex.Message}");
        }

        return results;
    }
}

public class SearchResult(string filePath, int pageNumber, string previewText)
{
    public string FilePath { get; } = filePath;
    public int PageNumber { get; } = pageNumber;
    public string PreviewText { get; } = previewText;

    public override string ToString()
    {
        return $"File: {FilePath}, Page: {PageNumber}, Preview: {PreviewText}";
    }
}
