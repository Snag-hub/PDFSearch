namespace FindInPDFs
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
            txtSearchBox.Location = new Point(8, 38);
            txtSearchBox.Margin = new Padding(5, 4, 5, 4);
            txtSearchBox.Name = "txtSearchBox";
            txtSearchBox.PlaceholderText = "Enter text to search";
            txtSearchBox.RightToLeft = RightToLeft.No;
            txtSearchBox.Size = new Size(344, 22);
            txtSearchBox.TabIndex = 0;
            // 
            // chkWholeWord
            // 
            chkWholeWord.AutoSize = true;
            chkWholeWord.FlatAppearance.BorderSize = 0;
            chkWholeWord.Font = new Font("0xProto Nerd Font", 7.79999971F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkWholeWord.Location = new Point(8, 68);
            chkWholeWord.Margin = new Padding(3, 2, 3, 2);
            chkWholeWord.Name = "chkWholeWord";
            chkWholeWord.Size = new Size(131, 17);
            chkWholeWord.TabIndex = 1;
            chkWholeWord.Text = "Whole word only";
            chkWholeWord.UseVisualStyleBackColor = true;
            // 
            // chkCaseSensitive
            // 
            chkCaseSensitive.AutoSize = true;
            chkCaseSensitive.FlatAppearance.BorderSize = 0;
            chkCaseSensitive.Font = new Font("0xProto Nerd Font", 7.79999971F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkCaseSensitive.Location = new Point(8, 90);
            chkCaseSensitive.Margin = new Padding(3, 2, 3, 2);
            chkCaseSensitive.Name = "chkCaseSensitive";
            chkCaseSensitive.Size = new Size(124, 17);
            chkCaseSensitive.TabIndex = 2;
            chkCaseSensitive.Text = "Case-sensitive";
            chkCaseSensitive.UseVisualStyleBackColor = true;
            // 
            // BtnSearchText
            // 
            BtnSearchText.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            BtnSearchText.Font = new Font("0xProto Nerd Font", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BtnSearchText.Location = new Point(3, 410);
            BtnSearchText.Margin = new Padding(3, 2, 3, 2);
            BtnSearchText.Name = "BtnSearchText";
            BtnSearchText.Size = new Size(344, 22);
            BtnSearchText.TabIndex = 3;
            BtnSearchText.Text = "Search";
            BtnSearchText.UseVisualStyleBackColor = true;
            BtnSearchText.Click += BtnSearchText_Click;
            BtnSearchText.KeyPress += BtnSearchText_KeyPress;
            // 
            // statusLabel
            // 
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            statusLabel.Location = new Point(10, 499);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(360, 25);
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
            grpBoxSearch.Location = new Point(10, 9);
            grpBoxSearch.Margin = new Padding(3, 2, 3, 2);
            grpBoxSearch.Name = "grpBoxSearch";
            grpBoxSearch.Padding = new Padding(3, 2, 3, 2);
            grpBoxSearch.Size = new Size(360, 494);
            grpBoxSearch.TabIndex = 7;
            grpBoxSearch.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(8, 113);
            label1.Name = "label1";
            label1.Size = new Size(78, 15);
            label1.TabIndex = 5;
            label1.Text = "Search Range";
            // 
            // TvSearchRange
            // 
            TvSearchRange.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            TvSearchRange.CheckBoxes = true;
            TvSearchRange.Location = new Point(8, 137);
            TvSearchRange.Margin = new Padding(3, 2, 3, 2);
            TvSearchRange.Name = "TvSearchRange";
            TvSearchRange.Size = new Size(342, 244);
            TvSearchRange.TabIndex = 4;
            TvSearchRange.AfterCheck += TvSearchRange_AfterCheck;
            // 
            // grpBoxSearchResult
            // 
            grpBoxSearchResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grpBoxSearchResult.Controls.Add(dataGridViewResults);
            grpBoxSearchResult.Controls.Add(BtnRetSearch);
            grpBoxSearchResult.Location = new Point(5, 187);
            grpBoxSearchResult.Margin = new Padding(3, 2, 3, 2);
            grpBoxSearchResult.Name = "grpBoxSearchResult";
            grpBoxSearchResult.Padding = new Padding(3, 2, 3, 2);
            grpBoxSearchResult.Size = new Size(360, 334);
            grpBoxSearchResult.TabIndex = 8;
            grpBoxSearchResult.TabStop = false;
            // 
            // dataGridViewResults
            // 
            dataGridViewResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewResults.Cursor = Cursors.Hand;
            dataGridViewResults.Location = new Point(8, 66);
            dataGridViewResults.Margin = new Padding(3, 2, 3, 2);
            dataGridViewResults.MultiSelect = false;
            dataGridViewResults.Name = "dataGridViewResults";
            dataGridViewResults.RowHeadersWidth = 51;
            dataGridViewResults.Size = new Size(346, 264);
            dataGridViewResults.TabIndex = 5;
            dataGridViewResults.CellMouseDoubleClick += dataGridViewResults_CellMouseDoubleClick;
            dataGridViewResults.CellPainting += dataGridViewResults_CellPainting;
            // 
            // BtnRetSearch
            // 
            BtnRetSearch.Font = new Font("0xProto Nerd Font", 7.79999971F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BtnRetSearch.Location = new Point(5, 40);
            BtnRetSearch.Margin = new Padding(3, 2, 3, 2);
            BtnRetSearch.Name = "BtnRetSearch";
            BtnRetSearch.Size = new Size(126, 22);
            BtnRetSearch.TabIndex = 4;
            BtnRetSearch.Text = "Return to Search";
            BtnRetSearch.UseVisualStyleBackColor = true;
            BtnRetSearch.Click += BtnRetSearch_Click;
            // 
            // SearchInPDFs
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(374, 530);
            Controls.Add(grpBoxSearchResult);
            Controls.Add(grpBoxSearch);
            Controls.Add(statusLabel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            Name = "SearchInPDFs";
            Text = "SearchInPDFs";
            FormClosing += SearchInPDFs_FormClosing;
            Resize += SearchInPDFs_Resize;
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