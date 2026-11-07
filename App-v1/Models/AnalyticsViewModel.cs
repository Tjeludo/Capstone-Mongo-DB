// ViewModels/AnalyticsViewModel.cs

using App_v1.Models;
using App_v1.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace App_v1.ViewModels;

public class TimePeriod
{
    public string DisplayName { get; set; } = string.Empty;
    public int Days
    {
        get; set;
    }
}

public partial class AnalyticsViewModel : ObservableObject
{
    private readonly AnalyticsApiService _analyticsApiService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<ProductMetric> _mostOrderedProducts = new();

    [ObservableProperty]
    private ObservableCollection<StatusMetric> _orderStatusDistribution = new();

    [ObservableProperty]
    private ObservableCollection<SupplierMetric> _orderFrequencyBySupplier = new();

    // Collection for the ComboBox items
    public ObservableCollection<TimePeriod> Periods
    {
        get;
    }

    // Property to hold the selected time period
    [ObservableProperty]
    private TimePeriod _selectedPeriod;

    public AnalyticsViewModel()
    {
        _analyticsApiService = new AnalyticsApiService();

        Periods = new ObservableCollection<TimePeriod>
        {
            new() { DisplayName = "Last 7 Days", Days = 7 },
            new() { DisplayName = "Last 14 Days", Days = 14 },
            new() { DisplayName = "Last 30 Days", Days = 30 },
            new() { DisplayName = "Last Quarter", Days = 92 },
            new() { DisplayName = "Last 6 Months", Days = 183 },
            new() { DisplayName = "Last Year", Days = 365 }
        };

        // Set the default selection
        _selectedPeriod = Periods.FirstOrDefault(p => p.Days == 30) ?? Periods.First();

        _ = LoadAnalyticsAsync();
    }

    // This method is automatically called when SelectedPeriod changes
    partial void OnSelectedPeriodChanged(TimePeriod value)
    {
        _ = LoadAnalyticsAsync();
    }

    public async Task LoadAnalyticsAsync()
    {
        IsLoading = true;

        // Use the 'Days' value from the currently selected period
        var response = await _analyticsApiService.GetAnalyticsAsync(SelectedPeriod.Days);

        if (response.Success && response.Data != null)
        {
            MostOrderedProducts = new ObservableCollection<ProductMetric>(response.Data.MostOrderedProducts);
            OrderStatusDistribution = new ObservableCollection<StatusMetric>(response.Data.OrderStatusDistribution);
            OrderFrequencyBySupplier = new ObservableCollection<SupplierMetric>(response.Data.OrderFrequencyBySupplier);
        }
        else
        {
            // Optionally, handle the error state in the UI
            System.Diagnostics.Debug.WriteLine($"Error loading analytics: {response.Message}");
        }

        IsLoading = false;
    }
}