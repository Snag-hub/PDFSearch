using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFSearch;

internal class DirectoryUtils
{
    public static string RemoveFileName(string fullPath)
    {
        string path = string.Empty;
        if (!string.IsNullOrEmpty(fullPath))
        {
            path = Path.GetDirectoryName(fullPath);
            if (path != null && path.StartsWith("\\"))
            {
                string driveLetter = fullPath.Substring(0, fullPath.IndexOf(':') + 1);
                path = path.Substring(1).Insert(1, driveLetter + ":");
            }
        }
        return path;
    }

    public static string AbbreviateDirectoryPath(string fullPath, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(fullPath) || fullPath.Length <= maxLength)
            return fullPath;

        string root = Path.GetPathRoot(fullPath); // E.g., "E:\"
        string[] directories = fullPath.Substring(root.Length).Split(Path.DirectorySeparatorChar);

        // Handle edge cases where there's no middle section
        if (directories.Length <= 2)
            return fullPath;

        string middle = "...";
        string leaf = directories[^1]; // Last directory name or file name
        int remainingLength = maxLength - root.Length - leaf.Length - middle.Length - 2; // -2 for separator chars

        if (remainingLength <= 0)
            return $"{root}{middle}{Path.DirectorySeparatorChar}{leaf}";

        string middlePart = string.Join(Path.DirectorySeparatorChar.ToString(), directories.TakeWhile(d => d.Length <= remainingLength));
        return $"{root}{middle}{Path.DirectorySeparatorChar}{middlePart}{Path.DirectorySeparatorChar}{leaf}";
    }
}
