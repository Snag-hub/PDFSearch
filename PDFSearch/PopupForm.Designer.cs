namespace FindInPDFs
{
    partial class PopupForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PopupForm));
            BtnLaunchSearch = new Button();
            progressBarIndexing = new ProgressBar();
            statusLabel = new Label();
            btnPlayPause = new Button();
            SuspendLayout();
            // 
            // BtnLaunchSearch
            // 
            BtnLaunchSearch.Location = new Point(10, 9);
            BtnLaunchSearch.Margin = new Padding(3, 2, 3, 2);
            BtnLaunchSearch.Name = "BtnLaunchSearch";
            BtnLaunchSearch.Size = new Size(212, 22);
            BtnLaunchSearch.TabIndex = 0;
            BtnLaunchSearch.Text = "Launch Search Interface";
            BtnLaunchSearch.UseVisualStyleBackColor = true;
            BtnLaunchSearch.Click += BtnLaunchSearch_Click;
            // 
            // progressBarIndexing
            // 
            progressBarIndexing.ForeColor = Color.SpringGreen;
            progressBarIndexing.Location = new Point(10, 22);
            progressBarIndexing.Name = "progressBarIndexing";
            progressBarIndexing.Size = new Size(212, 23);
            progressBarIndexing.TabIndex = 1;
            progressBarIndexing.Visible = false;
            // 
            // statusLabel
            // 
            statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            statusLabel.AutoSize = true;
            statusLabel.Font = new Font("Arial Nova Cond", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            statusLabel.Location = new Point(12, 54);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(34, 14);
            statusLabel.TabIndex = 2;
            statusLabel.Text = "Ready";
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnPlayPause
            // 
            btnPlayPause.Location = new Point(12, 85);
            btnPlayPause.Name = "btnPlayPause";
            btnPlayPause.Size = new Size(210, 23);
            btnPlayPause.TabIndex = 3;
            btnPlayPause.Text = "Pause Indexing...";
            btnPlayPause.UseVisualStyleBackColor = true;
            btnPlayPause.Click += btnPlayPause_Click;
            // 
            // PopupForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(234, 120);
            Controls.Add(btnPlayPause);
            Controls.Add(statusLabel);
            Controls.Add(progressBarIndexing);
            Controls.Add(BtnLaunchSearch);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            Name = "PopupForm";
            Text = "Search App";
            TopMost = true;
            Load += PopupForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button BtnLaunchSearch;
        private ProgressBar progressBarIndexing;
        private Label statusLabel;
        private Button btnPlayPause;
    }
}