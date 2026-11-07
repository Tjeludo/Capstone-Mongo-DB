// ViewModels/DashboardViewModel.cs

using App_v1.Pages;
using App_v1.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace App_v1.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _staffName;

    [ObservableProperty]
    private string? _staffRole;

    // Properties to control the visibility of each navigation item
    [ObservableProperty] private Visibility _homeNavVisibility;
    [ObservableProperty] private Visibility _branchesNavVisibility;
    [ObservableProperty] private Visibility _categoriesNavVisibility;
    [ObservableProperty] private Visibility _productsNavVisibility;
    [ObservableProperty] private Visibility _ordersNavVisibility;
    [ObservableProperty] private Visibility _suppliersNavVisibility;
    [ObservableProperty] private Visibility _staffNavVisibility;
    [ObservableProperty] private Visibility _iamNavVisibility;
    [ObservableProperty] private Visibility _deliveriesNavVisibility;
    [ObservableProperty] private Visibility _inventoryNavVisibility;
    [ObservableProperty] private Visibility _transfersNavVisibility;
    [ObservableProperty] private Visibility _analyticsNavVisibility;
    [ObservableProperty] private Visibility _messageRequestsNavVisibility;
    [ObservableProperty] private Visibility _distributionsNavVisibility; 
    [ObservableProperty] private Visibility _stockTakeNavVisibility;

    public DashboardViewModel()
    {
        LoadUserProfile();
        ApplyPermissions();
    }

    public void LoadUserProfile()
    {
        StaffName = UserSession.StaffName ?? "User";
        StaffRole = UserSession.Role ?? "Standard";
    }

    public void ApplyPermissions()
    {
        // This logic sets the visibility properties based on the permissions list
        // from the UserSession. The View will bind to these properties.
        HomeNavVisibility = GetVisibility("view_home");
        BranchesNavVisibility = GetVisibility("view_branches");
        CategoriesNavVisibility = GetVisibility("view_categories");
        ProductsNavVisibility = GetVisibility("view_products");
        OrdersNavVisibility = GetVisibility("view_orders");
        SuppliersNavVisibility = GetVisibility("view_suppliers");
        StaffNavVisibility = GetVisibility("view_staff");
        IamNavVisibility = GetVisibility("view_iam");
        DeliveriesNavVisibility = GetVisibility("view_deliveries");
        InventoryNavVisibility = GetVisibility("view_inventory");
        TransfersNavVisibility = GetVisibility("view_transfers");
        AnalyticsNavVisibility = GetVisibility("view_analytics");
        MessageRequestsNavVisibility = GetVisibility("view_message_requests");
        DistributionsNavVisibility = GetVisibility("view_distributions");
        StockTakeNavVisibility = GetVisibility("view_stock_take");
    }

    private Visibility GetVisibility(string permission)
    {
        return UserSession.HasPermission(permission) ? Visibility.Visible : Visibility.Collapsed;
    }
}