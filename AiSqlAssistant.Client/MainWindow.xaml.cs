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

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            string schema = SchemaTextBox.Text;
            string prompt = PromptTextBox.Text;

            if (string.IsNullOrWhiteSpace(prompt))
            {
                MessageBox.Show("Please enter a prompt.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update UI to show loading state
            GenerateButton.IsEnabled = false;
            GenerateButton.Content = "Thinking...";
            OutputTextBox.Text = "-- Generating SQL query...";

            // Call the ASP.NET Core API
            string generatedSql = await _apiService.GetSqlAsync(prompt, schema);

            // Update UI with result
            OutputTextBox.Text = generatedSql;
            GenerateButton.Content = "Generate SQL";
            GenerateButton.IsEnabled = true;
        }
    }
}