// FileName: ItemViewModel.cs
using App_v1.Models;
using App_v1.Services;
using CommunityToolkit.Mvvm.Input;
using Syncfusion.UI.Xaml.DataGrid;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace App_v1.ViewModels;

public class ItemViewModel
{
    private readonly ItemApiService _itemApiService;

    public ObservableCollection<ItemInfo> Items
    {
        get; set;
    }
    public ICommand AddToCartCommand
    {
        get;
    }

    // New property for button visibility
    public bool CanAddToCart => UserSession.HasPermission("add_orders");

    public event Func<ItemInfo, Task>? AddToCartRequested;

    public ItemViewModel()
    {
        _itemApiService = new ItemApiService();
        Items = new ObservableCollection<ItemInfo>();
        _ = LoadItemsAsync();

        AddToCartCommand = new RelayCommand<object?>(OnAddToCart);
    }

    public async Task LoadItemsAsync()
    {
        var response = await _itemApiService.GetItemsAsync();

        if (response.Success && response.Data != null)
        {
            Items.Clear();
            foreach (var item in response.Data)
            {
                Items.Add(item);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Error loading items: {response.Message}");
        }
    }

    private ItemInfo? GetItemFromParameter(object? parameter)
    {
        return parameter switch
        {
            GridRecordContextFlyoutInfo flyoutInfo => flyoutInfo.Record as ItemInfo,
            ItemInfo directItem => directItem,
            _ => null
        };
    }

    private async void OnAddToCart(object? parameter)
    {
        if (GetItemFromParameter(parameter) is ItemInfo item && AddToCartRequested != null)
        {
            await AddToCartRequested.Invoke(item);
        }
    }
}