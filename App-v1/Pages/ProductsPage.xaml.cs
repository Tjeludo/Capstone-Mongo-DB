using App_v1.Helpers;
using App_v1.Models;
using App_v1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace App_v1.Pages;

/*
  Handles user interaction for the Products page.
  This is where I wire up button clicks, manage dialogs,
  and kick off validation or file operations.
*/
public sealed partial class ProductsPage : Page
{
    // A little shortcut to get the ViewModel.
    private ProductViewModel ViewModel => (ProductViewModel)DataContext;

    public ProductsPage()
    {
        this.InitializeComponent();
        // Listen for requests from the ViewModel to show the edit or delete dialogs.
        ViewModel.EditRequested += OnEditRequested;
        ViewModel.DeleteRequested += OnDeleteRequested;
    }

    // Fired when the main "Add Product" button is clicked.
    private async void AddProduct_Click(object sender, RoutedEventArgs e)
    {
        // Can't add a product if there are no categories to put it in.
        if (ViewModel.Categories == null || !ViewModel.Categories.Any())
        {
            ShowNotification(NotificationInfoBar, "Error", "No categories available. Please add categories first.", InfoBarSeverity.Error);
            return;
        }

        // Reset the form to a clean state every time the dialog is opened.
        ProductNameTextBox.Text = string.Empty;
        ProductCategoryComboBox.SelectedIndex = -1;
        ProductSizeComboBox.SelectedIndex = -1;
        ProductColorComboBox.SelectedIndex = -1;
        ProductPriceNumberBox.Value = 1;

        // Check if the form is valid in its cleared state to set the save button correctly.
        ValidateAddForm();

        AddProductDialog.XamlRoot = this.XamlRoot;
        await AddProductDialog.ShowAsync();
    }

    // Fired when the "Save" button in the "Add Product" dialog is clicked.
    private async void AddProductDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Keep the dialog open while we run async validation and API calls.
        var deferral = args.GetDeferral();
        args.Cancel = true;

        // Use the central Validator to check the name.
        string validatedName = Validator.ValidateProductName(ProductNameTextBox.Text, NotificationInfoBar);
        if (validatedName == null)
        {
            deferral.Complete();
            return; // Name is invalid, so we stop here.
        }

        // Check the other fields, including price, on save.
        if (ProductCategoryComboBox.SelectedItem == null || ProductSizeComboBox.SelectedItem == null || ProductColorComboBox.SelectedItem == null || double.IsNaN(ProductPriceNumberBox.Value))
        {
            ShowNotification(NotificationInfoBar, "Validation Error", "Please ensure all dropdowns are selected and the price is valid.", InfoBarSeverity.Error);
            deferral.Complete();
            return;
        }

        var price = (decimal)ProductPriceNumberBox.Value;

        // If validation passes, assemble the new product object.
        var newProduct = new ProductInfo
        {
            Name = validatedName,
            Category = (ProductCategoryComboBox.SelectedItem as CategoryInfo)?.Id ?? string.Empty,
            Price = price,
            Size = (ProductSizeComboBox.SelectedItem as string) ?? string.Empty,
            Colour = (ProductColorComboBox.SelectedItem as string) ?? string.Empty,
        };

        // Pass it to the ViewModel to handle the API call.
        var (success, message) = await ViewModel.AddProductAsync(newProduct);
        if (success)
        {
            ShowNotification(NotificationInfoBar, "Success", message, InfoBarSeverity.Success);
            args.Cancel = false; // It worked! Let the dialog close.
        }
        else
        {
            ShowNotification(NotificationInfoBar, "Error", message, InfoBarSeverity.Error);
        }
        deferral.Complete();
    }

    // Fired by the "Edit" button in a grid row.
    private async void EditProduct_Click(object sender, RoutedEventArgs e)
    {
        // The product to edit is stored in the button's Tag property.
        if (sender is Button { Tag: ProductInfo product })
        {
            await OnEditRequested(product);
        }
    }

    // This sets up and shows the "Edit Product" dialog.
    private async Task OnEditRequested(ProductInfo product)
    {

        // Stash the product object in the dialog's Tag so we can get it back later.
        EditProductDialog.Tag = product;

        // Fill the form fields with the data from the selected product.
        EditProductIdTextBox.Text = product.Id;
        EditProductNameTextBox.Text = product.Name;
        EditProductCategoryTextBox.Text = ViewModel.Categories.FirstOrDefault(c => c.Id == product.Category)?.Name ?? "N/A";
        EditProductPriceNumberBox.Value = (double)product.Price;
        EditProductSizeTextBox.Text = product.Size;
        EditProductColorTextBox.Text = product.Colour;

        ValidateEditForm();

        EditProductDialog.XamlRoot = this.XamlRoot;
        await EditProductDialog.ShowAsync();
    }

    // Fired when the "Update" button in the "Edit" dialog is clicked.
    private async void EditProductDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        args.Cancel = true;

        // The product name is read-only, so no need to validate or update it.

        // Validate price on save.
        if (double.IsNaN(EditProductPriceNumberBox.Value))
        {
            ShowNotification(NotificationInfoBar, "Validation Error", "Please ensure the price is a valid number.", InfoBarSeverity.Error);
            deferral.Complete();
            return;
        }
        var price = (decimal)EditProductPriceNumberBox.Value;

        // Retrieve the original product from the Tag.
        if (sender.Tag is ProductInfo product)
        {
            // Update its properties with the new, validated values.
            product.Price = price;

            var (success, message) = await ViewModel.UpdateProductAsync(product);
            if (success)
            {
                ShowNotification(NotificationInfoBar, "Success", message, InfoBarSeverity.Success);
                args.Cancel = false; // Success, so close the dialog.
            }
            else
            {
                ShowNotification(NotificationInfoBar, "Error", message, InfoBarSeverity.Error);
            }
        }
        deferral.Complete();
    }

    // Fired by the "Delete" button in a grid row.
    private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ProductInfo product })
        {
            await OnDeleteRequested(product);
        }
    }

    // Shows the "Are you sure?" confirmation before deleting.
    private async Task OnDeleteRequested(ProductInfo product)
    {
        var dialog = new ContentDialog
        {
            Title = "Confirm Delete",
            Content = $"Are you sure you want to delete the product '{product.Name}'?\n\nThis action cannot be undone.",
            PrimaryButtonText = "Delete",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        // Only delete if the user explicitly clicks the "Delete" button.
        if (result == ContentDialogResult.Primary)
        {
            var (success, message) = await ViewModel.DeleteProductAsync(product.Id);
            if (success) ShowNotification(NotificationInfoBar, "Success", message, InfoBarSeverity.Success);
            else ShowNotification(NotificationInfoBar, "Error", message, InfoBarSeverity.Error);
        }
    }

    // These handlers trigger validation checks in real-time as the user types or selects values.
    private void AddForm_InputChanged(object sender, object e) => ValidateAddForm();
    private void EditForm_InputChanged(object sender, object e) => ValidateEditForm();

    // Enables or disables the Save button based on whether the form is valid.
    private void ValidateAddForm()
    {
        AddProductDialog.IsPrimaryButtonEnabled = AreAddFormFieldsValid(out _);
    }

    // Enables or disables the Update button.
    private void ValidateEditForm()
    {
        EditProductDialog.IsPrimaryButtonEnabled = AreEditFormFieldsValid(out _);
    }

    // The actual logic to check if the Add form is valid.
    private bool AreAddFormFieldsValid(out decimal price)
    {
        price = 0.0m;
        bool isPriceValid = !double.IsNaN(ProductPriceNumberBox.Value);
        if (isPriceValid)
        {
            price = (decimal)ProductPriceNumberBox.Value;
        }

        // All fields must be filled.
        return !string.IsNullOrWhiteSpace(ProductNameTextBox.Text) &&
               ProductCategoryComboBox.SelectedItem != null &&
               ProductSizeComboBox.SelectedItem != null &&
               ProductColorComboBox.SelectedItem != null;
    }

    // The logic to check if the Edit form is valid.
    private bool AreEditFormFieldsValid(out decimal price)
    {
        price = 0.0m;
        if (!double.IsNaN(EditProductPriceNumberBox.Value))
        {
            price = (decimal)EditProductPriceNumberBox.Value;
        }

        // The name field is read-only, and price validation happens on save.
        // Therefore, the Update button can always be enabled.
        return true;
    }

    // Handles the CSV download process.
    private async void DownloadCSV_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetButtonBusyState(DownloadButton, DownloadContent, DownloadBusyIndicator, true);
            var csvData = await ViewModel.DownloadCsvAsync();
            if (csvData is { Length: > 0 })
            {
                var savePicker = new FileSavePicker();
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance));
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV Files", new List<string>() { ".csv" });
                savePicker.SuggestedFileName = $"products_{DateTime.Now:dd_MM_yyyy_HHmmss}.csv";

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteBytesAsync(file, csvData);
                    ShowNotification(NotificationInfoBar, "Download Complete", "The CSV file has been saved successfully.", InfoBarSeverity.Success);
                }
            }
            else
            {
                ShowNotification(NotificationInfoBar, "No Data", "No data available to download.", InfoBarSeverity.Warning);
            }
        }
        catch (Exception ex)
        {
            ShowNotification(NotificationInfoBar, "Download Error", $"An error occurred: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            SetButtonBusyState(DownloadButton, DownloadContent, DownloadBusyIndicator, false);
        }
    }

    // Handles the CSV upload process.
    private async void UploadCSV_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance));
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".csv");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var fileProperties = await file.GetBasicPropertiesAsync();
                if (fileProperties.Size > 10 * 1024 * 1024) // 10MB limit
                {
                    ShowNotification(NotificationInfoBar, "File Too Large", "Please select a file smaller than 10MB.", InfoBarSeverity.Error);
                    return;
                }

                SetButtonBusyState(UploadButton, UploadContent, UploadBusyIndicator, true);
                try
                {
                    var (success, message) = await ViewModel.UploadCsvAsync(file);
                    if (success) ShowNotification(NotificationInfoBar, "Upload Complete", message, InfoBarSeverity.Success);
                    else ShowNotification(NotificationInfoBar, "Upload Error", message, InfoBarSeverity.Error);
                }
                finally
                {
                    SetButtonBusyState(UploadButton, UploadContent, UploadBusyIndicator, false);
                }
            }
        }
        catch (Exception ex)
        {
            SetButtonBusyState(UploadButton, UploadContent, UploadBusyIndicator, false);
            ShowNotification(NotificationInfoBar, "Error", $"An error occurred: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    // A helper to show a notification that goes away on its own.
    private void ShowNotification(InfoBar infoBar, string title, string message, InfoBarSeverity severity)
    {
        infoBar.Title = title;
        infoBar.Message = message;
        infoBar.Severity = severity;
        infoBar.IsOpen = true;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += (s, e) =>
        {
            infoBar.IsOpen = false;
            timer.Stop();
        };
        timer.Start();
    }

    // A helper to swap a button's content with a loading ring.
    private void SetButtonBusyState(Button button, FrameworkElement content, ProgressRing indicator, bool isBusy)
    {
        button.IsEnabled = !isBusy;
        content.Visibility = isBusy ? Visibility.Collapsed : Visibility.Visible;
        indicator.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        indicator.IsActive = isBusy;
    }

    // Clean up event handlers when I navigate away from the page to prevent memory leaks.
    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.EditRequested -= OnEditRequested;
            ViewModel.DeleteRequested -= OnDeleteRequested;
        }
    }
}

