namespace ParentControlsWinGui
{
    partial class LoginForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupBox1 = new GroupBox();
            signinButton = new Button();
            checkboxListLabel = new Label();
            checkedListBox1 = new CheckedListBox();
            deviceNameTextBox = new TextBox();
            deviceNameLabel = new Label();
            passwordTextBox = new TextBox();
            passwordLabel = new Label();
            emailTextBox = new TextBox();
            emailLabel = new Label();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            groupBox1.Controls.Add(signinButton);
            groupBox1.Controls.Add(checkboxListLabel);
            groupBox1.Controls.Add(checkedListBox1);
            groupBox1.Controls.Add(deviceNameTextBox);
            groupBox1.Controls.Add(deviceNameLabel);
            groupBox1.Controls.Add(passwordTextBox);
            groupBox1.Controls.Add(passwordLabel);
            groupBox1.Controls.Add(emailTextBox);
            groupBox1.Controls.Add(emailLabel);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(384, 426);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "ParentControlsWin Sign-in";
            // 
            // signinButton
            // 
            signinButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            signinButton.Location = new Point(6, 392);
            signinButton.Name = "signinButton";
            signinButton.Size = new Size(90, 28);
            signinButton.TabIndex = 7;
            signinButton.Text = "Sign-in";
            signinButton.UseVisualStyleBackColor = true;
            signinButton.Click += signinButton_Click;
            // 
            // checkboxListLabel
            // 
            checkboxListLabel.AutoSize = true;
            checkboxListLabel.Location = new Point(6, 178);
            checkboxListLabel.Name = "checkboxListLabel";
            checkboxListLabel.Size = new Size(316, 20);
            checkboxListLabel.TabIndex = 6;
            checkboxListLabel.Text = "Select Account to Monitor (i.e. Child Accounts)";
            // 
            // checkedListBox1
            // 
            checkedListBox1.FormattingEnabled = true;
            checkedListBox1.Location = new Point(6, 201);
            checkedListBox1.Name = "checkedListBox1";
            checkedListBox1.Size = new Size(279, 151);
            checkedListBox1.TabIndex = 1;
            // 
            // deviceNameTextBox
            // 
            deviceNameTextBox.Location = new Point(6, 149);
            deviceNameTextBox.Name = "deviceNameTextBox";
            deviceNameTextBox.Size = new Size(210, 26);
            deviceNameTextBox.TabIndex = 5;
            // 
            // deviceNameLabel
            // 
            deviceNameLabel.AutoSize = true;
            deviceNameLabel.Location = new Point(6, 126);
            deviceNameLabel.Name = "deviceNameLabel";
            deviceNameLabel.Size = new Size(98, 20);
            deviceNameLabel.TabIndex = 4;
            deviceNameLabel.Text = "Device Name";
            // 
            // passwordTextBox
            // 
            passwordTextBox.Location = new Point(6, 97);
            passwordTextBox.Name = "passwordTextBox";
            passwordTextBox.Size = new Size(210, 26);
            passwordTextBox.TabIndex = 3;
            passwordTextBox.TextChanged += passwordTextBox_TextChanged;
            // 
            // passwordLabel
            // 
            passwordLabel.AutoSize = true;
            passwordLabel.Location = new Point(6, 74);
            passwordLabel.Name = "passwordLabel";
            passwordLabel.Size = new Size(70, 20);
            passwordLabel.TabIndex = 2;
            passwordLabel.Text = "Password";
            // 
            // emailTextBox
            // 
            emailTextBox.Location = new Point(6, 45);
            emailTextBox.Name = "emailTextBox";
            emailTextBox.Size = new Size(210, 26);
            emailTextBox.TabIndex = 1;
            // 
            // emailLabel
            // 
            emailLabel.AutoSize = true;
            emailLabel.Location = new Point(3, 22);
            emailLabel.Name = "emailLabel";
            emailLabel.Size = new Size(103, 20);
            emailLabel.TabIndex = 0;
            emailLabel.Text = "Email Address";
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(groupBox1);
            Name = "LoginForm";
            Text = "Login to Parent Controls Win";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBox1;
        private TextBox passwordTextBox;
        private Label passwordLabel;
        private TextBox emailTextBox;
        private Label emailLabel;
        private TextBox deviceNameTextBox;
        private Label deviceNameLabel;
        private CheckedListBox checkedListBox1;
        private Button signinButton;
        private Label checkboxListLabel;
    }
}