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
            groupBox1 = new GroupBox();
            dgvSearchResult = new DataGridView();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSearchResult).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Top;
            button1.Location = new Point(569, 12);
            button1.Name = "button1";
            button1.Size = new Size(94, 29);
            button1.TabIndex = 0;
            button1.Text = "LoadPDF";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // BtnSearch
            // 
            BtnSearch.Anchor = AnchorStyles.Top;
            BtnSearch.Location = new Point(329, 22);
            BtnSearch.Name = "BtnSearch";
            BtnSearch.Size = new Size(94, 29);
            BtnSearch.TabIndex = 1;
            BtnSearch.Text = "Search";
            BtnSearch.UseVisualStyleBackColor = true;
            BtnSearch.Click += BtnSearch_Click;
            // 
            // TxtSearch
            // 
            TxtSearch.Location = new Point(13, 23);
            TxtSearch.Name = "TxtSearch";
            TxtSearch.PlaceholderText = "Enter term to search...";
            TxtSearch.Size = new Size(301, 27);
            TxtSearch.TabIndex = 2;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top;
            groupBox1.BackColor = SystemColors.Control;
            groupBox1.Controls.Add(TxtSearch);
            groupBox1.Controls.Add(BtnSearch);
            groupBox1.FlatStyle = FlatStyle.System;
            groupBox1.Location = new Point(374, 44);
            groupBox1.Margin = new Padding(0);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(0);
            groupBox1.Size = new Size(434, 64);
            groupBox1.TabIndex = 3;
            groupBox1.TabStop = false;
            groupBox1.Text = " ";
            // 
            // dgvSearchResult
            // 
            dgvSearchResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvSearchResult.BackgroundColor = SystemColors.Control;
            dgvSearchResult.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSearchResult.GridColor = SystemColors.Control;
            dgvSearchResult.Location = new Point(72, 128);
            dgvSearchResult.Name = "dgvSearchResult";
            dgvSearchResult.RowHeadersWidth = 51;
            dgvSearchResult.Size = new Size(1082, 413);
            dgvSearchResult.TabIndex = 4;
            dgvSearchResult.DoubleClick += dgvSearchResult_DoubleClick;
            // 
            // Search
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1230, 553);
            Controls.Add(dgvSearchResult);
            Controls.Add(groupBox1);
            Controls.Add(button1);
            Name = "Search";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SearchForm";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSearchResult).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button button1;
        private Button BtnSearch;
        private TextBox TxtSearch;
        private GroupBox groupBox1;
        private DataGridView dgvSearchResult;
    }
}