using FindInPDFs.Acrobat;
using FindInPDFs.Utilities;
using Microsoft.Win32;
using PDFSearch;
using PDFSearch.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace FindInPDFs;

public partial class PopupForm : Form
{
    private readonly string folderPath = string.Empty;
    private readonly AcrobatWindowManager acrobatWindowManager;
    private static PopupForm instance;
    private CancellationTokenSource _indexingCts;

    public PopupForm(string folderPath)
    {
        this.folderPath = folderPath;
        InitializeComponent();

        acrobatWindowManager = new AcrobatWindowManager(folderPath);
        _indexingCts = new CancellationTokenSource();

        // Check if configuration exists
        ConfigManager config = ConfigManager.LoadConfig(folderPath);

        if (config == null)
        {
            ShowFirstTimeSetup();
        }

        // Prevent the form from being maximized
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        // Check for previous indexing state
        var stateFilePath = Path.Combine(FolderUtility.GetFolderForPath(folderPath), "indexState.json");
        if (File.Exists(stateFilePath))
        {
            statusLabel.Text = "Previous indexing paused. Indexing will resume.";
        }
        else
        {
            statusLabel.Text = "Ready to start indexing.";
        }

        instance = this;
    }

    public static PopupForm Instance => instance;

    private async void PopupForm_Load(object sender, EventArgs e)
    {

        // Start indexing automatically
        btnPlayPause.Text = "Pause";
        btnPlayPause.Visible = true;
        statusLabel.Text = "Indexing started...";
        await ProcessIndexingInBackground();

        // Launch Acrobat window
        acrobatWindowManager.FindOrLaunchAcrobatWindow();
    }

    private void btnPlayPause_Click(object sender, EventArgs e)
    {
        try
        {
            // Cancel indexing (pause)
            _indexingCts.Cancel();
            btnPlayPause.Visible = false; // Hide to enforce restart
            statusLabel.Text = "Indexing cancelled. Restart the application to resume.";
        }
        catch (Exception ex)
        {
            Invoke(new Action(() =>
            {
                statusLabel.Text = $"Error: {ex.Message}";
                progressBarIndexing.Visible = false;
                btnPlayPause.Visible = false;
                foreach (Control control in this.Controls)
                {
                    control.Visible = true;
                }
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"[ERROR] Play/Pause error: {ex.Message}");
            }));
        }
    }

    private void BtnLaunchSearch_Click(object sender, EventArgs e)
    {
        if (SearchInPDFs.Instance is not null)
        {
            if (SearchInPDFs.Instance.WindowState == FormWindowState.Minimized)
            {
                // Restore the existing SearchInPDFs if it's minimized
                SearchInPDFs.Instance.WindowState = FormWindowState.Normal;
                this.WindowState = FormWindowState.Minimized;
                SearchInPDFs.Instance.BringToFront();
            }
        }
        else
        {
            // Minimize the current form
            this.WindowState = FormWindowState.Minimized;

            // Show the new search form
            SearchInPDFs searchInPdFs = new(folderPath);
            searchInPdFs.Show();
        }
    }

    /// <summary>
    /// Processes indexing in the background with progress feedback.
    /// </summary>
    private async Task ProcessIndexingInBackground()
    {
        try
        {
            // Show ProgressBar and hide controls
            Invoke(new Action(() =>
            {
                progressBarIndexing.Visible = true;
                progressBarIndexing.Value = 0;
                statusLabel.Text = "Indexing started...";
                foreach (Control control in this.Controls)
                {
                    if (control != progressBarIndexing && control != statusLabel && control != btnPlayPause)
                    {
                        control.Visible = false;
                    }
                }
                Console.WriteLine("[INFO] Indexing started");
            }));

            // Perform indexing in a background thread
            await Task.Run(() =>
            {
                LuceneIndexer.IndexDirectory(
                    folderPath,
                    (progress, total) =>
                    {
                        int percentage = total > 0 ? (int)((progress / (double)total) * 100) : 0;
                        Invoke(new Action(() =>
                        {
                            progressBarIndexing.Value = Math.Min(percentage, 100);
                            statusLabel.Text = $"Indexing: {percentage}% ({progress}/{total} files)";
                        }));
                    },
                    _indexingCts.Token);
            });

            // Indexing completed
            Invoke(new Action(() =>
            {
                statusLabel.Text = "Indexing completed.";
                progressBarIndexing.Visible = false;
                btnPlayPause.Visible = false;
                foreach (Control control in this.Controls)
                {
                    control.Visible = true;
                }
                // Clear state on completion
                var stateFilePath = Path.Combine(FolderUtility.GetFolderForPath(folderPath), "indexState.json");
                if (File.Exists(stateFilePath))
                {
                    File.Delete(stateFilePath);
                }
                Console.WriteLine("[INFO] Indexing completed");
            }));
        }
        catch (OperationCanceledException)
        {
            // Indexing cancelled
            Invoke(new Action(() =>
            {
                statusLabel.Text = "Indexing cancelled. Restart the application to resume.";
                progressBarIndexing.Visible = false;
                btnPlayPause.Visible = false;
                foreach (Control control in this.Controls)
                {
                    control.Visible = true;
                }
                Console.WriteLine("[INFO] Indexing cancelled");
            }));
        }
        catch (Exception ex)
        {
            // Handle errors
            Invoke(new Action(() =>
            {
                statusLabel.Text = $"Error during indexing: {ex.Message}";
                progressBarIndexing.Visible = false;
                btnPlayPause.Visible = false;
                foreach (Control control in this.Controls)
                {
                    control.Visible = true;
                }
                MessageBox.Show($"Error during indexing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"[ERROR] Indexing failed: {ex.Message}");
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
            @"SOFTWARE\Adobe\Adobe Acrobat",
            @"SOFTWARE\Adobe\Acrobat Reader",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Adobe\Adobe Acrobat",
            @"SOFTWARE\WOW6432Node\Adobe\Acrobat Reader",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        ];

        // Check HKEY_LOCAL_MACHINE
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
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using var subKey = key.OpenSubKey(subKeyName);
                            if (subKey == null) continue;

                            var displayName = subKey.GetValue("DisplayName")?.ToString();
                            var installLocation = subKey.GetValue("InstallLocation")?.ToString();
                            var exePath = "";

                            if (string.IsNullOrEmpty(displayName)) continue;

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
                    continue;
                }
            }
        }

        return pdfReaders;
    }

    private static string? FindExecutable(string installPath, string exeName)
    {
        try
        {
            var fullPath = Path.Combine(installPath, exeName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

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
        }
        return null;
    }
}