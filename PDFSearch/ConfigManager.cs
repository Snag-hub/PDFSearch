using System;
using System.IO;
using System.Text.Json;
using PDFSearch.Utilities;

namespace PDFSearch;

public class ConfigManager
{
    public string StartFile { get; set; }
    public string PdfOpener { get; set; }

    // Path to the config file, stored in the same base path as FolderManager
    private static string ConfigFilePath => Path.Combine(FolderUtility.BasePath, "config.json");

    // Load the configuration from the file
    public static ConfigManager LoadConfig()
    {
        if (File.Exists(ConfigFilePath))
        {
            string json = File.ReadAllText(ConfigFilePath);
            return JsonSerializer.Deserialize<ConfigManager>(json);
        }
        else
        {
            return null;
        }
    }

    // Save the configuration to the file
    public void SaveConfig()
    {
        // Ensure the base directory exists
        FolderUtility.EnsureBasePathExists();

        // Serialize the configuration to JSON
        string json = JsonSerializer.Serialize(this);

        // Write the JSON to the config file
        File.WriteAllText(ConfigFilePath, json);
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