using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PDFSearch.Utilities;

public static class FolderUtility
{
    public static readonly string BasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "No1Knows",
        "Index"
    );

    public static void EnsureBasePathExists()
    {
        Directory.CreateDirectory(BasePath);
    }

    public static string GetFolderForPath(string folderPath)
    {
        var hash = GenerateHashedFolderName(folderPath);
        return Path.Combine(BasePath, hash);
    }

    public static string GenerateHashedFolderName(string folderPath)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(folderPath));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public static void CleanAllFolders()
    {
        if (Directory.Exists(BasePath))
        {
            Directory.Delete(BasePath, recursive: true);
            EnsureBasePathExists();
        }
    }
}

