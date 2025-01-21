using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PDFSearch.Utilities;

namespace PDFSearch;

public static class FolderManager
{
    static FolderManager()
    {
        FolderUtility.EnsureBasePathExists();
    }

    // Get the path to the folder structure JSON file for a given root path
    private static string GetJsonFilePath(string rootPath)
    {
        // Get the hashed folder for the root path
        string hashedFolderPath = FolderUtility.GetFolderForPath(rootPath);

        // Ensure the hashed folder exists
        Directory.CreateDirectory(hashedFolderPath);

        // Return the path to the JSON file inside the hashed folder
        return Path.Combine(hashedFolderPath, "FolderStructure.json");
    }

    // Save folder structure to JSON file
    public static void SaveFolderStructure(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            Console.WriteLine($"Root path does not exist: {rootPath}");
            return;
        }

        // Get the folder structure from the root path
        var folderStructure = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories)
                                       .OrderBy(dir => dir)
                                       .ToList();

        // Serialize to JSON
        var json = JsonSerializer.Serialize(folderStructure);

        // Get the path to the JSON file inside the hashed folder
        string jsonFilePath = GetJsonFilePath(rootPath);

        // Write the JSON to the file
        File.WriteAllText(jsonFilePath, json);
        Console.WriteLine($"Folder structure saved to: {jsonFilePath}");
    }

    // Load folder structure from JSON file
    public static List<string> LoadFolderStructure(string rootPath)
    {
        // Get the path to the JSON file inside the hashed folder
        string jsonFilePath = GetJsonFilePath(rootPath);

        if (!File.Exists(jsonFilePath))
        {
            Console.WriteLine("Folder structure file not found.");
            return [];
        }

        // Read and deserialize JSON
        var json = File.ReadAllText(jsonFilePath);
        return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
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