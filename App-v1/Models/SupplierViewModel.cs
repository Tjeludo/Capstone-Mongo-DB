using App_v1.Models;
using App_v1.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace App_v1.ViewModels;

/// <summary>
/// ViewModel for SuppliersPage.
/// Manages data and logic between the UI and API service.
/// </summary>
public class SupplierViewModel
{
    /* API service for supplier operations */
    private readonly SupplierApiService _supplierApiService;

    /* Observable collection bound to the DataGrid in the UI */
    public ObservableCollection<SupplierInfo> Suppliers
    {
        get; set;
    }

    /*
      Permission properties controlling button visibility in XAML.
      Bound directly to UI elements.
    */
    public bool CanAddSupplier => UserSession.HasPermission("add_suppliers");
    public bool CanEditSupplier => UserSession.HasPermission("edit_suppliers");
    public bool CanDeleteSupplier => UserSession.HasPermission("delete_suppliers");
    public bool CanViewSupplier => UserSession.HasPermission("view_suppliers");
    public bool CanDownloadCsv => UserSession.HasPermission("download_suppliers");
    public bool CanUploadCsv => UserSession.HasPermission("upload_suppliers");

    /*
      Events used to notify the View (code-behind) when
      an action like view, edit, or delete is requested.
    */
    public event Func<SupplierInfo, Task>? ViewRequested;
    public event Func<SupplierInfo, Task>? EditRequested;
    public event Func<SupplierInfo, Task>? DeleteRequested;

    /* Constructor initializes API service, collection, and loads suppliers */
    public SupplierViewModel()
    {
        _supplierApiService = new SupplierApiService();
        Suppliers = new ObservableCollection<SupplierInfo>();
        _ = LoadSuppliersAsync(); // Fire-and-forget to populate UI
    }

    /* Fetches suppliers from API and updates the collection */
    public async Task LoadSuppliersAsync()
    {
        var response = await _supplierApiService.GetSuppliersAsync();
        if (response.Success && response.Data != null)
        {
            Suppliers.Clear();
            foreach (var supplier in response.Data)
                Suppliers.Add(supplier);
        }
        else
        {
            Debug.WriteLine($"Failed to load suppliers: {response.Message}");
        }
    }

    /* Adds a new supplier via API and refreshes the grid */
    public async Task<(bool success, string message)> AddSupplierAsync(SupplierInfo newSupplier)
    {
        var response = await _supplierApiService.AddSupplierAsync(newSupplier);
        if (response.Success)
            await LoadSuppliersAsync(); // Refresh collection
        return (response.Success, response.Message);
    }

    /* Updates an existing supplier via API and refreshes the grid */
    public async Task<(bool success, string message)> UpdateSupplierAsync(SupplierInfo supplier)
    {
        var response = await _supplierApiService.UpdateSupplierAsync(supplier);
        if (response.Success)
            await LoadSuppliersAsync(); // Refresh collection
        return (response.Success, response.Message);
    }

    /* Deletes a supplier via API and removes it locally for faster UI update */
    public async Task<(bool success, string message)> DeleteSupplierAsync(string? supplierId)
    {
        if (string.IsNullOrEmpty(supplierId))
            return (false, "Supplier ID was not provided.");

        var response = await _supplierApiService.DeleteSupplierAsync(supplierId);
        if (response.Success)
        {
            var supplierToRemove = Suppliers.FirstOrDefault(s => s.ID == supplierId);
            if (supplierToRemove != null)
                Suppliers.Remove(supplierToRemove); // Update UI instantly
        }
        return (response.Success, response.Message);
    }

    /* Requests CSV data from API */
    public async Task<byte[]?> DownloadCsvAsync()
        => await _supplierApiService.DownloadCsvAsync();

    /* Uploads CSV file to API and refreshes the grid on success */
    public async Task<(bool success, string message)> UploadCsvAsync(StorageFile file)
    {
        var response = await _supplierApiService.UploadCsvAsync(file);
        if (response.Success)
            await LoadSuppliersAsync(); // Refresh collection
        return (response.Success, response.Message);
    }
}
