using App_v1.Helpers;
using App_v1.Models;
using App_v1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace App_v1.Pages;

/// <summary>
/// Handles user interactions on the SuppliersPage.xaml.
/// Manages dialogs, form validation, and CSV operations.
/// </summary>
public sealed partial class SuppliersPage : Page
{
    // Shortcut to access the ViewModel from DataContext
    private SupplierViewModel ViewModel => (SupplierViewModel)DataContext;

    public SuppliersPage()
    {
        this.InitializeComponent();

        /* Subscribe to ViewModel events.
           This allows the ViewModel to trigger dialogs or navigation without
           tight coupling to the page's UI. */
        ViewModel.ViewRequested += OnViewRequested;
        ViewModel.EditRequested += OnEditRequested;
        ViewModel.DeleteRequested += OnDeleteRequested;
    }

    // --- DataGrid row button click handlers ---
    private async void ViewSupplier_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SupplierInfo supplier })
            await OnViewRequested(supplier);
    }

    private async void EditSupplier_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SupplierInfo supplier })
            await OnEditRequested(supplier);
    }

    private async void DeleteSupplier_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SupplierInfo supplier })
            await OnDeleteRequested(supplier);
    }

    /* Opens the Add Supplier dialog and resets all input fields */
    private async void AddSupplier_Click(object sender, RoutedEventArgs e)
    {
        NotificationInfoBar.IsOpen = false;

        // Clear form fields
        SupplierNameTextBox.Text = string.Empty;
        SupplierEmailTextBox.Text = string.Empty;
        SupplierCountryCodeComboBox.SelectedIndex = -1;
        SupplierPhoneTextBox.Text = string.Empty;
        SupplierStreetTextBox.Text = string.Empty;
        SupplierSuburbTextBox.Text = string.Empty;
        SupplierCityTextBox.Text = string.Empty;
        SupplierCountryTextBox.Text = string.Empty;
        SupplierZipCodeTextBox.Text = string.Empty;
        SupplierSalesRepTextBox.Text = string.Empty;

        ValidateAddForm();

        AddSupplierDialog.XamlRoot = this.XamlRoot;
        await AddSupplierDialog.ShowAsync();
    }

    /* Handles 'Save' in Add Supplier dialog with validation and API call */
    private async void AddSupplierDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        args.Cancel = true; // Prevent closing by default

        /* Validate each input field. NotificationInfoBar shows error messages */
        var validatedName = Validator.ValidateSupplierName(SupplierNameTextBox.Text, NotificationInfoBar);
        if (validatedName == null) { deferral.Complete(); return; }

        var validatedEmail = Validator.ValidateEmail(SupplierEmailTextBox.Text, NotificationInfoBar);
        if (validatedEmail == null) { deferral.Complete(); return; }

        var validatedPhone = Validator.ValidatePhoneNumber(SupplierPhoneTextBox.Text, NotificationInfoBar);
        if (validatedPhone == null) { deferral.Complete(); return; }

        var validatedStreet = Validator.ValidateStreet(SupplierStreetTextBox.Text, NotificationInfoBar);
        if (validatedStreet == null) { deferral.Complete(); return; }

        var validatedCity = Validator.ValidateCity(SupplierCityTextBox.Text, NotificationInfoBar);
        if (validatedCity == null) { deferral.Complete(); return; }

        var validatedCountry = Validator.ValidateCountry(SupplierCountryTextBox.Text, NotificationInfoBar);
        if (validatedCountry == null) { deferral.Complete(); return; }

        var validatedZipCode = Validator.ValidateZipCode(SupplierZipCodeTextBox.Text, NotificationInfoBar);
        if (validatedZipCode == null) { deferral.Complete(); return; }

        var validatedSalesRep = Validator.ValidateSalesRep(SupplierSalesRepTextBox.Text, NotificationInfoBar);
        if (validatedSalesRep == null) { deferral.Complete(); return; }

        /* Assemble new supplier object after validation */
        var countryCode = (SupplierCountryCodeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        var fullPhoneNumber = $"{countryCode}{validatedPhone}";

        string[] addressParts = {
            validatedStreet,
            SupplierSuburbTextBox.Text.Trim(),
            validatedCity,
            validatedCountry,
            validatedZipCode
        };
        var fullAddress = string.Join(", ", addressParts.Where(part => !string.IsNullOrWhiteSpace(part)));

        var newSupplier = new SupplierInfo
        {
            Name = validatedName,
            Email = validatedEmail,
            PhoneNumber = fullPhoneNumber,
            Address = fullAddress,
            SalesRep = validatedSalesRep
        };

        // Send new supplier to ViewModel for API processing
        var (success, message) = await ViewModel.AddSupplierAsync(newSupplier);
        if (success)
        {
            ShowNotification(NotificationInfoBar, "Success", message, InfoBarSeverity.Success);
            args.Cancel = false; // Close dialog on success
        }
        else
        {
            ShowNotification(NotificationInfoBar, "Error", message, InfoBarSeverity.Error);
        }

        deferral.Complete();
    }

    // --- Navigation and Edit/Delete handlers ---
    private async Task OnViewRequested(SupplierInfo supplier)
    {
        AppState.SelectedSupplierId = supplier.ID;
        this.Frame.Navigate(typeof(SupplierProductPage));
        await Task.CompletedTask;
    }

    /* Opens Edit Supplier dialog pre-filled with existing data */
    private async Task OnEditRequested(SupplierInfo supplier)
    {
        NotificationInfoBar.IsOpen = false;
        EditSupplierDialog.Tag = supplier; // Store original supplier object

        // Pre-fill form
        EditSupplierIdTextBox.Text = supplier.ID;
        EditSupplierNameTextBox.Text = supplier.Name;
        EditSupplierEmailTextBox.Text = supplier.Email;
        EditSupplierSalesRepTextBox.Text = supplier.SalesRep;

        /* Parse phone number to separate country code */
        var phone = supplier.PhoneNumber ?? "";
        if (phone.StartsWith("+27"))
        {
            EditSupplierCountryCodeComboBox.SelectedIndex = 0;
            EditSupplierPhoneTextBox.Text = phone.Substring(3);
        }
        else if (phone.StartsWith("+263"))
        {
            EditSupplierCountryCodeComboBox.SelectedIndex = 1;
            EditSupplierPhoneTextBox.Text = phone.Substring(4);
        }
        else
        {
            EditSupplierCountryCodeComboBox.SelectedIndex = -1;
            EditSupplierPhoneTextBox.Text = phone;
        }

        EditSupplierAddressTextBox.Text = supplier.Address ?? "";
        ValidateEditForm();

        EditSupplierDialog.XamlRoot = this.XamlRoot;
        await EditSupplierDialog.ShowAsync();
    }

    /* Handles 'Save' in Edit Supplier dialog */
    private async void EditSupplierDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        args.Cancel = true; // Prevent closing by default

        if (sender.Tag is SupplierInfo supplier)
        {
            // Validate all fields
            var validatedName = Validator.ValidateSupplierName(EditSupplierNameTextBox.Text, NotificationInfoBar);
            if (validatedName == null) { deferral.Complete(); return; }

            var validatedEmail = Validator.ValidateEmail(EditSupplierEmailTextBox.Text, NotificationInfoBar);
            if (validatedEmail == null) { deferral.Complete(); return; }

            var validatedPhone = Validator.ValidatePhoneNumber(EditSupplierPhoneTextBox.Text, NotificationInfoBar);
            if (validatedPhone == null) { deferral.Complete(); return; }

            var validatedAddress = Validator.ValidateAddress(EditSupplierAddressTextBox.Text, NotificationInfoBar);
            if (validatedAddress == null) { deferral.Complete(); return; }

            var validatedSalesRep = Validator.ValidateSalesRep(EditSupplierSalesRepTextBox.Text, NotificationInfoBar);
            if (validatedSalesRep == null) { deferral.Complete(); return; }

            // Update supplier object
            var countryCode = (EditSupplierCountryCodeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            var fullPhoneNumber = string.IsNullOrWhiteSpace(validatedPhone) ? "" : $"{countryCode}{validatedPhone}";

            supplier.Name = validatedName;
            supplier.Email = validatedEmail;
            supplier.PhoneNumber = fullPhoneNumber;
            supplier.Address = validatedAddress;
            supplier.SalesRep = validatedSalesRep;

            // Send update to ViewModel
            var (success, message) = await ViewModel.UpdateSupplierAsync(supplier);
            if (success)
            {
                ShowNotification(NotificationInfoBar, "Success", message, InfoBarSeverity.Success);
                args.Cancel = false;
            }
            else
            {
                ShowNotification(NotificationInfoBar, "Error", message, InfoBarSeverity.Error);
            }
        }

        deferral.Complete();
    }

    /* Confirms deletion and calls ViewModel to delete supplier */
    private async Task OnDeleteRequested(SupplierInfo supplier)
    {
        var dialog = new ContentDialog
        {
            Title = "Confirm Deletion",
            Content = $"Are you sure you want to delete '{supplier.Name}'?\nThis action cannot be undone.",
            PrimaryButtonText = "Delete",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var (success, message) = await ViewModel.DeleteSupplierAsync(supplier.ID);
            ShowNotification(
                NotificationInfoBar,
                success ? "Success" : "Error",
                message,
                success ? InfoBarSeverity.Success : InfoBarSeverity.Error
            );
        }
    }

    // --- Form validation helpers ---
    private void AddForm_InputChanged(object sender, object e) => ValidateAddForm();
    private void EditForm_InputChanged(object sender, object e) => ValidateEditForm();

    private void ValidateAddForm() => AddSupplierDialog.IsPrimaryButtonEnabled = AreAddFormFieldsValid();
    private bool AreAddFormFieldsValid() =>
        !string.IsNullOrWhiteSpace(SupplierNameTextBox.Text) &&
        !string.IsNullOrWhiteSpace(SupplierEmailTextBox.Text) &&
        SupplierCountryCodeComboBox.SelectedItem != null &&
        !string.IsNullOrWhiteSpace(SupplierPhoneTextBox.Text) &&
        !string.IsNullOrWhiteSpace(SupplierStreetTextBox.Text) &&
        !string.IsNullOrWhiteSpace(SupplierCityTextBox.Text) &&
        !string.IsNullOrWhiteSpace(SupplierCountryTextBox.Text) &&
        !string.IsNullOrWhiteSpace(SupplierZipCodeTextBox.Text) &&
        !string.IsNullOrWhiteSpace(SupplierSalesRepTextBox.Text);

    private void ValidateEditForm() => EditSupplierDialog.IsPrimaryButtonEnabled = AreEditFormFieldsValid();
    private bool AreEditFormFieldsValid() =>
        !string.IsNullOrWhiteSpace(EditSupplierNameTextBox.Text) &&
        !string.IsNullOrWhiteSpace(EditSupplierEmailTextBox.Text) &&
        EditSupplierCountryCodeComboBox.SelectedItem != null &&
        !string.IsNullOrWhiteSpace(EditSupplierPhoneTextBox.Text) &&
        !string.IsNullOrWhiteSpace(EditSupplierAddressTextBox.Text) &&
        !string.IsNullOrWhiteSpace(EditSupplierSalesRepTextBox.Text);

    // --- CSV operations ---
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
                savePicker.SuggestedFileName = $"suppliers_{DateTime.Now:dd_MM_yyyy_HHmmss}.csv";

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteBytesAsync(file, csvData);
                    ShowNotification(NotificationInfoBar, "Download Complete", "The CSV file was saved successfully.", InfoBarSeverity.Success);
                }
            }
            else
            {
                ShowNotification(NotificationInfoBar, "No Data", "There is no supplier data to download.", InfoBarSeverity.Warning);
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

                // 10MB file size limit
                if (fileProperties.Size > 10 * 1024 * 1024)
                {
                    ShowNotification(NotificationInfoBar, "File Too Large", "Please select a file smaller than 10MB.", InfoBarSeverity.Error);
                    return;
                }

                SetButtonBusyState(UploadButton, UploadContent, UploadBusyIndicator, true);
                try
                {
                    var (success, message) = await ViewModel.UploadCsvAsync(file);
                    ShowNotification(NotificationInfoBar, success ? "Upload Complete" : "Upload Error", message, success ? InfoBarSeverity.Success : InfoBarSeverity.Error);
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
            ShowNotification(NotificationInfoBar, "Error", $"An unexpected error occurred: {ex.Message}", InfoBarSeverity.Error);
        }
    }

    /* Shows a temporary notification using InfoBar */
    private void ShowNotification(InfoBar infoBar, string title, string message, InfoBarSeverity severity)
    {
        infoBar.Title = title;
        infoBar.Message = message;
        infoBar.Severity = severity;
        infoBar.IsOpen = true;

        // Auto-close timer
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += (sender, e) =>
        {
            infoBar.IsOpen = false;
            timer.Stop();
        };
        timer.Start();
    }

    /* Toggles a button and its progress indicator during async operations */
    private void SetButtonBusyState(Button button, FrameworkElement content, ProgressRing indicator, bool isBusy)
    {
        button.IsEnabled = !isBusy;
        content.Visibility = isBusy ? Visibility.Collapsed : Visibility.Visible;
        indicator.IsActive = isBusy;
        indicator.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
    }

    // Unsubscribe from ViewModel events when page is unloaded to prevent memory leaks
    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.ViewRequested -= OnViewRequested;
            ViewModel.EditRequested -= OnEditRequested;
            ViewModel.DeleteRequested -= OnDeleteRequested;
        }
    }
}
