using System.Data;
using System.Windows;

namespace AiSqlAssistant.Client
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
        }

        // STEP 1: GENERATE ONLY
        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PromptTextBox.Text)) return;

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

                // Populate the Risk Dashboard
                RiskDashboard.Visibility = Visibility.Visible;
                OperationText.Text = response.RiskProfile.Operation;
                TablesText.Text = string.Join(", ", response.RiskProfile.AffectedTables);
                RiskText.Text = response.RiskProfile.RiskLevel;

                // Color code the Risk Badge
                if (response.RiskProfile.RiskLevel == "LOW") RiskBadge.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981")); // Green
                else if (response.RiskProfile.RiskLevel == "MEDIUM") RiskBadge.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F59E0B")); // Yellow
                else RiskBadge.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444")); // Red

                ExecuteButton.IsEnabled = !response.RiskProfile.IsExecutionBlocked; // Keep Execute locked if CRITICAL!
            }

            GenerateButton.IsEnabled = true;
        }

        // STEP 2: APPROVE & EXECUTE
        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OutputTextBox.Text)) return;

            ExecuteButton.IsEnabled = false;
            string approvedSql = OutputTextBox.Text; // Grab the user-reviewed text!

            var response = await _apiService.ExecuteSqlAsync(approvedSql);

            if (!string.IsNullOrEmpty(response.Error))
            {
                MessageBox.Show(response.Error, "Security/Execution Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                ExecuteButton.IsEnabled = true;
                return;
            }

            // Bind the DataGrid
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
    }
}