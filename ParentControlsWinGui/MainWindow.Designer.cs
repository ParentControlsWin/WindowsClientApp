namespace ParentControlsWinGui
{
    partial class MainWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            reconnectButton = new Button();
            logTextBox = new TextBox();
            certificateButton = new Button();
            notifyIcon1 = new NotifyIcon(components);
            SuspendLayout();
            // 
            // reconnectButton
            // 
            reconnectButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            reconnectButton.Location = new Point(12, 12);
            reconnectButton.Name = "reconnectButton";
            reconnectButton.Size = new Size(776, 28);
            reconnectButton.TabIndex = 0;
            reconnectButton.Text = "Reconnect";
            reconnectButton.UseVisualStyleBackColor = true;
            reconnectButton.Click += reconnectButton_Click;
            // 
            // logTextBox
            // 
            logTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            logTextBox.Location = new Point(12, 46);
            logTextBox.Multiline = true;
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.Size = new Size(776, 358);
            logTextBox.TabIndex = 1;
            // 
            // certificateButton
            // 
            certificateButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            certificateButton.Location = new Point(12, 410);
            certificateButton.Name = "certificateButton";
            certificateButton.Size = new Size(776, 28);
            certificateButton.TabIndex = 2;
            certificateButton.Text = "Save SSL/TLS Certificate";
            certificateButton.UseVisualStyleBackColor = true;
            certificateButton.Click += certificateButton_Click;
            // 
            // notifyIcon1
            // 
            notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon1.Icon = (Icon)resources.GetObject("notifyIcon1.Icon");
            notifyIcon1.Text = "ParentControlsWin";
            notifyIcon1.Visible = true;
            notifyIcon1.DoubleClick += notifyIcon1_DoubleClick;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(certificateButton);
            Controls.Add(logTextBox);
            Controls.Add(reconnectButton);
            Name = "MainWindow";
            Text = "ParentControlsWin Wireguard Panel";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button reconnectButton;
        private TextBox logTextBox;
        private Button certificateButton;
        public NotifyIcon notifyIcon1;
    }
}