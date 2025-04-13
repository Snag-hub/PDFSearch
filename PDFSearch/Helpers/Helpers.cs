using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFSearch.Helpers;

internal static class Helpers
{
    public static string GetShortenedDirectoryPath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return string.Empty;

        // Get the directory info
        var directoryInfo = new DirectoryInfo(fullPath);

        // Get the root drive letter (e.g., C:)
        string driveLetter = directoryInfo.Root.FullName;

        // Split the full path into directory names
        var directoryParts = fullPath.Substring(driveLetter.Length).Trim(Path.DirectorySeparatorChar)
                                    .Split(Path.DirectorySeparatorChar);

        // If there are more than two directories, shorten the middle part
        if (directoryParts.Length > 2)
        {
            var secondLastPart = directoryParts[directoryParts.Length - 2];
            var lastPart = directoryParts[directoryParts.Length - 1];
            return $"{driveLetter}{secondLastPart}\\{lastPart}";
        }
        else
        {
            // If two or fewer directories, return them as is
            return fullPath;
        }
    }

    // Helper method to estimate CPU usage
    public static double GetCpuUsage(Process process)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpuTime = process.TotalProcessorTime;
            Thread.Sleep(500); // Wait briefly to measure usage
            var endTime = DateTime.UtcNow;
            var endCpuTime = process.TotalProcessorTime;

            var cpuTimeUsed = (endCpuTime - startCpuTime).TotalMilliseconds;
            var elapsedTime = (endTime - startTime).TotalMilliseconds;
            var cpuUsage = (cpuTimeUsed / (elapsedTime * Environment.ProcessorCount)) * 100;

            return Math.Min(cpuUsage, 100); // Cap at 100%
        }
        catch
        {
            return 0; // Fallback if measurement fails
        }
    }

}
