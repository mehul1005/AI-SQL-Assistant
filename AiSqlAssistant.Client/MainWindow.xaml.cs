using System.Data;
using System.Windows;

namespace AiSqlAssistant.Client
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly string _jwtToken; // Store the JWT token

        // Updated constructor to accept JWT details from LoginWindow
        public MainWindow(string jwtToken, string user, string role)
        {
            InitializeComponent();
            _apiService = new ApiService();
            _jwtToken = jwtToken;

            // Updated the window title to show the logged-in user
            this.Title = $"AI SQL Assistant - Enterprise Security | Logged in as: {user} ({role})";
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PromptTextBox.Text)) return;

            // Inject the JWT Bearer Token
            _apiService.SetAuthToken(_jwtToken);

            GenerateButton.IsEnabled = false;
            ExecuteButton.IsEnabled = false;
            OutputTextBox.Text = "-- Generating SQL...";
            ResultsDataGrid.ItemsSource = null;

            var response = await _apiService.GenerateSqlAsync(PromptTextBox.Text);

            if (!string.IsNullOrEmpty(response.Error))
            {
                OutputTextBox.Text = $"-- ERROR: {response.Error}";
                RiskDashboard.Visibility = Visibility.Collapsed;
            }
            else
            {
                OutputTextBox.Text = response.GeneratedSql;

                RiskDashboard.Visibility = Visibility.Visible;
                OperationText.Text = response.RiskProfile.Operation;
                TablesText.Text = string.Join(", ", response.RiskProfile.AffectedTables);
                RiskText.Text = response.RiskProfile.RiskLevel;

                if (response.RiskProfile.RiskLevel == "LOW") RiskBadge.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981"));
                else if (response.RiskProfile.RiskLevel == "MEDIUM") RiskBadge.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F59E0B"));
                else RiskBadge.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"));

                ExecuteButton.IsEnabled = !response.RiskProfile.IsExecutionBlocked;
            }

            GenerateButton.IsEnabled = true;
        }

        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OutputTextBox.Text)) return;

            // Inject the JWT Bearer Token
            _apiService.SetAuthToken(_jwtToken);

            ExecuteButton.IsEnabled = false;
            string approvedSql = OutputTextBox.Text;

            var response = await _apiService.ExecuteSqlAsync(approvedSql, PromptTextBox.Text, RiskText.Text);

            if (!string.IsNullOrEmpty(response.Error))
            {
                MessageBox.Show(response.Error, "Security/Execution Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                ExecuteButton.IsEnabled = true;
                return;
            }

            if (response.Data != null && response.Data.Count > 0)
            {
                DataTable dataTable = new DataTable();
                foreach (var key in response.Data[0].Keys) dataTable.Columns.Add(key);
                foreach (var rowDict in response.Data)
                {
                    DataRow newRow = dataTable.NewRow();
                    foreach (var kvp in rowDict) newRow[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                    dataTable.Rows.Add(newRow);
                }
                ResultsDataGrid.ItemsSource = dataTable.DefaultView;
            }
            else
            {
                ResultsDataGrid.ItemsSource = null;
                MessageBox.Show("Query executed successfully, but returned 0 rows.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            ExecuteButton.IsEnabled = true;
        }

        private void ViewLogsButton_Click(object sender, RoutedEventArgs e)
        {
            // Pass the JWT Token instead of the ApiKey
            var logWindow = new AuditLogWindow(_jwtToken) { Owner = this };
            logWindow.ShowDialog();
        }

        private void ViewAnalyticsButton_Click(object sender, RoutedEventArgs e)
        {
            // Pass the JWT Token instead of the ApiKey
            var dashboardWindow = new AnalyticsDashboardWindow(_jwtToken) { Owner = this };
            dashboardWindow.ShowDialog();
        }
    }
}