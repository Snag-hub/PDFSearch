using FindInPDFs.Acrobat;
using FindInPDFs.Utilities;
using Microsoft.Win32;
using PDFSearch;
using PDFSearch.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
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

    private const int FHeight = 120;

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
            if (!ShowFirstTimeSetup())
            {
                AcrobatWindowManager.EnsureAcrobatClosed();
                this.Close();
                return;
            }
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
            statusLabel.Text = "Indexing cancelled. \nRestart the application to resume.";
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
                    if (control != progressBarIndexing && control != btnPlayPause)
                        control.Visible = true;

                    // adjust the height of the popform after indexing complete.
                    this.Size = new Size(250, FHeight);
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
                statusLabel.Text = "Indexing cancelled. \nRestart the application to resume.";
                progressBarIndexing.Visible = false;
                btnPlayPause.Visible = false;
                foreach (Control control in this.Controls)
                {
                    if (control != progressBarIndexing && control != btnPlayPause)
                        control.Visible = true;

                    // adjust the height of the popform after indexing pause.
                    this.Size = new Size(250, FHeight);
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
                    if (control != progressBarIndexing && control != btnPlayPause)
                        control.Visible = true;

                    // adjust the height of the popform after indexing error.
                    this.Size = new Size(250, FHeight);
                }
                MessageBox.Show($"There is some error while Indexing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"[ERROR] Indexing failed: {ex.Message}");
            }));
        }
    }

    private bool ShowFirstTimeSetup()
    {
        this.WindowState = FormWindowState.Normal;
        OpenFileDialog openFileDialog = new()
        {
            Filter = "PDF Files|*.pdf",
            Title = "Select the Start/Landing Page File (index.pdf)"
        };
        if (openFileDialog.ShowDialog() != DialogResult.OK)
        {
            MessageBox.Show("Setup cancelled. Application will close.", "Setup Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Console.WriteLine("[DEBUG] OpenFileDialog cancelled, closing application");
            return false;
        }
        string startFile = openFileDialog.FileName;
        if (!startFile.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase) || !File.Exists(startFile))
        {
            MessageBox.Show("Selected start file must be within the dataset folder and accessible.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine("[DEBUG] Invalid start file, closing application");
            return false;
        }
        var installedReaders = GetInstalledPDFReaders();
        if (installedReaders.Count == 0)
        {
            MessageBox.Show("No PDF readers found on the system. Please install a PDF reader and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine("[DEBUG] No PDF readers found, closing application");
            return false;
        }
        int radioButtonHeight = 30;
        int formHeight = 100 + (installedReaders.Count * radioButtonHeight) + 60;
        Form selectionForm = new()
        {
            Text = "Select PDF Reader",
            Size = new System.Drawing.Size(400, Math.Min(formHeight, 600)),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            WindowState = FormWindowState.Normal
        };
        selectionForm.Load += (s, e) =>
        {
            selectionForm.WindowState = FormWindowState.Normal;
            selectionForm.Activate();
            Console.WriteLine($"[DEBUG] selectionForm WindowState: {selectionForm.WindowState}");
        };
        Label label = new()
        {
            Text = "Select your preferred PDF reader:",
            Location = new System.Drawing.Point(20, 20),
            AutoSize = true
        };
        selectionForm.Controls.Add(label);
        int yOffset = 50;
        bool firstRadioButton = true;
        foreach (var reader in installedReaders)
        {
            RadioButton radioButton = new()
            {
                Text = reader.Key,
                Location = new System.Drawing.Point(20, yOffset),
                AutoSize = true,
                Checked = firstRadioButton
            };
            selectionForm.Controls.Add(radioButton);
            yOffset += radioButtonHeight;
            firstRadioButton = false;
        }
        Button okButton = new()
        {
            Text = "OK",
            Location = new System.Drawing.Point(100, yOffset + 20),
            DialogResult = DialogResult.OK
        };
        selectionForm.Controls.Add(okButton);
        Button cancelButton = new()
        {
            Text = "Cancel",
            Location = new System.Drawing.Point(200, yOffset + 20),
            DialogResult = DialogResult.Cancel
        };
        selectionForm.Controls.Add(cancelButton);
        Console.WriteLine($"[DEBUG] Before ShowDialog, WindowState: {selectionForm.WindowState}");
        DialogResult result = selectionForm.ShowDialog();
        selectionForm.Activate();
        Console.WriteLine($"[DEBUG] After ShowDialog, DialogResult: {result}");
        if (result != DialogResult.OK)
        {
            MessageBox.Show("Setup cancelled. Application will close.", "Setup Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Console.WriteLine("[DEBUG] selectionForm cancelled, closing application");
            return false;
        }
        string? selectedReaderName = selectionForm.Controls.OfType<RadioButton>()
            .FirstOrDefault(rb => rb.Checked)?.Text;
        if (selectedReaderName == null)
        {
            MessageBox.Show("Please select a PDF reader.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine("[DEBUG] No PDF reader selected, closing application");
            return false;
        }
        string selectedReaderPath = installedReaders[selectedReaderName];
        try
        {
            ConfigManager config = new()
            {
                StartFile = startFile,
                PdfOpener = selectedReaderPath
            };
            config.SaveConfig(folderPath);
            MessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Console.WriteLine("[DEBUG] Configuration saved successfully");
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine($"[ERROR] Config save failed: {ex.Message}");
            return false;
        }
    }

    private static Dictionary<string, string> GetInstalledPDFReaders()
    {
        var pdfReaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var uniqueReaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] registryBasePaths =
        [
            @"SOFTWARE\Adobe\Acrobat Reader",
            @"SOFTWARE\Adobe\Adobe Acrobat",
            @"SOFTWARE\WOW6432Node\Adobe\Acrobat Reader",
            @"SOFTWARE\WOW6432Node\Adobe\Adobe Acrobat",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        ];
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
                            if (string.IsNullOrEmpty(displayName)) continue;
                            string exePath = null;
                            if (displayName.Contains("Adobe Acrobat", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.IsNullOrEmpty(installLocation))
                                {
                                    exePath = FindExecutable(installLocation, "Acrobat.exe") ?? FindExecutable(installLocation, "AcroRd32.exe");
                                }
                            }
                            else if (displayName.Contains("Foxit Reader", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.IsNullOrEmpty(installLocation))
                                {
                                    exePath = FindExecutable(installLocation, "FoxitReader.exe");
                                }
                            }
                            if (!string.IsNullOrEmpty(exePath))
                            {
                                string normalizedName = NormalizeReaderName(displayName, exePath);
                                if (uniqueReaders.Add(normalizedName))
                                {
                                    pdfReaders[normalizedName] = exePath;
                                    Console.WriteLine($"[DEBUG] Added reader: {normalizedName}, Path: {exePath}");
                                }
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
                                string name = basePath.Contains("Acrobat Reader")
                                    ? $"Adobe Acrobat Reader {version}"
                                    : $"Adobe Acrobat {version}";
                                string normalizedName = NormalizeReaderName(name, exePath);
                                if (uniqueReaders.Add(normalizedName))
                                {
                                    pdfReaders[normalizedName] = exePath;
                                    Console.WriteLine($"[DEBUG] Added reader: {normalizedName}, Path: {exePath}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Registry error at {basePath}: {ex.Message}");
                    continue;
                }
            }
        }
        return pdfReaders;
    }

    private static string NormalizeReaderName(string name, string exePath)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        name = name.Trim();
        if (exePath.Contains("AcroRd32.exe", StringComparison.OrdinalIgnoreCase))
        {
            return "Adobe Acrobat Reader";
        }
        else if (exePath.Contains("Acrobat.exe", StringComparison.OrdinalIgnoreCase))
        {
            if (name.Contains("Pro", StringComparison.OrdinalIgnoreCase) || name.Contains("XI", StringComparison.OrdinalIgnoreCase))
            {
                return "Adobe Acrobat Pro 11";
            }
            return "Adobe Acrobat";
        }
        else if (name.Contains("Foxit Reader", StringComparison.OrdinalIgnoreCase))
        {
            return "Foxit Reader";
        }
        return name;
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

    private void PopupForm_FormClosed(object sender, FormClosedEventArgs e) => AcrobatWindowManager.EnsureAcrobatClosed();
}