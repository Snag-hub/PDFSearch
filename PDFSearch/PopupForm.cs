using PDFSearch.Acrobat;
using PDFSearch.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDFSearch;

public partial class PopupForm : Form
{
    private readonly string folderPath = string.Empty;
    private Panel overlayPanel;
    private Label loadingLabel;
    private readonly AcrobatWindowManager acrobatWindowManager;

    // Static reference to keep track of the instance
    private static PopupForm instance;

    public PopupForm(string folderPath)
    {
        this.folderPath = folderPath;
        InitializeComponent();

        acrobatWindowManager = new AcrobatWindowManager(folderPath);

        // Check if configuration exists
        ConfigManager config = ConfigManager.LoadConfig(folderPath);

        if (config == null)
        {
            // If no config, show the first-time setup
            ShowFirstTimeSetup();
        }

        // Prevent the form from being maximized
        this.FormBorderStyle = FormBorderStyle.FixedSingle; // or FormBorderStyle.FixedDialog
        this.MaximizeBox = false;

        // Create the overlay panel
        overlayPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = System.Drawing.Color.FromArgb(64, 128, 128, 128), // Semi-transparent grey
            Visible = false
        };
        this.Controls.Add(overlayPanel);
        this.Controls.SetChildIndex(overlayPanel, 0); // Ensure overlay is on top

        // Create and configure the loading label
        loadingLabel = new Label
        {
            Text = "Indexing is in progress...\nPlease Wait and do not close the app.",
            AutoSize = false,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            ForeColor = System.Drawing.Color.Black,
            Font = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Regular)
        };
        overlayPanel.Controls.Add(loadingLabel);

        instance = this;
    }

    public static PopupForm Instance => instance;

    private void BtnLaunchSearch_Click(object sender, EventArgs e)
    {
        if (SearchInPDFs.Instance is not null)
        {
            if (SearchInPDFs.Instance.WindowState == FormWindowState.Minimized)
            {
                // Restore the existing PopupForm if it's minimized
                if (SearchInPDFs.Instance != null && SearchInPDFs.Instance.WindowState == FormWindowState.Minimized)
                {
                    SearchInPDFs.Instance.WindowState = FormWindowState.Normal;
                    this.WindowState = FormWindowState.Minimized;
                    SearchInPDFs.Instance.BringToFront();
                }
            }
        }
        else
        {
            // Minimize the current form
            this.WindowState = FormWindowState.Minimized;

            // Show the new search form and arrange windows after it's shown
            SearchInPDFs searchInPDFs = new(folderPath);
            searchInPDFs.Show();
        }
    }

    public async void PopupForm_Load(object sender, EventArgs e)
    {
        // Show loading indicator and start indexing in background
        await ProcessIndexingInBackground();
        acrobatWindowManager.FindOrLaunchAcrobatWindow();
    }

    private async Task ProcessIndexingInBackground()
    {
        try
        {
            // Show the overlay panel and disable controls on the main thread
            Invoke(new Action(() =>
            {
                overlayPanel.Visible = true;
                foreach (Control control in this.Controls)
                {
                    if (control != overlayPanel)
                    {
                        control.Enabled = false;
                    }
                }
                Console.WriteLine("Indexing started");  // For debugging
            }));

            // Perform the indexing in a background thread
            await Task.Run(() => LuceneIndexer.IndexDirectory(folderPath));
        }
        catch (Exception ex)
        {
            // Handle the exception as needed
            Invoke(new Action(() =>
            {
                MessageBox.Show($"Error during indexing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }));
        }
        finally
        {
            // Hide the overlay panel and enable controls on the main thread
            Invoke(new Action(() =>
            {
                overlayPanel.Visible = false;
                foreach (Control control in this.Controls)
                {
                    if (control != overlayPanel)
                    {
                        control.Enabled = true;
                    }
                }
                Console.WriteLine("Indexing completed");  // For debugging
            }));
        }
    }

    private void ShowFirstTimeSetup()
    {
        // Show file dialog for index.pdf
        OpenFileDialog openFileDialog = new()
        {
            Filter = "PDF Files|*.pdf",
            Title = "Select the Start/Landing Page File (index.pdf)"
        };

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            string startFile = openFileDialog.FileName;

            // Get installed PDF readers
            var installedReaders = GetInstalledPDFReaders();

            if (installedReaders.Count == 0)
            {
                MessageBox.Show("No PDF readers found on the system. Please install a PDF reader and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Create a form for PDF reader selection
            Form selectionForm = new()
            {
                Text = "Select PDF Reader",
                Size = new System.Drawing.Size(400, 400),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                WindowState = FormWindowState.Normal
            };

            // Add a label
            Label label = new()
            {
                Text = "Select your preferred PDF reader:",
                Location = new System.Drawing.Point(20, 20),
                AutoSize = true
            };
            selectionForm.Controls.Add(label);

            // Add RadioButtons for each PDF reader
            int yOffset = 50;
            foreach (var reader in installedReaders)
            {
                RadioButton radioButton = new()
                {
                    Text = reader.Key,
                    Location = new System.Drawing.Point(20, yOffset),
                    AutoSize = true
                };
                selectionForm.Controls.Add(radioButton);
                yOffset += 30;
            }

            // Add an OK button
            Button okButton = new()
            {
                Text = "OK",
                Location = new System.Drawing.Point(150, yOffset),
                DialogResult = DialogResult.OK
            };
            selectionForm.Controls.Add(okButton);

            // Show the selection form
            if (selectionForm.ShowDialog() == DialogResult.OK)
            {
                string selectedReaderName = selectionForm.Controls.OfType<RadioButton>()
                    .FirstOrDefault(rb => rb.Checked)?.Text;

                if (selectedReaderName != null)
                {
                    string selectedReaderPath = installedReaders[selectedReaderName];

                    // Save the config
                    ConfigManager config = new()
                    {
                        StartFile = startFile,
                        PdfOpener = selectedReaderPath
                    };

                    config.SaveConfig(folderPath);

                    MessageBox.Show("Configuration saved successfully!");
                }
            }
        }
    }

    private static Dictionary<string, string> GetInstalledPDFReaders()
    {
        var pdfReaders = new Dictionary<string, string>();

        // Common PDF readers and their default installation paths
        string[] programFilesPaths =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), // C:\Program Files (x86)
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)     // C:\Program Files
        ];

        // Detect multiple versions of Adobe Acrobat Reader
        foreach (var programFilesPath in programFilesPaths)
        {
            // Check for Adobe Acrobat Reader DC
            string acrobatDCPath = Path.Combine(programFilesPath, @"Adobe\Acrobat Reader DC\Reader\AcroRd32.exe");
            if (File.Exists(acrobatDCPath))
            {
                pdfReaders.Add("Adobe Acrobat Reader DC", acrobatDCPath);
            }

            string acrobatDC2Path = Path.Combine(programFilesPath, @"Adobe\Acrobat DC\Acrobat\Acrobat.exe");
            if (File.Exists(acrobatDC2Path))
            {
                pdfReaders.Add("Adobe Acrobat Reader", acrobatDC2Path);
            }

            string acrobat10Path = Path.Combine(programFilesPath, @"Adobe\Acrobat 10.0\Acrobat\AcroRd32.exe");
            if (File.Exists(acrobat10Path))
            {
                pdfReaders.Add("Adobe Acrobat Reader DC", acrobat10Path);
            }

            // Check for Adobe Acrobat Reader 2023
            string acrobat2023Path = Path.Combine(programFilesPath, @"Adobe\Acrobat Reader 2023\Reader\AcroRd32.exe");
            if (File.Exists(acrobat2023Path))
            {
                pdfReaders.Add("Adobe Acrobat Reader 2023", acrobat2023Path);
            }

            // Check for Adobe Acrobat Reader 2020
            string acrobat2020Path = Path.Combine(programFilesPath, @"Adobe\Acrobat Reader 2020\Reader\AcroRd32.exe");
            if (File.Exists(acrobat2020Path))
            {
                pdfReaders.Add("Adobe Acrobat Reader 2020", acrobat2020Path);
            }

            // Add more versions as needed...
        }

        // Foxit Reader
        foreach (var programFilesPath in programFilesPaths)
        {
            string foxitPath = Path.Combine(programFilesPath, @"Foxit Software\Foxit Reader\FoxitReader.exe");
            if (File.Exists(foxitPath))
            {
                pdfReaders.Add("Foxit Reader", foxitPath);
                break; // Stop searching once found
            }
        }

        // Google Chrome
        foreach (var programFilesPath in programFilesPaths)
        {
            string chromePath = Path.Combine(programFilesPath, @"Google\Chrome\Application\chrome.exe");
            if (File.Exists(chromePath))
            {
                pdfReaders.Add("Google Chrome", chromePath);
                break; // Stop searching once found
            }
        }

        return pdfReaders;
    }
}