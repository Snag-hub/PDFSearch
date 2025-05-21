namespace FindInPDFs;

public class IndexingProgressEventArgs : EventArgs
{
    public string CurrentSubfolder { get; }
    public int Percentage { get; }

    public IndexingProgressEventArgs(string currentSubfolder, int percentage)
    {
        CurrentSubfolder = currentSubfolder;
        Percentage = percentage;
    }
}