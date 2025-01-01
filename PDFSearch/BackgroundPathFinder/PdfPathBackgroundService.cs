using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using Timer = System.Threading.Timer;

namespace PDFSearch.BackgroundPathFinder;

// Interface for Acrobat Service
public interface IAcrobatService
{
    dynamic? GetActiveDocument();
}

// Implementation of the Acrobat Service
public class AcrobatService : IAcrobatService
{
    public dynamic? GetActiveDocument()
    {
        // Create Acrobat application object
        var acroApp = Activator.CreateInstance(Type.GetTypeFromProgID("AcroExch.App")) as dynamic;
        return acroApp?.GetActiveDoc();
    }
}

public class PdfPathBackgroundService
{
    private readonly ILogger<PdfPathBackgroundService> _logger;
    private readonly IAcrobatService _acrobatService;
    private string? _filePath;
    private Timer? _timer;

    public string? FilePath => _filePath; // Property to get the file path

    public PdfPathBackgroundService(ILogger<PdfPathBackgroundService> logger, IAcrobatService acrobatService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _acrobatService = acrobatService ?? throw new ArgumentNullException(nameof(acrobatService));
    }

    // Start the Timer
    public void Start()
    {
        _logger.LogInformation("PdfPathBackgroundService is starting.");

        // Create a Timer to run every 2 seconds
        _timer = new Timer(Callback, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }

    // Callback function to retrieve the active PDF file path
    private void Callback(object? state)
    {
        try
        {
            _logger.LogInformation("Retrieving active PDF file path.");

            // Get the active document (AVDoc)
            var avDoc = _acrobatService.GetActiveDocument();
            if (avDoc == null)
            {
                _logger.LogWarning("No active document found.");
                return;
            }

            // Retrieve the PDDoc object
            var pdDoc = avDoc.GetPDDoc();
            if (pdDoc == null)
            {
                _logger.LogWarning("No PDDoc found.");
                return;
            }

            // Get the JavaScript object
            object jsObj = pdDoc.GetJSObject();
            if (jsObj == null)
            {
                _logger.LogWarning("No JavaScript object found.");
                return;
            }

            // Retrieve the file path using JavaScript
            string? filePath = jsObj.GetType()
                                   .InvokeMember("path", System.Reflection.BindingFlags.GetProperty, null, jsObj, null) as string;

            // Store the file path if it's valid
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogInformation($"Active PDF File Path: {filePath}");
                _filePath = filePath; // Update the file path
            }
            else
            {
                _logger.LogWarning("Failed to retrieve file path.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred: {ex.Message}");
        }
    }

    // Stop the timer when you're done
    public void Stop()
    {
        _timer?.Dispose();
        _logger.LogInformation("PdfPathBackgroundService has stopped.");
    }
}
