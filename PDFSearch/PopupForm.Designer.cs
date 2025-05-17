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
            SuspendLayout();
            // 
            // BtnLaunchSearch
            // 
            BtnLaunchSearch.Location = new Point(12, 12);
            BtnLaunchSearch.Name = "BtnLaunchSearch";
            BtnLaunchSearch.Size = new Size(212, 29);
            BtnLaunchSearch.TabIndex = 0;
            BtnLaunchSearch.Text = "Launch Search Interface";
            BtnLaunchSearch.UseVisualStyleBackColor = true;
            BtnLaunchSearch.Click += BtnLaunchSearch_Click;
            // 
            // PopupForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(236, 53);
            Controls.Add(BtnLaunchSearch);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "PopupForm";
            Text = "Search App";
            TopMost = true;
            Load += PopupForm_Load;
            //Resize += PopupForm_Resize;
            ResumeLayout(false);
        }

        #endregion

        private Button BtnLaunchSearch;
    }
}