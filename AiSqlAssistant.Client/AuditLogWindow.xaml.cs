using System.Windows;

namespace AiSqlAssistant.Client
{
    public partial class AuditLogWindow : Window
    {
        private readonly ApiService _apiService;

        // Updated constructor to require the JWT Token
        public AuditLogWindow(string jwtToken)
        {
            InitializeComponent();
            _apiService = new ApiService();

            // 1. Set the JWT Token
            _apiService.SetAuthToken(jwtToken);

            // 2. Fetch the protected data
            LoadLogs();
        }

        private async void LoadLogs()
        {
            var logs = await _apiService.GetAuditLogsAsync();
            LogsDataGrid.ItemsSource = logs;
        }
    }
}