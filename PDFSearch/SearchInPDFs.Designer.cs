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
            TreeNode treeNode1 = new TreeNode("Result Item");
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SearchInPDFs));
            txtSearchBox = new TextBox();
            chkWholeWord = new CheckBox();
            chkCaseSensitive = new CheckBox();
            BtnSearchText = new Button();
            toolTip1 = new ToolTip(components);
            treeVwResult = new TreeView();
            statusLabel = new Label();
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
            txtSearchBox.Location = new Point(12, 12);
            txtSearchBox.Margin = new Padding(6);
            txtSearchBox.Name = "txtSearchBox";
            txtSearchBox.PlaceholderText = "Enter text to search";
            txtSearchBox.RightToLeft = RightToLeft.No;
            txtSearchBox.Size = new Size(379, 25);
            txtSearchBox.TabIndex = 0;
            // 
            // chkWholeWord
            // 
            chkWholeWord.AutoSize = true;
            chkWholeWord.Font = new Font("0xProto Nerd Font", 7.79999971F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkWholeWord.Location = new Point(12, 52);
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
            chkCaseSensitive.Location = new Point(12, 81);
            chkCaseSensitive.Name = "chkCaseSensitive";
            chkCaseSensitive.Size = new Size(141, 19);
            chkCaseSensitive.TabIndex = 2;
            chkCaseSensitive.Text = "Case-sensitive";
            chkCaseSensitive.UseVisualStyleBackColor = true;
            // 
            // BtnSearchText
            // 
            BtnSearchText.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            BtnSearchText.Font = new Font("0xProto Nerd Font", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BtnSearchText.Location = new Point(297, 99);
            BtnSearchText.Name = "BtnSearchText";
            BtnSearchText.Size = new Size(94, 29);
            BtnSearchText.TabIndex = 3;
            BtnSearchText.Text = "Search";
            BtnSearchText.UseVisualStyleBackColor = true;
            BtnSearchText.Click += BtnSearchText_Click;
            BtnSearchText.KeyPress += BtnSearchText_KeyPress;
            // 
            // treeVwResult
            // 
            treeVwResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            treeVwResult.Location = new Point(12, 136);
            treeVwResult.Name = "treeVwResult";
            treeNode1.Name = "Node0";
            treeNode1.Text = "Result Item";
            treeVwResult.Nodes.AddRange(new TreeNode[] { treeNode1 });
            treeVwResult.Size = new Size(379, 481);
            treeVwResult.TabIndex = 4;
            treeVwResult.DrawNode += treeVwResult_DrawNode;
            treeVwResult.NodeMouseDoubleClick += treeVwResult_NodeMouseDoubleClick;
            // 
            // statusLabel
            // 
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            statusLabel.Location = new Point(12, 620);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(379, 33);
            statusLabel.TabIndex = 6;
            statusLabel.Text = "Status: ";
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // SearchInPDFs
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(403, 662);
            Controls.Add(statusLabel);
            Controls.Add(treeVwResult);
            Controls.Add(BtnSearchText);
            Controls.Add(chkCaseSensitive);
            Controls.Add(chkWholeWord);
            Controls.Add(txtSearchBox);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "SearchInPDFs";
            Text = "SearchInPDFs";
            FormClosing += SearchInPDFs_FormClosing;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtSearchBox;
        private CheckBox chkWholeWord;
        private CheckBox chkCaseSensitive;
        private Button BtnSearchText;
        private ToolTip toolTip1;
        private TreeView treeVwResult;
        private Label statusLabel;
    }
}