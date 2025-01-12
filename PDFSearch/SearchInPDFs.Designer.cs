namespace PDFSearch
{
    partial class SearchInPDFs
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SearchInPDFs));
            txtSearchBox = new TextBox();
            chkWholeWord = new CheckBox();
            chkCaseSensitive = new CheckBox();
            BtnSearchText = new Button();
            toolTip1 = new ToolTip(components);
            statusLabel = new Label();
            grpBoxSearch = new GroupBox();
            label1 = new Label();
            TvSearchRange = new TreeView();
            grpBoxSearchResult = new GroupBox();
            dataGridViewResults = new DataGridView();
            BtnRetSearch = new Button();
            grpBoxSearch.SuspendLayout();
            grpBoxSearchResult.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewResults).BeginInit();
            SuspendLayout();
            // 
            // txtSearchBox
            // 
            txtSearchBox.AcceptsReturn = true;
            txtSearchBox.AcceptsTab = true;
            txtSearchBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSearchBox.BackColor = SystemColors.Control;
            txtSearchBox.BorderStyle = BorderStyle.FixedSingle;
            txtSearchBox.Cursor = Cursors.Hand;
            txtSearchBox.Font = new Font("0xProto Nerd Font", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtSearchBox.Location = new Point(9, 51);
            txtSearchBox.Margin = new Padding(6);
            txtSearchBox.Name = "txtSearchBox";
            txtSearchBox.PlaceholderText = "Enter text to search";
            txtSearchBox.RightToLeft = RightToLeft.No;
            txtSearchBox.Size = new Size(393, 25);
            txtSearchBox.TabIndex = 0;
            // 
            // chkWholeWord
            // 
            chkWholeWord.AutoSize = true;
            chkWholeWord.Font = new Font("0xProto Nerd Font", 7.79999971F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkWholeWord.Location = new Point(9, 91);
            chkWholeWord.Name = "chkWholeWord";
            chkWholeWord.Size = new Size(149, 19);
            chkWholeWord.TabIndex = 1;
            chkWholeWord.Text = "Whole word only";
            chkWholeWord.UseVisualStyleBackColor = true;
            // 
            // chkCaseSensitive
            // 
            chkCaseSensitive.AutoSize = true;
            chkCaseSensitive.Font = new Font("0xProto Nerd Font", 7.79999971F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkCaseSensitive.Location = new Point(9, 120);
            chkCaseSensitive.Name = "chkCaseSensitive";
            chkCaseSensitive.Size = new Size(141, 19);
            chkCaseSensitive.TabIndex = 2;
            chkCaseSensitive.Text = "Case-sensitive";
            chkCaseSensitive.UseVisualStyleBackColor = true;
            // 
            // BtnSearchText
            // 
            BtnSearchText.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            BtnSearchText.Font = new Font("0xProto Nerd Font", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BtnSearchText.Location = new Point(3, 546);
            BtnSearchText.Name = "BtnSearchText";
            BtnSearchText.Size = new Size(393, 29);
            BtnSearchText.TabIndex = 3;
            BtnSearchText.Text = "Search";
            BtnSearchText.UseVisualStyleBackColor = true;
            BtnSearchText.Click += BtnSearchText_Click;
            BtnSearchText.KeyPress += BtnSearchText_KeyPress;
            // 
            // statusLabel
            // 
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            statusLabel.Location = new Point(12, 665);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(411, 33);
            statusLabel.TabIndex = 6;
            statusLabel.Text = "Status: ";
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // grpBoxSearch
            // 
            grpBoxSearch.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grpBoxSearch.Controls.Add(label1);
            grpBoxSearch.Controls.Add(TvSearchRange);
            grpBoxSearch.Controls.Add(txtSearchBox);
            grpBoxSearch.Controls.Add(chkWholeWord);
            grpBoxSearch.Controls.Add(chkCaseSensitive);
            grpBoxSearch.Controls.Add(BtnSearchText);
            grpBoxSearch.Location = new Point(12, 12);
            grpBoxSearch.Name = "grpBoxSearch";
            grpBoxSearch.Size = new Size(411, 659);
            grpBoxSearch.TabIndex = 7;
            grpBoxSearch.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(9, 151);
            label1.Name = "label1";
            label1.Size = new Size(99, 20);
            label1.TabIndex = 5;
            label1.Text = "Search Range";
            // 
            // TvSearchRange
            // 
            TvSearchRange.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            TvSearchRange.CheckBoxes = true;
            TvSearchRange.Location = new Point(9, 183);
            TvSearchRange.Name = "TvSearchRange";
            TvSearchRange.Size = new Size(390, 324);
            TvSearchRange.TabIndex = 4;
            TvSearchRange.AfterCheck += TvSearchRange_AfterCheck;
            // 
            // grpBoxSearchResult
            // 
            grpBoxSearchResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grpBoxSearchResult.Controls.Add(dataGridViewResults);
            grpBoxSearchResult.Controls.Add(BtnRetSearch);
            grpBoxSearchResult.Location = new Point(12, 16);
            grpBoxSearchResult.Name = "grpBoxSearchResult";
            grpBoxSearchResult.Size = new Size(411, 679);
            grpBoxSearchResult.TabIndex = 8;
            grpBoxSearchResult.TabStop = false;
            // 
            // dataGridViewResults
            // 
            dataGridViewResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewResults.Location = new Point(9, 88);
            dataGridViewResults.Name = "dataGridViewResults";
            dataGridViewResults.RowHeadersWidth = 51;
            dataGridViewResults.Size = new Size(395, 585);
            dataGridViewResults.TabIndex = 5;
            dataGridViewResults.CellMouseDoubleClick += dataGridViewResults_CellMouseDoubleClick;
            dataGridViewResults.CellPainting += dataGridViewResults_CellPainting;
            // 
            // BtnRetSearch
            // 
            BtnRetSearch.Font = new Font("0xProto Nerd Font", 7.79999971F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BtnRetSearch.Location = new Point(6, 53);
            BtnRetSearch.Name = "BtnRetSearch";
            BtnRetSearch.Size = new Size(144, 29);
            BtnRetSearch.TabIndex = 4;
            BtnRetSearch.Text = "Return to Search";
            BtnRetSearch.UseVisualStyleBackColor = true;
            BtnRetSearch.Click += BtnRetSearch_Click;
            // 
            // SearchInPDFs
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(428, 707);
            Controls.Add(grpBoxSearchResult);
            Controls.Add(grpBoxSearch);
            Controls.Add(statusLabel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "SearchInPDFs";
            Text = "SearchInPDFs";
            FormClosing += SearchInPDFs_FormClosing;
            grpBoxSearch.ResumeLayout(false);
            grpBoxSearch.PerformLayout();
            grpBoxSearchResult.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewResults).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TextBox txtSearchBox;
        private CheckBox chkWholeWord;
        private CheckBox chkCaseSensitive;
        private Button BtnSearchText;
        private ToolTip toolTip1;
        private Label statusLabel;
        private GroupBox grpBoxSearch;
        private GroupBox grpBoxSearchResult;
        private Button BtnRetSearch;
        private Label label1;
        private TreeView TvSearchRange;
        private DataGridView dataGridViewResults;
    }
}