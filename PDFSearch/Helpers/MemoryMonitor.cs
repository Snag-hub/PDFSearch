using System.Diagnostics;
using Microsoft.VisualBasic.Devices;

namespace FindInPDFs.Helpers;

public static class MemoryMonitor
{
    private static readonly ComputerInfo _computerInfo = new ComputerInfo();
    private static readonly ulong _totalMemory = _computerInfo.TotalPhysicalMemory;
    private const long MinimumMemoryThreshold = 512 * 1024 * 1024; // 512MB
    
    public static bool IsMemorySafe()
    {
        var process = Process.GetCurrentProcess();
        var workingSet = (ulong)process.WorkingSet64;
        var availableMemory = _totalMemory - workingSet;
        
        return availableMemory > MinimumMemoryThreshold && 
               (double)workingSet / _totalMemory < 0.7;
    }
    
    public static void ForceGCIfNeeded()
    {
        var process = Process.GetCurrentProcess();
        if (!((double)process.WorkingSet64 / _totalMemory > 0.65)) return;
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
    }
}
