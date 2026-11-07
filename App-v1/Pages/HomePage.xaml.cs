// Pages/HomePage.xaml.cs
using System;
using System.Linq;
using App_v1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace App_v1.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel
    {
        get;
    }

    public HomePage()
    {
        this.InitializeComponent();
        ViewModel = new HomeViewModel();
    }

    private void PlaceOrder_Click(object sender, RoutedEventArgs e)
    {
        // Assuming you have an ItemPage for creating new orders
        this.Frame.Navigate(typeof(ItemPage));
    }

    private void AddProduct_Click(object sender, RoutedEventArgs e)
    {
        // Assuming you have a ProductsPage
        this.Frame.Navigate(typeof(ProductsPage));
    }



    private void Distribute_Click(object sender, RoutedEventArgs e)
    {
        // Assuming you have a Distributions page
        this.Frame.Navigate(typeof(Distributions));
    }

    /// <summary>
    /// Handles the click event for any of the dashboard cards.
    /// </summary>
    private void DashboardCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button clickedButton || clickedButton.Tag is not DashboardCard card) return;

        Type targetPage = null;
        string targetTag = null;

        switch (card.CardIdentifier)
        {
            case "PendingOrders":
            case "ConfirmedOrders":
                targetPage = typeof(OrdersPage);
                targetTag = "OrdersPage"; 
                break;
            case "Deliveries":
                targetPage = typeof(DeliveryPage);
                targetTag = "DeliveriesPage"; 
                break;
            case "Transfers":
                targetPage = typeof(Transfers);
                targetTag = "TransfersPage"; 
                break;
            case "NewRequests":
                targetPage = typeof(MessageRequestPage);
                targetTag = "MessageRequestPage";
                break;
            default:
                return; 
        }

        if (targetPage != null)
        {
            
            this.Frame.Navigate(targetPage);
           
            if (DashboardPage.Instance != null && targetTag != null)
            {
                var newItem = DashboardPage.Instance.AppNavigationView.MenuItems
                    .OfType<NavigationViewItem>()
                    .FirstOrDefault(item => item.Tag?.ToString() == targetTag);

                if (newItem != null)
                {
                    DashboardPage.Instance.AppNavigationView.SelectedItem = newItem;
                }
            }
        }
    }
}