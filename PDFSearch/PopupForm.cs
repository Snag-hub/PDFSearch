using FindInPDFs.Utilities;
using Microsoft.Win32;
using PDFSearch;
using FindInPDFs;
using FindInPDFs.Acrobat;

namespace FindInPDFs;

public partial class PopupForm : Form
{
    private readonly string folderPath = string.Empty;
    private readonly Panel overlayPanel;
    private readonly Label loadingLabel;
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
            SearchInPDFs searchInPdFs = new(folderPath);
            searchInPdFs.Show();
        }
    }

    private async void PopupForm_Load(object sender, EventArgs eventArgs)
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
                string? selectedReaderName = selectionForm.Controls.OfType<RadioButton>()
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

        // Registry base paths to check
        string[] registryBasePaths =
        [
            @"SOFTWARE\Adobe\Adobe Acrobat",           // Full Acrobat installations
            @"SOFTWARE\Adobe\Acrobat Reader",         // Acrobat Reader installations
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", // Uninstall entries
            @"SOFTWARE\WOW6432Node\Adobe\Adobe Acrobat",           // 32-bit Acrobat on 64-bit systems
            @"SOFTWARE\WOW6432Node\Adobe\Acrobat Reader",          // 32-bit Reader on 64-bit systems
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" // 32-bit Uninstall
        ];

        // Check HKEY_LOCAL_MACHINE (you can add Registry.CurrentUser if needed)
        RegistryKey[] rootKeys = [Registry.LocalMachine];

        foreach (var rootKey in rootKeys)
        {
            foreach (var basePath in registryBasePaths)
            {
                try
                {
                    using var key = rootKey.OpenSubKey(basePath);
                    if (key == null) continue;

                    if (basePath.Contains("Uninstall"))
                    {
                        // Search Uninstall keys for Adobe Acrobat or Foxit entries
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using var subKey = key.OpenSubKey(subKeyName);
                            if (subKey == null) continue;

                            var displayName = subKey.GetValue("DisplayName")?.ToString();
                            var installLocation = subKey.GetValue("InstallLocation")?.ToString();
                            var exePath = "";

                            if (string.IsNullOrEmpty(displayName)) continue;

                            // Check for Adobe Acrobat or Foxit
                            if (displayName.Contains("Adobe Acrobat") || displayName.Contains("Acrobat Reader"))
                            {
                                if (!string.IsNullOrEmpty(installLocation))
                                {
                                    exePath = FindExecutable(installLocation, "Acrobat.exe") ?? FindExecutable(installLocation, "AcroRd32.exe");
                                }
                            }
                            else if (displayName.Contains("Foxit Reader"))
                            {
                                if (!string.IsNullOrEmpty(installLocation))
                                {
                                    exePath = FindExecutable(installLocation, "FoxitReader.exe");
                                }
                            }

                            if (!string.IsNullOrEmpty(exePath) && !pdfReaders.ContainsKey(displayName))
                            {
                                pdfReaders.Add(displayName, exePath);
                            }
                        }
                    }
                    else
                    {
                        // Search Adobe-specific keys for version subkeys
                        foreach (var version in key.GetSubKeyNames())
                        {
                            using var versionKey = key.OpenSubKey($@"{version}\Installer");
                            if (versionKey == null) continue;

                            var installPath = versionKey.GetValue("Path")?.ToString();
                            if (string.IsNullOrEmpty(installPath)) continue;

                            var exePath = basePath.Contains("Acrobat Reader")
                                ? FindExecutable(installPath, "AcroRd32.exe")
                                : FindExecutable(installPath, "Acrobat.exe");

                            if (!string.IsNullOrEmpty(exePath))
                            {
                                var name = basePath.Contains("Acrobat Reader")
                                    ? $"Adobe Acrobat Reader {version}"
                                    : $"Adobe Acrobat {version}";
                                if (!pdfReaders.ContainsKey(name))
                                {
                                    pdfReaders.Add(name, exePath);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Log exception if needed, e.g., Console.WriteLine($"Error accessing registry: {ex.Message}");
                    continue;
                }
            }
        }

        return pdfReaders;
    }

    // Helper method to find the executable in the installation directory
    private static string? FindExecutable(string installPath, string exeName)
    {
        try
        {
            var fullPath = Path.Combine(installPath, exeName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            // Search subdirectories (e.g., Reader or Acrobat folder)
            foreach (var dir in Directory.GetDirectories(installPath, "*", SearchOption.AllDirectories))
            {
                fullPath = Path.Combine(dir, exeName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }
        catch (Exception)
        {
            // Ignore errors (e.g., access denied)
        }
        return null;
    }
}