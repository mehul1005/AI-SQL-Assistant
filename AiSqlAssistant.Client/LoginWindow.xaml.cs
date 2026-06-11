using System.Windows;

namespace AiSqlAssistant.Client
{
    public partial class LoginWindow : Window
    {
        private readonly ApiService _apiService;

        public string JwtToken { get; private set; } = string.Empty;
        public string LoggedInUser { get; private set; } = string.Empty;
        public string Role { get; private set; } = string.Empty;

        public LoginWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;
            BtnLogin.IsEnabled = false;
            BtnLogin.Content = "Authenticating...";

            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both username and password.");
                return;
            }

            var response = await _apiService.LoginAsync(username, password);

            if (!string.IsNullOrEmpty(response.Token))
            {
                JwtToken = response.Token;
                LoggedInUser = response.User;
                Role = response.Role;

                this.DialogResult = true;
                this.Close();
            }
            else
            {
                ShowError(response.Error);
            }
        }

        private void ShowError(string message)
        {
            TxtError.Text = message;
            TxtError.Visibility = Visibility.Visible;
            BtnLogin.IsEnabled = true;
            BtnLogin.Content = "Sign In";
        }
    }
}