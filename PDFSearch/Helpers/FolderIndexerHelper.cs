using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PDFSearch.Helpers;

public static class FolderIndexerHelper
{
    private const long MaxFolderSize = 20L * 1024 * 1024 * 1024; // 20 GB

    public static List<string> GetFoldersToIndex(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"The folder '{folderPath}' does not exist.");

        var foldersToIndex = new List<string>();
        ProcessFolder(folderPath, foldersToIndex);
        return foldersToIndex;
    }

    private static void ProcessFolder(string folderPath, List<string> foldersToIndex)
    {
        var allFiles = Directory.GetFiles(folderPath, "*.pdf", SearchOption.AllDirectories)
            .Select(file => new FileInfo(file))
            .ToList();

        long totalSize = allFiles.Sum(file => file.Length);

        if (totalSize <= MaxFolderSize)
        {
            foldersToIndex.Add(folderPath);
            Console.WriteLine($"Folder '{folderPath}' ({totalSize / (1024 * 1024)} MB) will be indexed as one unit.");
        }
        else
        {
            var subFolders = Directory.GetDirectories(folderPath, "*", SearchOption.TopDirectoryOnly);
            if (subFolders.Length == 0)
            {
                foldersToIndex.Add(folderPath);
                Console.WriteLine($"Folder '{folderPath}' ({totalSize / (1024 * 1024)} MB) has no subfolders but exceeds limit; indexing as is.");
            }
            else
            {
                foreach (var subFolder in subFolders)
                {
                    ProcessFolder(subFolder, foldersToIndex);
                }

                var topLevelFiles = Directory.GetFiles(folderPath, "*.pdf", SearchOption.TopDirectoryOnly);
                if (topLevelFiles.Length != 0)
                {
                    long topLevelSize = topLevelFiles.Sum(file => new FileInfo(file).Length);
                    if (topLevelSize > 0)
                    {
                        foldersToIndex.Add(folderPath);
                        Console.WriteLine($"Top-level files in '{folderPath}' ({topLevelSize / (1024 * 1024)} MB) will be indexed separately.");
                    }
                }
            }
        }
    }
}
