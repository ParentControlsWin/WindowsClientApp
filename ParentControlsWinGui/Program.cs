namespace ParentControlsWinGui
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the ParentControls.Win GUI, which
        ///  connects to the ParentControls.Win Service.
        /// </summary>
        [STAThread]
        static void Main()
        {
            LoginManager login_manager = new LoginManager();
            PrivilegedServiceController hiddenService = new PrivilegedServiceController();

            if (!hiddenService.IsServiceRunning())
            {
                MessageBox.Show("ParentControls.Win Service isn't running, please restart your computer. This sometimes happens after first install if the service fails to start.");
                // System.Windows.Forms.Application.Exit();
                System.Environment.Exit(1);
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            ApplicationConfiguration.Initialize();

            if (login_manager.LoadLoginTokens()) {
                MainWindow mainForm = new MainWindow();
                mainForm.Show();
                Application.Run();
            }

            while (!login_manager.LoadLoginTokens())
            {
                using (var loginForm = new LoginForm(login_manager))
                {
                    if (loginForm.ShowDialog() != DialogResult.OK)
                    {
                        // If the login form is closed without successful login, exit the application
                        break;
                    }
                }
            }
        }
    }
}