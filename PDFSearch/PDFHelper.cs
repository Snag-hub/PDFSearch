﻿using System;
using System.Collections.Generic;
using System.IO;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PDFSearch;

public static class PdfHelper
{
    public static Dictionary<string, Dictionary<int, string>> ExtractTextFromMultipleDirectories(IEnumerable<string> directories)
    {
        var extractedData = new Dictionary<string, Dictionary<int, string>>();

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"Directory does not exist: {directory}");
                continue;
            }

            var pdfFiles = Directory.GetFiles(directory, "*.pdf", SearchOption.AllDirectories);
            foreach (var pdfFile in pdfFiles)
            {
                try
                {
                    var textByPage = ExtractTextFromPdfPages(pdfFile);
                    extractedData[pdfFile] = textByPage;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {pdfFile}: {ex.Message}");
                }
            }
        }
        return extractedData;
    }

    public static Dictionary<int, string> ExtractTextFromPdfPages(string pdfFilePath)
    {
        var textByPage = new Dictionary<int, string>();
        using var pdfDoc = PdfDocument.Open(pdfFilePath);

        foreach (var page in pdfDoc.GetPages())
        {
            textByPage[page.Number] = page.Text;
        }

        return textByPage;
    }
}
