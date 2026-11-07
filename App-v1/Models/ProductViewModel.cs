using App_v1.Models;
using App_v1.Services;
using CommunityToolkit.Mvvm.Input;
using Syncfusion.UI.Xaml.DataGrid;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;

namespace App_v1.ViewModels;

/*
  This is the "brain" for the ProductsPage.
  
  It's responsible for:
  - Holding the list of products and categories.
  - Exposing commands (like edit or delete) that the UI can bind to.
  - Talking to the ApiService to get or change data.
  - Checking user permissions to control what the user can see and do.
*/
public class ProductViewModel
{
    // My connection to the backend API.
    private readonly ProductApiService _productApiService;

    // These collections hold the data for the page.
    // ObservableCollection is special because the UI automatically updates when I add or remove items.
    public ObservableCollection<ProductInfo> Products
    {
        get; set;
    }
    public ObservableCollection<CategoryInfo> Categories
    {
        get; set;
    }

    // These are the commands that buttons on the UI will call.
    public ICommand GridEditCommand
    {
        get;
    }
    public ICommand GridDeleteCommand
    {
        get;
    }

    /*
      These bool properties are for controlling UI visibility.
      A button's `Visibility` can be bound to these, so it only shows up
      if the logged-in user has the right permission.
    */
    public bool CanAddProduct => UserSession.HasPermission("add_products");
    public bool CanEditProduct => UserSession.HasPermission("edit_products");
    public bool CanDeleteProduct => UserSession.HasPermission("delete_products");
    public bool CanDownloadCsv => UserSession.HasPermission("download_products");
    public bool CanUploadCsv => UserSession.HasPermission("upload_products");

    // These events let the ViewModel tell the View (the Page) to do something, like open a dialog.
    // This keeps the ViewModel from knowing directly about UI elements.
    public event Func<ProductInfo, Task>? EditRequested;
    public event Func<ProductInfo, Task>? DeleteRequested;

    // Constructor: This runs when the page is created.
    public ProductViewModel()
    {
        _productApiService = new ProductApiService();
        Products = new ObservableCollection<ProductInfo>();
        Categories = new ObservableCollection<CategoryInfo>();

        // Fire-and-forget task to load data in the background without freezing the UI.
        _ = LoadInitialDataAsync();

        // Set up the commands.
        GridEditCommand = new RelayCommand<object?>(OnEdit);
        GridDeleteCommand = new RelayCommand<object?>(OnDelete);
    }

    // Kicks off the initial data loading. I load categories first, then products.
    private async Task LoadInitialDataAsync()
    {
        await LoadCategoriesAsync();
        await LoadProductsAsync();
    }

    // Asks the API service for the latest list of products.
    public async Task LoadProductsAsync()
    {
        var response = await _productApiService.GetProductsAsync();
        if (response.Success && response.Data != null)
        {
            Products.Clear();
            foreach (var product in response.Data)
            {
                Products.Add(product);
            }
        }
        else
        {
            Debug.WriteLine($"Error loading products: {response.Message}");
        }
    }

    // Gets the categories, which I need for the 'Add Product' dropdown.
    public async Task LoadCategoriesAsync()
    {
        var response = await _productApiService.GetCategoriesAsync();
        if (response.Success && response.Data != null)
        {
            Categories.Clear();
            foreach (var category in response.Data)
            {
                Categories.Add(category);
            }
        }
        else
        {
            Debug.WriteLine($"Error loading categories: {response.Message}");
        }
    }

    // Sends a new product to the API. If it works, I reload the grid.
    public async Task<(bool success, string message)> AddProductAsync(ProductInfo newProduct)
    {
        var response = await _productApiService.AddProductAsync(newProduct);
        if (response.Success)
        {
            await LoadProductsAsync();
        }
        return (response.Success, response.Message);
    }

    // Sends an updated product to the API.
    public async Task<(bool success, string message)> UpdateProductAsync(ProductInfo product)
    {
        var response = await _productApiService.UpdateProductAsync(product);
        if (response.Success)
        {
            await LoadProductsAsync();
        }
        return (response.Success, response.Message);
    }

    // Tells the API to delete a product.
    public async Task<(bool success, string message)> DeleteProductAsync(string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            return (false, "Product ID cannot be null.");
        }

        var response = await _productApiService.DeleteProductAsync(productId);
        if (response.Success)
        {
            // Instead of a full reload, just remove the item from the local list. It's faster.
            var productToRemove = Products.FirstOrDefault(p => p.Id == productId);
            if (productToRemove != null)
            {
                Products.Remove(productToRemove);
            }
        }
        return (response.Success, response.Message);
    }

    // Asks the service for the raw CSV file data.
    public async Task<byte[]?> DownloadCsvAsync()
    {
        return await _productApiService.DownloadCsvAsync();
    }

    // Sends a file to the service for uploading.
    public async Task<(bool success, string message)> UploadCsvAsync(StorageFile file)
    {
        var response = await _productApiService.UploadCsvAsync(file);
        if (response.Success)
        {
            await LoadProductsAsync(); // Reload the grid to show the imported data.
        }
        return (response.Success, response.Message);
    }

    // These methods are called by the RelayCommands when a button is clicked.
    // They figure out which product was clicked and then raise an event for the Page to handle.
    private async void OnEdit(object? parameter)
    {
        ProductInfo? product = (parameter as GridRecordContextFlyoutInfo)?.Record as ProductInfo ?? parameter as ProductInfo;
        if (product != null && EditRequested != null)
        {
            await EditRequested.Invoke(product);
        }
    }

    private async void OnDelete(object? parameter)
    {
        ProductInfo? product = (parameter as GridRecordContextFlyoutInfo)?.Record as ProductInfo ?? parameter as ProductInfo;
        if (product != null && DeleteRequested != null)
        {
            await DeleteRequested.Invoke(product);
        }
    }
}