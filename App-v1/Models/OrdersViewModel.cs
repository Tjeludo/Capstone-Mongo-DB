/*
This ViewModel is the brain for the OrdersPage. It holds the list of orders
and manages user actions like viewing, editing, or canceling an order.
It talks to the OrderApiService for any server-side work.
*/
using App_v1.Models;
using App_v1.Services;
using CommunityToolkit.Mvvm.Input;
using Syncfusion.UI.Xaml.DataGrid;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace App_v1.ViewModels;

public class OrdersViewModel
{
    private readonly OrderApiService _orderApiService;

    public ObservableCollection<OrdersInfo> Orders
    {
        get; set;
    }

    public ICommand GridViewCommand
    {
        get;
    }
    public ICommand GridEditCommand
    {
        get;
    }
    public ICommand GridCancelCommand
    {
        get;
    }

    // Controls which buttons are visible based on user permissions.
    public bool CanPlaceOrder => UserSession.HasPermission("place_orders");
    public bool CanViewOrder => UserSession.HasPermission("view_orders");
    public bool CanEditOrder => UserSession.HasPermission("edit_orders");
    public bool CanCancelOrder => UserSession.HasPermission("cancel_orders");
    public bool CanDownloadCsv => UserSession.HasPermission("download_orders");

    /*
    These events let the ViewModel tell the View (the UI) to do things
    like open a dialog, without the ViewModel needing to know about UI controls.
    */
    public event Func<OrdersInfo, Task>? ViewRequested;
    public event Func<OrdersInfo, Task>? EditRequested;
    public event Func<OrdersInfo, Task>? CancelRequested;

    public OrdersViewModel()
    {
        _orderApiService = new OrderApiService();
        Orders = new ObservableCollection<OrdersInfo>();

        _ = LoadOrdersAsync();

        GridViewCommand = new RelayCommand<object?>(OnView);
        GridEditCommand = new RelayCommand<object?>(OnEdit);
        GridCancelCommand = new RelayCommand<object?>(OnCancel);
    }

    public async Task LoadOrdersAsync()
    {
        var response = await _orderApiService.GetOrdersAsync();

        if (response.Success && response.Data != null)
        {
            Orders.Clear();
            foreach (var order in response.Data)
            {
                Orders.Add(order);
            }
        }
        else
        {
            // If loading fails, log it. The UI will just show an empty list.
            System.Diagnostics.Debug.WriteLine($"Error loading orders: {response.Message}");
        }
    }

    public async Task<(bool success, string message)> UpdateOrderStatusAsync(string orderId, string newStatus)
    {
        var response = await _orderApiService.UpdateOrderStatusAsync(orderId, newStatus);
        if (response.Success)
        {
            // After updating, reload the list to show the change.
            await LoadOrdersAsync();
        }
        return (response.Success, response.Message);
    }

    public async Task<(bool success, string message)> CancelOrderAsync(string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
        {
            return (false, "Order ID cannot be null.");
        }

        var response = await _orderApiService.CancelOrderAsync(orderId);
        if (response.Success)
        {
            // Reload the entire list to ensure the UI is in sync with the database.
            await LoadOrdersAsync();
        }
        return (response.Success, response.Message);
    }

    public async Task<(bool success, string message)> ConfirmOrderAsync(string orderedBy)
    {
        var response = await _orderApiService.ConfirmOrderAsync(orderedBy);
        if (response.Success)
        {
            // After placing a new order, refresh the list.
            await LoadOrdersAsync();
        }
        return (response.Success, response.Message);
    }

    public async Task<byte[]?> DownloadCsvAsync()
    {
        return await _orderApiService.DownloadCsvAsync();
    }

    private OrdersInfo? GetOrderFromParameter(object? parameter)
    {
        // This helper figures out where the order object is coming from.
        // It could be from a flyout menu or passed directly.
        return parameter switch
        {
            GridRecordContextFlyoutInfo flyoutInfo => flyoutInfo.Record as OrdersInfo,
            OrdersInfo directOrder => directOrder,
            _ => null
        };
    }

    private async void OnView(object? parameter)
    {
        if (GetOrderFromParameter(parameter) is OrdersInfo order && ViewRequested != null)
            await ViewRequested.Invoke(order);
    }

    private async void OnEdit(object? parameter)
    {
        if (GetOrderFromParameter(parameter) is OrdersInfo order && EditRequested != null)
            await EditRequested.Invoke(order);
    }

    private async void OnCancel(object? parameter)
    {
        if (GetOrderFromParameter(parameter) is OrdersInfo order && CancelRequested != null)
            await CancelRequested.Invoke(order);
    }
}