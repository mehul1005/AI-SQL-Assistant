using System.Linq;
using System.Windows;

namespace AiSqlAssistant.Client
{
    public partial class AnalyticsDashboardWindow : Window
    {
        private readonly ApiService _apiService;

        public AnalyticsDashboardWindow(string jwtToken)
        {
            InitializeComponent();
            _apiService = new ApiService();

            // Set the JWT Token
            _apiService.SetAuthToken(jwtToken);

            Loaded += AnalyticsDashboardWindow_Loaded;
        }

        private async void AnalyticsDashboardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var data = await _apiService.GetAnalyticsAsync();
            if (data != null)
            {
                TxtTotal.Text = data.TotalQueries.ToString();
                TxtBlocked.Text = data.BlockedQueries.ToString();
                TxtCritical.Text = data.CriticalRisks.ToString();
                TxtAvgTime.Text = $"{data.AverageDurationMs}ms";

                // Update the maximum value for the bar charts based on the highest user
                int maxQueries = data.TopUsers.Any() ? data.TopUsers.Max(u => u.QueryCount) : 100;

                // We use an anonymous type to inject the dynamic Maximum into the binding
                var chartData = data.TopUsers.Select(u => new
                {
                    UserName = u.UserName,
                    QueryCount = u.QueryCount,
                    MaxCount = maxQueries
                }).ToList();

                TopUsersChart.ItemsSource = chartData;
            }
        }
    }
}