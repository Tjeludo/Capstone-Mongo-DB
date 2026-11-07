using App_v1.Helpers;
using App_v1.Models;
using App_v1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace App_v1.Pages;

public sealed partial class SupplierProductPage : Page
{
    private SupplierProductViewModel ViewModel => (SupplierProductViewModel)DataContext;

    public SupplierProductPage()
    {
        this.InitializeComponent();
        this.Loaded += SupplierProductPage_Loaded;
    }

    private async void SupplierProductPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Subscribe to ViewModel events here
        ViewModel.EditRequested += OnEditRequested;
        ViewModel.DeleteRequested += OnDeleteRequested;

        // Get the selected supplier ID from shared state and load data
        await ViewModel.LoadSupplierProductsAsync(AppState.SelectedSupplierId);
    }

    private async void EditProduct_Click(object sender, RoutedEventArgs _)
    {
        if (sender is Button { Tag: SupplierProductInfo product })
        {
            await OnEditRequested(product);
        }
    }

    private async void DeleteProduct_Click(object sender, RoutedEventArgs _)
    {
        if (sender is Button { Tag: SupplierProductInfo product })
        {
            await OnDeleteRequested(product);
        }
    }

    private async Task OnEditRequested(SupplierProductInfo product)
    {
        // Pre-fill the edit dialog with current data
        EditSupplierIdTextBox.Text = product.SupplierId ?? string.Empty;
        EditProductCodeTextBox.Text = product.ProductCode ?? string.Empty;
        EditDescriptionTextBox.Text = product.Description ?? string.Empty;
        EditPriceNumberBox.Value = product.Price;

        EditProductDialog.XamlRoot = this.XamlRoot;
        var result = await EditProductDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(EditDescriptionTextBox.Text) || EditPriceNumberBox.Value < 0)
            {
                await ShowErrorDialog("Please enter a valid description and a non-negative price.");
                return;
            }

            // Update product with new values
            product.Price = EditPriceNumberBox.Value;
            product.Description = EditDescriptionTextBox.Text.Trim();

            // Call the ViewModel to update via API
            var (success, message) = await ViewModel.UpdateSupplierProductAsync(product);
            if (success)
            {
                await ShowSuccessDialog(message);
            }
            else
            {
                await ShowErrorDialog(message);
            }
        }
    }

    private async Task OnDeleteRequested(SupplierProductInfo product)
    {
        // Show confirmation dialog
        var dialog = new ContentDialog
        {
            Title = "Confirm Delete",
            Content = $"Are you sure you want to delete the product '{product.ProductCode}' from supplier '{product.SupplierId}'?\nThis action cannot be undone.",
            PrimaryButtonText = "Delete",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var (success, message) = await ViewModel.DeleteSupplierProductAsync(product.SupplierId, product.ProductCode);
            if (success)
            {
                await ShowSuccessDialog(message);
            }
            else
            {
                await ShowErrorDialog(message);
            }
        }
    }

    private async void DownloadCSV_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? currentSupplierId = AppState.SelectedSupplierId;
            if (string.IsNullOrEmpty(currentSupplierId))
            {
                await ShowErrorDialog("No supplier is selected.");
                return;
            }

            ShowDownloadBusyState(true);
            var csvData = await ViewModel.DownloadCsvAsync(currentSupplierId);

            if (csvData is { Length: > 0 })
            {
                var savePicker = new FileSavePicker();
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance));
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV Files", [".csv"]);
                savePicker.SuggestedFileName = $"price_update_{currentSupplierId}_{DateTime.Now:dd_MM_yyyy}.csv";

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteBytesAsync(file, csvData);
                    await ShowSuccessDialog("Download successful.");
                }
            }
            else
            {
                await ShowErrorDialog("No product data available to download for this supplier.");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error downloading CSV: {ex.Message}");
        }
        finally
        {
            ShowDownloadBusyState(false);
        }
    }

    private async void UploadCSV_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance));
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".csv");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                ShowUploadBusyState(true);
                try
                {
                    var (success, message) = await ViewModel.UploadCsvAsync(file, AppState.SelectedSupplierId);
                    if (success)
                    {
                        await ShowSuccessDialog(message);
                    }
                    else
                    {
                        await ShowErrorDialog(message);
                    }
                }
                finally
                {
                    ShowUploadBusyState(false);
                }
            }
        }
        catch (Exception ex)
        {
            ShowUploadBusyState(false);
            await ShowErrorDialog($"Error uploading CSV: {ex.Message}");
        }
    }

    private void ShowDownloadBusyState(bool isBusy)
    {
        DownloadButton.IsEnabled = !isBusy;
        DownloadContent.Visibility = isBusy ? Visibility.Collapsed : Visibility.Visible;
        DownloadBusyIndicator.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        DownloadBusyIndicator.IsActive = isBusy;
    }

    private void ShowUploadBusyState(bool isBusy)
    {
        UploadButton.IsEnabled = !isBusy;
        UploadContent.Visibility = isBusy ? Visibility.Collapsed : Visibility.Visible;
        UploadBusyIndicator.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        UploadBusyIndicator.IsActive = isBusy;
    }

    private async Task ShowSuccessDialog(string message)
    {
        var dialog = new ContentDialog { Title = "Success", Content = message, CloseButtonText = "OK", XamlRoot = this.XamlRoot };
        await dialog.ShowAsync();
    }

    private async Task ShowErrorDialog(string message)
    {
        var dialog = new ContentDialog { Title = "Error", Content = message, CloseButtonText = "OK", XamlRoot = this.XamlRoot };
        await dialog.ShowAsync();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.EditRequested -= OnEditRequested;
            ViewModel.DeleteRequested -= OnDeleteRequested;
        }
        this.Loaded -= SupplierProductPage_Loaded;
    }
}