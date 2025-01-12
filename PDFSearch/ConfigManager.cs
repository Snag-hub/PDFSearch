using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PDFSearch;

public class ConfigManager
{
    public string StartFile { get; set; }
    public string PdfOpener { get; set; }

    private static string ConfigFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "No1Knows", "config.json");

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
        string json = JsonSerializer.Serialize(this);
        string directory = Path.GetDirectoryName(ConfigFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
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
