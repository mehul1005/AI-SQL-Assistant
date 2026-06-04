using System.Windows;

namespace AiSqlAssistant.Client
{
    public partial class AuditLogWindow : Window
    {
        private readonly ApiService _apiService;

        public AuditLogWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            LoadLogs(); // Fetch data
        }

        private async void LoadLogs()
        {
            var logs = await _apiService.GetAuditLogsAsync();
            LogsDataGrid.ItemsSource = logs;
        }
    }
}