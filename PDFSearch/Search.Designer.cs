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
            groupBox1 = new System.Windows.Forms.GroupBox();
            dgvSearchResult = new System.Windows.Forms.DataGridView();
            Btn_Clean = new System.Windows.Forms.Button();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSearchResult).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            button1.Location = new System.Drawing.Point(471, 12);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(94, 29);
            button1.TabIndex = 0;
            button1.Text = "LoadPDF";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // BtnSearch
            // 
            BtnSearch.Anchor = System.Windows.Forms.AnchorStyles.Top;
            BtnSearch.Location = new System.Drawing.Point(329, 22);
            BtnSearch.Name = "BtnSearch";
            BtnSearch.Size = new System.Drawing.Size(94, 29);
            BtnSearch.TabIndex = 1;
            BtnSearch.Text = "Search";
            BtnSearch.UseVisualStyleBackColor = true;
            BtnSearch.Click += BtnSearch_Click;
            // 
            // TxtSearch
            // 
            TxtSearch.Location = new System.Drawing.Point(13, 23);
            TxtSearch.Name = "TxtSearch";
            TxtSearch.PlaceholderText = "Enter term to search...";
            TxtSearch.Size = new System.Drawing.Size(301, 27);
            TxtSearch.TabIndex = 2;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            groupBox1.BackColor = System.Drawing.SystemColors.Control;
            groupBox1.Controls.Add(TxtSearch);
            groupBox1.Controls.Add(BtnSearch);
            groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            groupBox1.Location = new System.Drawing.Point(374, 44);
            groupBox1.Margin = new System.Windows.Forms.Padding(0);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(0);
            groupBox1.Size = new System.Drawing.Size(434, 64);
            groupBox1.TabIndex = 3;
            groupBox1.TabStop = false;
            groupBox1.Text = " ";
            // 
            // dgvSearchResult
            // 
            dgvSearchResult.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
            dgvSearchResult.BackgroundColor = System.Drawing.SystemColors.Control;
            dgvSearchResult.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSearchResult.GridColor = System.Drawing.SystemColors.Control;
            dgvSearchResult.Location = new System.Drawing.Point(72, 128);
            dgvSearchResult.Name = "dgvSearchResult";
            dgvSearchResult.RowHeadersWidth = 51;
            dgvSearchResult.Size = new System.Drawing.Size(1082, 413);
            dgvSearchResult.TabIndex = 4;
            dgvSearchResult.DoubleClick += dgvSearchResult_DoubleClick;
            // 
            // Btn_Clean
            // 
            Btn_Clean.Anchor = System.Windows.Forms.AnchorStyles.Top;
            Btn_Clean.Location = new System.Drawing.Point(623, 12);
            Btn_Clean.Name = "Btn_Clean";
            Btn_Clean.Size = new System.Drawing.Size(94, 29);
            Btn_Clean.TabIndex = 5;
            Btn_Clean.Text = "Clean Indexing";
            Btn_Clean.UseVisualStyleBackColor = true;
            Btn_Clean.Click += Btn_Clean_Click;
            // 
            // Search
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1230, 553);
            Controls.Add(Btn_Clean);
            Controls.Add(dgvSearchResult);
            Controls.Add(groupBox1);
            Controls.Add(button1);
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "SearchForm";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSearchResult).EndInit();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Button Btn_Clean;

        #endregion

        private System.Windows.Forms.Button button1;
        private Button BtnSearch;
        private TextBox TxtSearch;
        private GroupBox groupBox1;
        private DataGridView dgvSearchResult;
    }
}