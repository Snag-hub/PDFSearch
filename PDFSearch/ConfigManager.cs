using System;
using System.IO;
using System.Text.Json;
using PDFSearch.Utilities;

namespace PDFSearch;

public class ConfigManager
{
    public string StartFile { get; set; }
    public string PdfOpener { get; set; }

    // Path to the config file, stored in the hashed folder
    private static string GetConfigFilePath(string folderPath)
    {
        string hashedFolderPath = FolderUtility.GetFolderForPath(folderPath);
        return Path.Combine(hashedFolderPath, "config.json");
    }

    // Load the configuration from the file
    public static ConfigManager LoadConfig(string folderPath)
    {
        string configFilePath = GetConfigFilePath(folderPath);
        if (File.Exists(configFilePath))
        {
            string json = File.ReadAllText(configFilePath);
            return JsonSerializer.Deserialize<ConfigManager>(json);
        }
        else
        {
            return null;
        }
    }

    // Save the configuration to the file
    public void SaveConfig(string folderPath)
    {
        // Ensure the base directory exists
        FolderUtility.EnsureBasePathExists();

        // Get the hashed folder path for the given folderPath
        string hashedFolderPath = FolderUtility.GetFolderForPath(folderPath);

        // Ensure the hashed folder exists
        Directory.CreateDirectory(hashedFolderPath);

        // Serialize the configuration to JSON
        string json = JsonSerializer.Serialize(this);

        // Define the path for the config file inside the hashed folder
        string configFilePath = Path.Combine(hashedFolderPath, "config.json");

        // Write the JSON to the config file
        File.WriteAllText(configFilePath, json);
    }

    // Create a new config with default values (for first-time setup)
    public static ConfigManager CreateDefaultConfig()
    {
        return new ConfigManager
        {
            StartFile = string.Empty,
            PdfOpener = string.Empty
        };
    }
}