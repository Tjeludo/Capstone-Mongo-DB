// ViewModels/OrderItemsViewModel.cs

using App_v1.Models;
using App_v1.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace App_v1.ViewModels;

public class OrderItemsViewModel
{
    private readonly OrderApiService _orderApiService;

    public ObservableCollection<OrderItemInfo> OrderItems
    {
        get; set;
    }
    public string OrderId { get; private set; } = string.Empty;

    public OrderItemsViewModel()
    {
        _orderApiService = new OrderApiService();
        OrderItems = new ObservableCollection<OrderItemInfo>();
    }

    public async Task LoadOrderItemsAsync(string orderId)
    {
        OrderId = orderId;
        var response = await _orderApiService.GetOrderItemsAsync(orderId);

        if (response.Success && response.Data != null)
        {
            OrderItems.Clear();
            foreach (var item in response.Data)
            {
                OrderItems.Add(item);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Error loading order items: {response.Message}");
        }
    }
}