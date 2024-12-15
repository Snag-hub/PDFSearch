//namespace PDFSearch;

//static class Program
//{
//    /// <summary>
//    ///  The main entry point for the application.
//    /// </summary>
//    [STAThread]
//    static void Main()
//    {
//        string launchDirectory = Directory.GetCurrentDirectory(); // Get current directory
//        Console.WriteLine($"Application launched from: {launchDirectory}");

//        // Use the directory in your application logic
//        Search searchForm = new(launchDirectory);
//        Application.Run(searchForm);
//    }
//}

namespace PDFSearch;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        string folderPath;

        if (args.Length > 0)
        {
            folderPath = args[0]; // Path from the context menu
        }
        else
        {
            folderPath = Environment.CurrentDirectory; // Default fallback
        }

        if (Directory.Exists(folderPath))
        {
            Console.WriteLine($"Opening location: {folderPath}");
            Application.Run(new Search(folderPath));
        }
        else
        {
            MessageBox.Show($"Invalid folder path: {folderPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

