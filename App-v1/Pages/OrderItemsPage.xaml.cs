// Pages/OrderItemsPage.xaml.cs

using App_v1.Helpers;
using App_v1.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace App_v1.Pages;

public sealed partial class OrderItemsPage : Page
{
    private OrderItemsViewModel ViewModel => (OrderItemsViewModel)DataContext;

    public OrderItemsPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (!string.IsNullOrEmpty(AppState.SelectedOrderId))
        {
            await ViewModel.LoadOrderItemsAsync(AppState.SelectedOrderId);
            PageTitle.Text = $"Items for Order: {ViewModel.OrderId}";
        }
    }
}