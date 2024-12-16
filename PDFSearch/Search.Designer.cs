namespace PDFSearch
{
    partial class Search
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
            button1 = new Button();
            BtnSearch = new Button();
            TxtSearch = new TextBox();
            dgvSearchResult = new DataGridView();
            statusLabel = new Label();
            BtnClean = new Button();
            lblResult = new Label();
            groupBox1 = new GroupBox();
            ((System.ComponentModel.ISupportInitialize)dgvSearchResult).BeginInit();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(19, 18);
            button1.Name = "button1";
            button1.Size = new Size(124, 29);
            button1.TabIndex = 11;
            button1.Text = "LoadPDF";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // BtnSearch
            // 
            BtnSearch.Anchor = AnchorStyles.Top;
            BtnSearch.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            BtnSearch.Location = new Point(407, 60);
            BtnSearch.Name = "BtnSearch";
            BtnSearch.Padding = new Padding(1);
            BtnSearch.Size = new Size(180, 35);
            BtnSearch.TabIndex = 2;
            BtnSearch.Text = "Search";
            BtnSearch.UseVisualStyleBackColor = true;
            BtnSearch.Click += BtnSearch_Click;
            // 
            // TxtSearch
            // 
            TxtSearch.Anchor = AnchorStyles.Top;
            TxtSearch.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            TxtSearch.Location = new Point(19, 61);
            TxtSearch.Name = "TxtSearch";
            TxtSearch.PlaceholderText = "Enter term to search...";
            TxtSearch.Size = new Size(371, 34);
            TxtSearch.TabIndex = 1;
            TxtSearch.KeyPress += TxtSearch_KeyPress;
            // 
            // dgvSearchResult
            // 
            dgvSearchResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvSearchResult.BackgroundColor = SystemColors.Control;
            dgvSearchResult.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSearchResult.GridColor = SystemColors.Control;
            dgvSearchResult.Location = new Point(12, 138);
            dgvSearchResult.Name = "dgvSearchResult";
            dgvSearchResult.RowHeadersWidth = 51;
            dgvSearchResult.Size = new Size(968, 476);
            dgvSearchResult.TabIndex = 3;
            dgvSearchResult.DoubleClick += dgvSearchResult_DoubleClick;
            // 
            // statusLabel
            // 
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            statusLabel.Location = new Point(197, 617);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(604, 41);
            statusLabel.TabIndex = 5;
            statusLabel.Text = "Status: ";
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // BtnClean
            // 
            BtnClean.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            BtnClean.Location = new Point(463, 18);
            BtnClean.Name = "BtnClean";
            BtnClean.Size = new Size(124, 29);
            BtnClean.TabIndex = 10;
            BtnClean.Text = "Clean Indexing";
            BtnClean.UseVisualStyleBackColor = true;
            BtnClean.Click += BtnClean_Click;
            // 
            // lblResult
            // 
            lblResult.Anchor = AnchorStyles.Top;
            lblResult.ForeColor = Color.DarkOliveGreen;
            lblResult.Location = new Point(84, 96);
            lblResult.Name = "lblResult";
            lblResult.Padding = new Padding(2);
            lblResult.Size = new Size(411, 23);
            lblResult.TabIndex = 7;
            lblResult.Text = " ";
            lblResult.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(lblResult);
            groupBox1.Controls.Add(button1);
            groupBox1.Controls.Add(BtnSearch);
            groupBox1.Controls.Add(BtnClean);
            groupBox1.Controls.Add(TxtSearch);
            groupBox1.Location = new Point(197, -3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(604, 126);
            groupBox1.TabIndex = 8;
            groupBox1.TabStop = false;
            // 
            // Search
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(992, 663);
            Controls.Add(statusLabel);
            Controls.Add(groupBox1);
            Controls.Add(dgvSearchResult);
            Name = "Search";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SearchForm";
            ((System.ComponentModel.ISupportInitialize)dgvSearchResult).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        private System.Windows.Forms.GroupBox groupBox1;

        private System.Windows.Forms.Label lblResult;

        private System.Windows.Forms.Button BtnClean;

        private System.Windows.Forms.Label statusLabel;

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button BtnSearch;
        private System.Windows.Forms.TextBox TxtSearch;
        private System.Windows.Forms.DataGridView dgvSearchResult;
    }
}