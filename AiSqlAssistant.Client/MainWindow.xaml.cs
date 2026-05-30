using System.Data;
using System.Windows;
using System.Text.Json;

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

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            string prompt = PromptTextBox.Text;

            if (string.IsNullOrWhiteSpace(prompt))
            {
                MessageBox.Show("Please enter a prompt.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // UI Loading State
            GenerateButton.IsEnabled = false;
            GenerateButton.Content = "Executing...";
            OutputTextBox.Text = "-- Discovering schema and executing SQL...";
            ResultsDataGrid.ItemsSource = null; // Clear previous results

            // Call the API (Only passing the prompt now!)
            var response = await _apiService.GetSqlAndDataAsync(prompt);

            // Handle Errors
            if (!string.IsNullOrEmpty(response.Error))
            {
                OutputTextBox.Text = $"-- ERROR: {response.Error}\n\n{response.GeneratedSql}";
                GenerateButton.Content = "Execute AI Query";
                GenerateButton.IsEnabled = true;
                return;
            }

            // Display SQL Text
            OutputTextBox.Text = response.GeneratedSql;

            // Bind the dynamic JSON data to the WPF DataGrid
            if (response.Data != null && response.Data.Count > 0)
            {
                System.Data.DataTable dataTable = new System.Data.DataTable();

                // Create columns dynamically based on the first row's keys
                foreach (var key in response.Data[0].Keys)
                {
                    dataTable.Columns.Add(key);
                }

                // Add the rows
                foreach (var rowDict in response.Data)
                {
                    System.Data.DataRow newRow = dataTable.NewRow();
                    foreach (var kvp in rowDict)
                    {
                        newRow[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                    }
                    dataTable.Rows.Add(newRow);
                }

                ResultsDataGrid.ItemsSource = dataTable.DefaultView;
            }

            // Reset UI
            GenerateButton.Content = "Execute AI Query";
            GenerateButton.IsEnabled = true;
        }
    }
}