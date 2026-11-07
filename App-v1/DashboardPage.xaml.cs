// DashboardPage.xaml.cs

using System.Linq;
using System.Threading.Tasks;
using App_v1.Models;
using App_v1.Pages;
using App_v1.Services;
using App_v1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace App_v1;

public sealed partial class DashboardPage : Page
{
    //A static property to hold the instance of the page
    public static DashboardPage Instance
    {
        get; private set;
    }

    //A public property to expose the NavigationView control
    public NavigationView AppNavigationView => NavView;

    // Public property to access the ViewModel
    public DashboardViewModel ViewModel => (DashboardViewModel)DataContext;

    public DashboardPage()
    {
        this.InitializeComponent();
        ContentFrame.Navigated += On_Navigated;
        // Set the instance to the current page
        Instance = this;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        ViewModel.LoadUserProfile();
        ViewModel.ApplyPermissions();

        // Set a default page only for Administrators.
        if (NavView.SelectedItem == null)
        {
            // Check if the current user is an Administrator.
            if (UserSession.Role == "Administrator")
            {
                var homeItem = NavView.MenuItems
                                      .OfType<NavigationViewItem>()
                                      .FirstOrDefault(item => item.Tag?.ToString() == "HomePage");

                if (homeItem != null && homeItem.Visibility == Visibility.Visible)
                {
                    NavView.SelectedItem = homeItem;
                    ContentFrame.Navigate(typeof(HomePage));
                }
            }
            // For all other roles, NavView.SelectedItem remains null, and no page is loaded by default.
            // The user will see a blank frame and must select an item from the menu.
        }
    }

    private void On_Navigated(object sender, NavigationEventArgs e)
    {
        // Find the NavigationViewItem that corresponds to the page just navigated to.
        var item = NavView.MenuItems
                          .OfType<NavigationViewItem>()
                          .FirstOrDefault(i => i.Tag is string tag && typeof(HomePage).Namespace + "." + tag == e.SourcePageType.FullName);

        if (item == null)
        {
            item = NavView.FooterMenuItems
                          .OfType<NavigationViewItem>()
                          .FirstOrDefault(i => i.Tag is string tag && typeof(HomePage).Namespace + "." + tag == e.SourcePageType.FullName);
        }

        if (item != null)
        {
            // Update the selected item without triggering a new navigation.
            NavView.SelectedItem = item;
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem selectedItem)
        {
            var tag = selectedItem.Tag?.ToString();

            // This now navigates to your actual page files
            switch (tag)
            {
                case "HomePage":
                    ContentFrame.Navigate(typeof(HomePage));
                    break;
                case "CategoriesPage":
                    ContentFrame.Navigate(typeof(CategoriesPage));
                    break;
                case "ProductsPage":
                    ContentFrame.Navigate(typeof(ProductsPage));
                    break;
                case "OrdersPage":
                    ContentFrame.Navigate(typeof(OrdersPage));
                    break;
                case "SuppliersPage":
                    ContentFrame.Navigate(typeof(SuppliersPage));
                    break;
                case "StaffPage":
                    ContentFrame.Navigate(typeof(StaffPage));
                    break;
                case "IAMPage":
                    ContentFrame.Navigate(typeof(IAMPage));
                    break;
                case "DeliveriesPage":
                    ContentFrame.Navigate(typeof(DeliveryPage));
                    break;
                case "AnalyticsPage":
                    ContentFrame.Navigate(typeof(AnalyticsPage));
                    break;
                case "Help":
                    ContentFrame.Navigate(typeof(WebViewPage));
                    break;
                case "Settings":
                    ContentFrame.Navigate(typeof(SettingsPage));
                    break;
                case "Logout":
                    UserSession.EndSession();
                    this.Frame.Navigate(typeof(LoginPage));
                    this.Frame.BackStack.Clear();
                    break;
                case "TransfersPage":
                    ContentFrame.Navigate(typeof(Transfers));
                    break;
                case "DistributionsPage":
                    ContentFrame.Navigate(typeof(App_v1.Pages.Distributions));
                    break;
                case "InventoryPage":
                    ContentFrame.Navigate(typeof(App_v1.Pages.Inventory));
                    break;
                case "BranchesPage":
                    ContentFrame.Navigate(typeof(Branches));
                    break;
                case "StockTakePage":
                    ContentFrame.Navigate(typeof(StockTakePage));
                    break;
                // Add the new navigation case for MessageRequestPage
                case "MessageRequestPage":
                    ContentFrame.Navigate(typeof(MessageRequestPage));
                    break;
            }
        }
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
    }

    private void Control2_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Logic for when search text changes goes here
    }

    private void Control2_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        // Logic for when a search query is submitted goes here
    }

    private void Control2_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        // Logic for when a search suggestion is chosen goes here
    }

    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        //ContentFrame.Navigate(typeof(SettingsPage));
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshButton.IsEnabled = false;

        // Refresh the main dashboard counts regardless of the current page
        if (ContentFrame.DataContext is HomeViewModel homeVm)
        {
            await homeVm.LoadHomePageDataAsync();
        }


        // Refresh data for the currently displayed page
        if (ContentFrame.Content is Page currentPage && currentPage.DataContext != null)
        {
            var context = currentPage.DataContext;
            if (context is AnalyticsViewModel analyticsVm) await analyticsVm.LoadAnalyticsAsync();
            if (context is CategoryViewModel categoryVm) await categoryVm.LoadCategoriesAsync();
            if (context is DeliveryViewModel deliveryVm) deliveryVm.LoadDeliveries();
            if (context is DistributionViewModel distributionVm) await distributionVm.LoadDistribibutions();
            if (context is InventoryViewModel inventoryVm) await inventoryVm.LoadInventory();
            if (context is MessageRequestViewModel messageRequestVm) await messageRequestVm.LoadRequestsAsync();
            if (context is OrdersViewModel ordersVm) await ordersVm.LoadOrdersAsync();
            if (context is PermissionsViewModel permissionsVm) await permissionsVm.LoadPermissionsAsync();
            if (context is ProductViewModel productVm) await productVm.LoadProductsAsync();
            if (context is RolesViewModel rolesVm) await rolesVm.LoadRolesAsync();
            if (context is StaffViewModel staffVm) await staffVm.LoadStaffAsync();
            if (context is StockTakeViewModel stockTakeVm) stockTakeVm.LoadStockTakes();
            if (context is SupplierViewModel supplierVm) await supplierVm.LoadSuppliersAsync();
            if (context is TransferViewModel transferVm) await transferVm.LoadTransfers();
            if (context is UserViewModel userVm) await userVm.LoadUsersAsync();
        }

        // Refresh the current user's permissions
        await UserSession.UpdatePermissionsAsync();
        ViewModel.ApplyPermissions(); // Re-apply visibility rules for nav items

        RefreshButton.IsEnabled = true;
    }
}