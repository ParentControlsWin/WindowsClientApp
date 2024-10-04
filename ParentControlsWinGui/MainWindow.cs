using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ParentControlsWinGui
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeNotificationIconComponent();
        }

        private void MainWindow_Load(object? sender, EventArgs e)
        {
            notifyIcon1.Visible = true; // Show the NotifyIcon
            this.Resize += new EventHandler(MainWindow_Resize); // Add Resize event handler
            this.FormClosing += new FormClosingEventHandler(MainWindow_FormClosing); // Add FormClosing event handler

            this.logTextBox.Text = $"Thank you for using ParentControls.Win{Environment.NewLine}{Environment.NewLine}If you're having trouble connecting to the internet, try clicking reconnect.{Environment.NewLine}{Environment.NewLine}If you have questions or concerns about the product, don't be afraid to contact us through email at contact@parentontrols.win";
        }

        private void MainWindow_Resize(object? sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                MinimizeToTray();
            }
        }

        private void MainWindow_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // Cancel the close event
                MinimizeToTray();
            }
        }

        private void MinimizeToTray()
        {
            this.Hide(); // Hide the form
            notifyIcon1.BalloonTipTitle = "ParentControls.Win";
            notifyIcon1.BalloonTipText = "Application minimized to tray.";
            notifyIcon1.ShowBalloonTip(3000); // Show for 3 seconds
        }

        private void notifyIcon1_DoubleClick(object? sender, EventArgs e)
        {
            this.Show(); // Show the form when the NotifyIcon is double-clicked
            this.WindowState = FormWindowState.Normal; // Restore the form
        }

        private void InitializeNotificationIconComponent()
        {
            //this.components = new System.ComponentModel.Container();
            //this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            //this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            notifyIcon1.DoubleClick += new System.EventHandler(notifyIcon1_DoubleClick);
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Name = "Main Window";
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.ResumeLayout(false);

        }

        private async void reconnectButton_Click(object sender, EventArgs e)
        {
            reconnectButton.Enabled = false;
            reconnectButton.Text = "Connecting with server, please wait...";

            await Task.Run(() => LoginManager.StartWireguardServer());


            reconnectButton.Text = "Reconnect";
            reconnectButton.Enabled = true;
        }

        private async void certificateButton_Click(object sender, EventArgs e)
        {
            certificateButton.Enabled = false;
            certificateButton.Text = "Saving info...";

            await Task.Run(() => LoginManager.SaveLocalCertificates());

            certificateButton.Text = "Save SSL/TLS Certificate";
            certificateButton.Enabled = true;
        }
    }
}
