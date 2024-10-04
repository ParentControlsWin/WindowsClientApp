using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ParentControlsWinGui
{
    public partial class LoginForm : Form
    {
        LoginManager login_manager;

        public LoginForm(LoginManager login_manager_in)
        {
            InitializeComponent();

            LinkedList<String> allAccounts = UserMonitor.ListUserAccounts();

            foreach (String account in allAccounts)
            {
                //checkedListBox1.Items.Add(UserMonitor.getUsername(account));
                checkedListBox1.Items.Add(account);
            }

            this.login_manager = login_manager_in;
        }

        private void signinButton_Click(object sender, EventArgs e)
        {
            LinkedList<String> accounts = new LinkedList<String>();

            // TODO read in user selections
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                if (checkedListBox1.GetItemChecked(i))
                {
                    String item = checkedListBox1.Items[i].ToString();
                    accounts.AddLast(item);
                }
            }
            // Ping server to login and generate tokens
            bool login_success = login_manager.CreateLoginTokens(emailTextBox.Text, passwordTextBox.Text, deviceNameTextBox.Text, accounts);

            if (login_success)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Error logging in; please check your login credentials and try again.");
            }
        }

        private void passwordTextBox_TextChanged(object sender, EventArgs e)
        {
            passwordTextBox.PasswordChar = '*';
        }
    }
}