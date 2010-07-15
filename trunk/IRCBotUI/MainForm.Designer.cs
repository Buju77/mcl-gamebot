namespace irc_bot_v2._0
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MainMenu mainMenu1;

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
            this.mainMenu1 = new System.Windows.Forms.MainMenu();
            this.miQuit = new System.Windows.Forms.MenuItem();
            this.miSendCMD = new System.Windows.Forms.MenuItem();
            this.tbLogWindow = new System.Windows.Forms.TextBox();
            this.tbInput = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.Add(this.miQuit);
            this.mainMenu1.MenuItems.Add(this.miSendCMD);
            // 
            // miQuit
            // 
            this.miQuit.Text = "Quit";
            this.miQuit.Click += new System.EventHandler(this.miQuit_Click);
            // 
            // miSendCMD
            // 
            this.miSendCMD.Text = "SendCMD";
            this.miSendCMD.Click += new System.EventHandler(this.miSendCMD_Click);
            // 
            // tbLogWindow
            // 
            this.tbLogWindow.BackColor = System.Drawing.Color.Black;
            this.tbLogWindow.Font = new System.Drawing.Font("Courier New", 7F, System.Drawing.FontStyle.Regular);
            this.tbLogWindow.ForeColor = System.Drawing.Color.Gainsboro;
            this.tbLogWindow.Location = new System.Drawing.Point(3, 3);
            this.tbLogWindow.Multiline = true;
            this.tbLogWindow.Name = "tbLogWindow";
            this.tbLogWindow.ReadOnly = true;
            this.tbLogWindow.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbLogWindow.Size = new System.Drawing.Size(234, 235);
            this.tbLogWindow.TabIndex = 0;
            this.tbLogWindow.WordWrap = false;
            // 
            // tbInput
            // 
            this.tbInput.BackColor = System.Drawing.Color.Black;
            this.tbInput.Font = new System.Drawing.Font("Courier New", 8F, System.Drawing.FontStyle.Regular);
            this.tbInput.ForeColor = System.Drawing.Color.Gainsboro;
            this.tbInput.Location = new System.Drawing.Point(3, 244);
            this.tbInput.Name = "tbInput";
            this.tbInput.Size = new System.Drawing.Size(234, 19);
            this.tbInput.TabIndex = 1;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(240, 268);
            this.Controls.Add(this.tbInput);
            this.Controls.Add(this.tbLogWindow);
            this.Menu = this.mainMenu1;
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox tbLogWindow;
        private System.Windows.Forms.MenuItem miQuit;
        private System.Windows.Forms.MenuItem miSendCMD;
        private System.Windows.Forms.TextBox tbInput;
    }
}