namespace PDFSearch;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        string folderPath;

        if (args.Length > 0)
        {
            folderPath = args[0]; // Path from the context menu
            //MessageBox.Show($"Opening location in Args: {folderPath}", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            folderPath = Environment.CurrentDirectory; // Default fallback
            //MessageBox.Show($"Opening location in Default: {folderPath}", "Info", MessageBoxButtons.OK,                MessageBoxIcon.Information);
        }

        var path = folderPath.Trim();

        if (Directory.Exists(path))
        {

            //MessageBox.Show($"Opening location in Directory: {folderPath}", "Checking location", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Console.WriteLine($"Opening location: {folderPath}");
            Application.Run(new SearchInPDFs(folderPath));

        }
        else
        {
            MessageBox.Show($"Invalid folder path: {folderPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

