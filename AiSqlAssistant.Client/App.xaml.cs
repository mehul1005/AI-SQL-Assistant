using System.Windows;

namespace AiSqlAssistant.Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = new LoginWindow();

            if (loginWindow.ShowDialog() == true)
            {
                var mainWindow = new MainWindow(loginWindow.JwtToken, loginWindow.LoggedInUser, loginWindow.Role);

                Application.Current.MainWindow = mainWindow;

                Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

                mainWindow.Show();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
    }
}