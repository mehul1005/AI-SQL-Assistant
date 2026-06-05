using System.Windows;

namespace AiSqlAssistant.Client
{
    public partial class AuditLogWindow : Window
    {
        private readonly ApiService _apiService;

        // Updated constructor to require the API Key
        public AuditLogWindow(string currentApiKey)
        {
            InitializeComponent();
            _apiService = new ApiService();

            // 1. Set the identity immediately 
            _apiService.SetApiKey(currentApiKey);

            // 2. Now it is safe to fetch the protected data
            LoadLogs();
        }

        private async void LoadLogs()
        {
            var logs = await _apiService.GetAuditLogsAsync();
            LogsDataGrid.ItemsSource = logs;
        }
    }
}