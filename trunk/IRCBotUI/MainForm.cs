using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace irc_bot_v2._0
{
    internal partial class MainForm : Form
    {
        public IRCBot IrcBot
        {
            get;
            set;
        }

        public MainForm()
        {
            InitializeComponent();
        }

        internal void AddOutput(string output)
        {
            Action a = () =>
            {
                this.tbLogWindow.Text += output + Environment.NewLine;
                this.tbLogWindow.SelectionStart = this.tbLogWindow.Text.Length;
                this.tbLogWindow.ScrollToCaret();
            };

            if (this.tbLogWindow.InvokeRequired)
            {
                this.tbLogWindow.Invoke(a);
            }
            else
            {
                a();
            }
        }

        private void miSendCMD_Click(object sender, EventArgs e)
        {
            if (this.IrcBot != null)
            {
                this.IrcBot.HandleUserInput(this.tbInput.Text);
            }
        }

        private void miQuit_Click(object sender, EventArgs e)
        {
            if (this.IrcBot != null)
            {
                this.IrcBot.Quit();
            }
            Application.Exit();
        }
    }
}