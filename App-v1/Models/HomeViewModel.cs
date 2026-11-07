// ViewModels/HomeViewModel.cs

using App_v1.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;

namespace App_v1.ViewModels;

public class ActivityItem
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconColor { get; set; } = string.Empty; // CHANGE: Added IconColor property
}

public class DashboardCard
{
    public string? Icon
    {
        get; set;
    }
    public string? Title
    {
        get; set;
    }
    public string? Value
    {
        get; set;
    }
    public string? Description
    {
        get; set;
    }
    public string? ForegroundColor
    {
        get; set;
    }
    public string? CardIdentifier
    {
        get; set;
    }
}

public partial class HomeViewModel : ObservableObject
{
    private readonly HomeApiService _apiService;

    [ObservableProperty]
    private string? _staffName;

    public string WelcomeMessage => $"Welcome back, {UserSession.StaffName ?? "User"} 👋";

    [ObservableProperty]
    private ObservableCollection<DashboardCard> _dashboardCards = new();

    [ObservableProperty]
    private ObservableCollection<ActivityItem> _recentActivity = new();

    public HomeViewModel()
    {
        _apiService = new HomeApiService();
        LoadUserProfile();
        _ = LoadHomePageDataAsync();
    }

    public async Task LoadHomePageDataAsync()
    {

        var dashboardData = await _apiService.GetDashboardDataAsync();

        if (dashboardData != null)
        {
            // Clear the collection before adding new items to support data refreshing.
            DashboardCards.Clear();

            // Populate the DashboardCards collection with data from the API
            DashboardCards.Add(new()
            {
                Icon = "\uE939",
                Title = "New Requests",
                Value = dashboardData.NewRequestsCount.ToString(),
                Description = "Awaiting admin approval",
                ForegroundColor = "Teal",
                CardIdentifier = "NewRequests"
            });            
            DashboardCards.Add(new()
            {
                Icon = "\uE823",
                Title = "Pending Orders",
                Value = dashboardData.PendingOrdersCount.ToString(),
                Description = "Awaiting supplier confirmation",
                ForegroundColor = "Orange",
                CardIdentifier = "PendingOrders"
            });
            DashboardCards.Add(new()
            {
                Icon = "\uE930",
                Title = "Confirmed Orders",
                Value = dashboardData.ConfirmedOrdersCount.ToString(),
                Description = "Ready for processing",
                ForegroundColor = "Green",
                CardIdentifier = "ConfirmedOrders"
            });
            DashboardCards.Add(new()
            {
                Icon = "\uE7B8",
                Title = "Deliveries",
                Value = dashboardData.DeliveriesCount.ToString(),
                Description = "For the current week",
                ForegroundColor = "DodgerBlue",
                CardIdentifier = "Deliveries"
            });
            DashboardCards.Add(new()
            {
                Icon = "\uE895",
                Title = "Transfers",
                Value = dashboardData.TransfersCount.ToString(),
                Description = "For the current week",
                ForegroundColor = "#E83E8C",
                CardIdentifier = "Transfers"
            });
            DashboardCards.Add(new()
            {
                Icon = "\uEF42",
                Title = "Total Expenses",
                Value = dashboardData.TotalExpenses.ToString("C", CultureInfo.GetCultureInfo("en-ZA")),
                Description = "For the current month",
                ForegroundColor = "Red",
                CardIdentifier = "TotalExpenses"
            });
        }

        RecentActivity.Clear();
        // CHANGE: Added IconColor to each new ActivityItem
        RecentActivity.Add(new() { Icon = "\uEDAC", Title = "Order Placed", Description = "ODR105, Supplier S01, Ordered-by Ashley - 15 minutes ago", IconColor = "#007BFF" });
        RecentActivity.Add(new() { Icon = "\uEDAC", Title = "Delivery Received ", Description = "DEL002, From Supplier S03, Checked-in by Alice - 30 minutes ago", IconColor = "#28A745" });
        RecentActivity.Add(new() { Icon = "\uEDAC", Title = "Stock Take Completed", Description = "ST001, Kenilworth Branch, Audited by J. Doe - 1 hour ago", IconColor = "#FD7E14" });
        RecentActivity.Add(new() { Icon = "\uEDAC", Title = "Distribution Completed", Description = "DEL001, Destination: Claremont, Authorised by Sonia - 2 hours ago", IconColor = "#E83E8C" });
        RecentActivity.Add(new() { Icon = "\uEDAC", Title = "Transfer Completed", Description = "From Claremont to Kenilworth (Baby Bottles), Initiated-by Vimbai - 4 hours ago", IconColor = "#FFC107" });
    }

    public void LoadUserProfile()
    {
        // This can remain as is, or you could fetch user data from an API as well.
        StaffName = "User"; // Example name
        OnPropertyChanged(nameof(WelcomeMessage));
    }
}