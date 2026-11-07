/*
This is the SupplierProductViewModel. Its main jobs are to hold the UI's state
(like the list of products) and to handle user interactions. All the heavy
lifting, especially API calls, is delegated to the SupplierProductApiService.
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
using Windows.Storage;

namespace App_v1.ViewModels;

public class SupplierProductViewModel
{
    private readonly SupplierProductApiService _apiService;

    public ObservableCollection<SupplierProductInfo> SupplierProducts
    {
        get; set;
    }

    public ICommand GridEditCommand
    {
        get;
    }
    public ICommand GridDeleteCommand
    {
        get;
    }

    // New properties for button visibility
    public bool CanEditProduct => UserSession.HasPermission("edit_products");
    public bool CanDeleteProduct => UserSession.HasPermission("delete_products");
    public bool CanDownloadCsv => UserSession.HasPermission("download_products");
    public bool CanUploadCsv => UserSession.HasPermission("upload_products");


    /*
    These events allow the ViewModel to ask the View (the code-behind)
    to perform UI-specific actions, like showing a dialog window.
    */
    public event Func<SupplierProductInfo, Task>? EditRequested;
    public event Func<SupplierProductInfo, Task>? DeleteRequested;

    public SupplierProductViewModel()
    {
        _apiService = new SupplierProductApiService();
        SupplierProducts = new ObservableCollection<SupplierProductInfo>();

        // Data is now loaded by the page with a specific supplier ID when it loads.
        // _ = LoadSupplierProductsAsync(); // <-- This line is removed.

        GridEditCommand = new RelayCommand<object?>(OnEdit);
        GridDeleteCommand = new RelayCommand<object?>(OnDelete);
    }

    public async Task LoadSupplierProductsAsync(string? supplierId)
    {
        if (string.IsNullOrEmpty(supplierId))
        {
            SupplierProducts.Clear();
            System.Diagnostics.Debug.WriteLine("No supplier ID provided; clearing products.");
            return;
        }

        var response = await _apiService.GetSupplierProductsAsync(supplierId);

        SupplierProducts.Clear();
        if (response.Success && response.Data != null)
        {
            foreach (var product in response.Data)
            {
                SupplierProducts.Add(product);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Error loading supplier products: {response.Message}");
        }
    }

    public async Task<(bool success, string message)> UpdateSupplierProductAsync(SupplierProductInfo product)
    {
        var response = await _apiService.UpdateSupplierProductAsync(product);
        if (response.Success)
        {
            // Reload to show the changes for the current supplier.
            await LoadSupplierProductsAsync(product.SupplierId);
        }
        return (response.Success, response.Message);
    }

    public async Task<(bool success, string message)> DeleteSupplierProductAsync(string? supplierId, string? productCode)
    {
        if (string.IsNullOrEmpty(supplierId) || string.IsNullOrEmpty(productCode))
        {
            return (false, "Supplier ID or Product Code cannot be null.");
        }

        var response = await _apiService.DeleteSupplierProductAsync(supplierId, productCode);
        if (response.Success)
        {
            // For a faster UI update, remove the item directly from the local collection.
            var productToRemove = SupplierProducts.FirstOrDefault(p => p.SupplierId == supplierId && p.ProductCode == productCode);
            if (productToRemove != null)
            {
                SupplierProducts.Remove(productToRemove);
            }
        }
        return (response.Success, response.Message);
    }

    public async Task<byte[]?> DownloadCsvAsync(string? supplierId)
    {
        if (string.IsNullOrEmpty(supplierId)) return null;
        return await _apiService.DownloadCsvAsync(supplierId);
    }

    // The signature was already correct, this just confirms it calls the updated service method
    public async Task<(bool success, string message)> UploadCsvAsync(StorageFile file, string? currentSupplierId)
    {
        if (string.IsNullOrEmpty(currentSupplierId))
        {
            return (false, "Cannot upload file without a selected supplier.");
        }

        var response = await _apiService.UploadCsvAsync(file, currentSupplierId);
        if (response.Success)
        {
            await LoadSupplierProductsAsync(currentSupplierId); // Reload the list after a successful upload.
        }
        return (response.Success, response.Message);
    }

    private static SupplierProductInfo? GetProductFromParameter(object? parameter)
    {
        return parameter switch
        {
            GridRecordContextFlyoutInfo flyoutInfo => flyoutInfo.Record as SupplierProductInfo,
            SupplierProductInfo directProduct => directProduct,
            _ => null
        };
    }

    private async void OnEdit(object? parameter)
    {
        if (GetProductFromParameter(parameter) is { } product)
        {
            await EditRequested?.Invoke(product);
        }
    }

    private async void OnDelete(object? parameter)
    {
        if (GetProductFromParameter(parameter) is { } product)
        {
            await DeleteRequested?.Invoke(product);
        }
    }
}