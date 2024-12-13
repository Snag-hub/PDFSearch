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
            button1.Location = new Point(112, 19);
            button1.Name = "button1";
            button1.Size = new Size(124, 29);
            button1.TabIndex = 0;
            button1.Text = "LoadPDF";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // BtnSearch
            // 
            BtnSearch.Anchor = AnchorStyles.Top;
            BtnSearch.Location = new Point(612, 59);
            BtnSearch.Name = "BtnSearch";
            BtnSearch.Size = new Size(94, 29);
            BtnSearch.TabIndex = 1;
            BtnSearch.Text = "Search";
            BtnSearch.UseVisualStyleBackColor = true;
            BtnSearch.Click += BtnSearch_Click;
            // 
            // TxtSearch
            // 
            TxtSearch.Anchor = AnchorStyles.Top;
            TxtSearch.Location = new Point(295, 60);
            TxtSearch.Name = "TxtSearch";
            TxtSearch.PlaceholderText = "Enter term to search...";
            TxtSearch.Size = new Size(301, 27);
            TxtSearch.TabIndex = 2;
            TxtSearch.KeyPress += TxtSearch_KeyPress;
            // 
            // dgvSearchResult
            // 
            dgvSearchResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvSearchResult.BackgroundColor = SystemColors.Control;
            dgvSearchResult.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSearchResult.GridColor = SystemColors.Control;
            dgvSearchResult.Location = new Point(73, 138);
            dgvSearchResult.Name = "dgvSearchResult";
            dgvSearchResult.RowHeadersWidth = 51;
            dgvSearchResult.Size = new Size(1082, 407);
            dgvSearchResult.TabIndex = 4;
            dgvSearchResult.DoubleClick += dgvSearchResult_DoubleClick;
            // 
            // statusLabel
            // 
            statusLabel.Anchor = AnchorStyles.Top;
            statusLabel.Location = new Point(295, 23);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(541, 23);
            statusLabel.TabIndex = 5;
            statusLabel.Text = "Status: ";
            // 
            // BtnClean
            // 
            BtnClean.Location = new Point(112, 59);
            BtnClean.Name = "BtnClean";
            BtnClean.Size = new Size(124, 29);
            BtnClean.TabIndex = 6;
            BtnClean.Text = "Clean Indexing";
            BtnClean.UseVisualStyleBackColor = true;
            BtnClean.Click += BtnClean_Click;
            // 
            // lblResult
            // 
            lblResult.Anchor = AnchorStyles.Top;
            lblResult.Location = new Point(295, 100);
            lblResult.Name = "lblResult";
            lblResult.Padding = new Padding(2);
            lblResult.Size = new Size(411, 23);
            lblResult.TabIndex = 7;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(statusLabel);
            groupBox1.Controls.Add(lblResult);
            groupBox1.Controls.Add(button1);
            groupBox1.Controls.Add(BtnSearch);
            groupBox1.Controls.Add(BtnClean);
            groupBox1.Controls.Add(TxtSearch);
            groupBox1.Location = new Point(197, -3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(842, 126);
            groupBox1.TabIndex = 8;
            groupBox1.TabStop = false;
            // 
            // Search
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1230, 553);
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