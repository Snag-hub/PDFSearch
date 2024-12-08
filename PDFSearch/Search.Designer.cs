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
            button1 = new System.Windows.Forms.Button();
            BtnSearch = new System.Windows.Forms.Button();
            TxtSearch = new System.Windows.Forms.TextBox();
            dgvSearchResult = new System.Windows.Forms.DataGridView();
            statusLabel = new System.Windows.Forms.Label();
            BtnClean = new System.Windows.Forms.Button();
            lblResult = new System.Windows.Forms.Label();
            groupBox1 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)dgvSearchResult).BeginInit();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(112, 19);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(124, 29);
            button1.TabIndex = 0;
            button1.Text = "LoadPDF";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // BtnSearch
            // 
            BtnSearch.Anchor = System.Windows.Forms.AnchorStyles.Top;
            BtnSearch.Location = new System.Drawing.Point(612, 59);
            BtnSearch.Name = "BtnSearch";
            BtnSearch.Size = new System.Drawing.Size(94, 29);
            BtnSearch.TabIndex = 1;
            BtnSearch.Text = "Search";
            BtnSearch.UseVisualStyleBackColor = true;
            BtnSearch.Click += BtnSearch_Click;
            // 
            // TxtSearch
            // 
            TxtSearch.Anchor = System.Windows.Forms.AnchorStyles.Top;
            TxtSearch.Location = new System.Drawing.Point(295, 60);
            TxtSearch.Name = "TxtSearch";
            TxtSearch.PlaceholderText = "Enter term to search...";
            TxtSearch.Size = new System.Drawing.Size(301, 27);
            TxtSearch.TabIndex = 2;
            TxtSearch.KeyPress += TxtSearch_KeyPress;
            // 
            // dgvSearchResult
            // 
            dgvSearchResult.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
            dgvSearchResult.BackgroundColor = System.Drawing.SystemColors.Control;
            dgvSearchResult.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSearchResult.GridColor = System.Drawing.SystemColors.Control;
            dgvSearchResult.Location = new System.Drawing.Point(73, 138);
            dgvSearchResult.Name = "dgvSearchResult";
            dgvSearchResult.RowHeadersWidth = 51;
            dgvSearchResult.Size = new System.Drawing.Size(1082, 407);
            dgvSearchResult.TabIndex = 4;
            dgvSearchResult.DoubleClick += dgvSearchResult_DoubleClick;
            // 
            // statusLabel
            // 
            statusLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            statusLabel.Location = new System.Drawing.Point(295, 23);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new System.Drawing.Size(411, 23);
            statusLabel.TabIndex = 5;
            statusLabel.Text = "Status: ";
            // 
            // BtnClean
            // 
            BtnClean.Location = new System.Drawing.Point(112, 59);
            BtnClean.Name = "BtnClean";
            BtnClean.Size = new System.Drawing.Size(124, 29);
            BtnClean.TabIndex = 6;
            BtnClean.Text = "Clean Indexing";
            BtnClean.UseVisualStyleBackColor = true;
            BtnClean.Click += BtnClean_Click;
            // 
            // lblResult
            // 
            lblResult.Anchor = System.Windows.Forms.AnchorStyles.Top;
            lblResult.Location = new System.Drawing.Point(295, 100);
            lblResult.Name = "lblResult";
            lblResult.Padding = new System.Windows.Forms.Padding(2);
            lblResult.Size = new System.Drawing.Size(411, 23);
            lblResult.TabIndex = 7;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
            groupBox1.Controls.Add(statusLabel);
            groupBox1.Controls.Add(lblResult);
            groupBox1.Controls.Add(button1);
            groupBox1.Controls.Add(BtnSearch);
            groupBox1.Controls.Add(BtnClean);
            groupBox1.Controls.Add(TxtSearch);
            groupBox1.Location = new System.Drawing.Point(197, -3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(842, 126);
            groupBox1.TabIndex = 8;
            groupBox1.TabStop = false;
            // 
            // Search
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1230, 553);
            Controls.Add(groupBox1);
            Controls.Add(dgvSearchResult);
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
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