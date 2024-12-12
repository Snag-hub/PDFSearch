namespace PDFSearch;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        string launchDirectory = Directory.GetCurrentDirectory(); // Get current directory
        Console.WriteLine($"Application launched from: {launchDirectory}");

        // Use the directory in your application logic
        Search searchForm = new(launchDirectory);
        Application.Run(searchForm);
    }
}