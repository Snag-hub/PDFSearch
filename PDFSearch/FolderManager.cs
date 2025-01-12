using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PDFSearch;

public static class FolderManager
{
    private static readonly string JsonFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "No1Knows",
        "FolderStructure.json"
    );

    static FolderManager()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(JsonFilePath) ?? string.Empty);
    }

    // Save folder structure to JSON file
    public static void SaveFolderStructure(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            Console.WriteLine($"Root path does not exist: {rootPath}");
            return;
        }

        var folderStructure = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories)
                                       .OrderBy(dir => dir)
                                       .ToList();

        var json = JsonSerializer.Serialize(folderStructure);
        File.WriteAllText(JsonFilePath, json);
        Console.WriteLine("Folder structure saved.");
    }

    // Load folder structure from JSON file
    public static List<string> LoadFolderStructure()
    {
        if (!File.Exists(JsonFilePath))
        {
            Console.WriteLine("Folder structure file not found.");
            return [];
        }

        var json = File.ReadAllText(JsonFilePath);
        return JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }

    // Display folder structure
    public static void DisplayFolderStructure(List<string> folders)
    {
        Console.WriteLine("Available Folders:");
        for (int i = 0; i < folders.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {folders[i]}");
        }
    }

    // Get selected range of folders
    public static List<string> GetSelectedFolders(List<string> folders, int startIndex, int endIndex)
    {
        return folders.Skip(startIndex - 1).Take(endIndex - startIndex + 1).ToList();
    }
}
