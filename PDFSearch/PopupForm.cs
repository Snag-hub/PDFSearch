using PDFSearch.Acrobat;
using PDFSearch.Utilities;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDFSearch
{
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
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Regular)
            };
            overlayPanel.Controls.Add(loadingLabel);

            instance = this;
        }
        public static PopupForm Instance => instance;

        private void BtnLaunchSearch_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                // Restore the existing PopupForm if it's minimized
                if (SearchInPDFs.Instance != null && SearchInPDFs.Instance.WindowState == FormWindowState.Minimized)
                {
                    SearchInPDFs.Instance.WindowState = FormWindowState.Normal;
                    SearchInPDFs.Instance.BringToFront();
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

        //private void PopupForm_Resize(object sender, EventArgs e)
        //{
        //    if (this.WindowState == FormWindowState.Minimized)
        //    {
        //        // Restore the existing PopupForm if it's minimized
        //        if (SearchInPDFs.Instance != null && SearchInPDFs.Instance.WindowState == FormWindowState.Minimized)
        //        {
        //            SearchInPDFs.Instance.WindowState = FormWindowState.Normal;
        //            SearchInPDFs.Instance.BringToFront();
        //        }
        //    }
        //}
    }
}
