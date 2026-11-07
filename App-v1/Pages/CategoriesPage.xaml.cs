using App_v1.Helpers;
using App_v1.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Syncfusion.UI.Xaml.Notifications;

namespace App_v1.Pages;

// Code-behind for CategoriesPage.xaml. Handles UI events and user interaction.
public sealed partial class CategoriesPage : Page
{
    // A few constants to keep configuration clean.
    private const int NotificationTimeoutSeconds = 5;
    private const long MaxUploadFileSizeInBytes = 10 * 1024 * 1024; // 10 MB

    // Quick access to the page's ViewModel.
    private ViewModels.CategoryViewModel ViewModel => (ViewModels.CategoryViewModel)DataContext;

    // A reference to the main Frame for navigation purposes.
    public Frame ContentFrame => this.Frame;

    public CategoriesPage()
    {
        this.InitializeComponent();
        // Hook into ViewModel events to handle navigation.
        ViewModel.ViewRequested += OnViewRequested;
        ViewModel.EditRequested += OnEditRequested;
    }

    // Opens the 'Add Category' dialog with clean fields.
    private async void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        // Clear any previous text or errors.
        CategoryNameTextBox.Text = string.Empty;
        CategoryDescriptionTextBox.Text = string.Empty;
        AddDialogInfoBar.IsOpen = false;

        // Make sure the save button is correctly enabled/disabled from the start.
        ValidateAddForm();
        AddCategoryDialog.XamlRoot = this.XamlRoot;
        await AddCategoryDialog.ShowAsync();
    }

    // Handles the 'Edit' button click from a row in the datagrid.
    private async void EditCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CategoryInfo category })
        {
            await OnEditRequested(category);
        }
    }

    // Handles the 'View' button click from a row.
    private async void ViewCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CategoryInfo category })
        {
            await OnViewRequested(category);
        }
    }

    // Fetches CSV data from the API and prompts the user to save the file.
    private async void DownloadCSV_Click(object sender, RoutedEventArgs e)
    {
        SetButtonBusyState(DownloadButton, DownloadContent, DownloadBusyIndicator, true);
        try
        {
            var csvData = await ViewModel.DownloadCsvAsync();

            if (csvData is { Length: > 0 })
            {
                var savePicker = new FileSavePicker();
                // This is required to associate the picker with the main window.
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance));
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV Files", new[] { ".csv" });
                savePicker.SuggestedFileName = $"categories_{DateTime.Now:dd_MM_yyyy_HHmmss}.csv";

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteBytesAsync(file, csvData);
                    ShowNotification(NotificationInfoBar, "Download Complete", "The CSV file was saved successfully.", InfoBarSeverity.Success);
                }
            }
            else
            {
                ShowNotification(NotificationInfoBar, "No Data", "There was no data available to download.", InfoBarSeverity.Warning);
            }
        }
        catch (Exception)
        {
            ShowNotification(NotificationInfoBar, "Download Error", "An unexpected error occurred while downloading the file.", InfoBarSeverity.Error);
        }
        finally
        {
            SetButtonBusyState(DownloadButton, DownloadContent, DownloadBusyIndicator, false);
        }
    }

    // Opens a file picker for the user to select a CSV, then uploads it.
    private async void UploadCSV_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance));
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".csv");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file == null) return; // User cancelled.

            // Check the file size before attempting to upload.
            var fileProperties = await file.GetBasicPropertiesAsync();
            if (fileProperties.Size > MaxUploadFileSizeInBytes)
            {
                ShowNotification(NotificationInfoBar, "File Too Large", $"Please select a file smaller than {MaxUploadFileSizeInBytes / (1024 * 1024)}MB.", InfoBarSeverity.Error);
                return;
            }

            SetButtonBusyState(UploadButton, UploadContent, UploadBusyIndicator, true);
            var (success, message) = await ViewModel.UploadCsvAsync(file);
            var severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Error;
            ShowNotification(NotificationInfoBar, "Upload Result", message, severity);
        }
        catch (Exception)
        {
            ShowNotification(NotificationInfoBar, "Error", "An unexpected error occurred during the upload process.", InfoBarSeverity.Error);
        }
        finally
        {
            SetButtonBusyState(UploadButton, UploadContent, UploadBusyIndicator, false);
        }
    }

    // Handles the 'Save' button click on the Add Category dialog.
    private async void AddCategoryDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        args.Cancel = true; // Keep dialog open until everything succeeds.
        NotificationInfoBar.IsOpen = false; // Close previous page-level notifications.

        // Validate user input using the main page's InfoBar.
        var validatedName = Validator.ValidateCategoryName(CategoryNameTextBox.Text, NotificationInfoBar);
        if (validatedName == null)
        {
            deferral.Complete();
            return;
        }

        var validatedDescription = Validator.ValidateDescription(CategoryDescriptionTextBox.Text, NotificationInfoBar);
        if (validatedDescription == null)
        {
            deferral.Complete();
            return;
        }

        var newCategory = new CategoryInfo { Name = validatedName, Description = validatedDescription };
        var (success, message) = await ViewModel.AddCategoryAsync(newCategory);

        if (success)
        {
            ShowNotification(NotificationInfoBar, "Success", message, InfoBarSeverity.Success);
            args.Cancel = false; // Success! Allow the dialog to close.
        }
        else
        {
            // Show the API error on the main page InfoBar.
            ShowNotification(NotificationInfoBar, "Error", message, InfoBarSeverity.Error);
        }
        deferral.Complete();
    }

    // Handles the 'Update' button click on the Edit Category dialog.
    private async void EditCategoryDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        args.Cancel = true;
        NotificationInfoBar.IsOpen = false;

        if (sender.Tag is CategoryInfo category)
        {
            // Validate user input, showing errors on the main page's InfoBar.
            var validatedName = Validator.ValidateCategoryName(EditCategoryNameTextBox.Text, NotificationInfoBar);
            if (validatedName == null)
            {
                deferral.Complete();
                return;
            }

            var validatedDescription = Validator.ValidateDescription(EditCategoryDescriptionTextBox.Text, NotificationInfoBar);
            if (validatedDescription == null)
            {
                deferral.Complete();
                return;
            }

            // Update the category object with validated data.
            category.Name = validatedName;
            category.Description = validatedDescription;

            var (success, message) = await ViewModel.UpdateCategoryAsync(category);
            if (success)
            {
                ShowNotification(NotificationInfoBar, "Success", message, InfoBarSeverity.Success);
                args.Cancel = false; // Allow dialog to close.
            }
            else
            {
                // Show API error on the main page InfoBar.
                ShowNotification(NotificationInfoBar, "Error", message, InfoBarSeverity.Error);
            }
        }
        deferral.Complete();
    }

    // Navigates to the Category Products page.
    private Task OnViewRequested(CategoryInfo category)
    {
        AppState.SelectedCategoryId = category.Id;
        this.Frame.Navigate(typeof(CategoryProductsPage));
        return Task.CompletedTask;
    }

    // Pre-fills and shows the Edit Category dialog.
    private async Task OnEditRequested(CategoryInfo category)
    {
        // Clear previous state and load the selected category's data.
        EditDialogInfoBar.IsOpen = false;
        EditCategoryDialog.Tag = category; // Tag the dialog with the object to update.
        EditCategoryIdTextBox.Text = category.Id;
        EditCategoryNameTextBox.Text = category.Name;
        EditCategoryDescriptionTextBox.Text = category.Description;

        ValidateEditForm();
        EditCategoryDialog.XamlRoot = this.XamlRoot;
        await EditCategoryDialog.ShowAsync();
    }

    // Real-time validation to enable/disable dialog primary buttons.
    private void AddForm_TextChanged(object sender, TextChangedEventArgs e) => ValidateAddForm();
    private void EditForm_TextChanged(object sender, TextChangedEventArgs e) => ValidateEditForm();

    // Enables the 'Save' button only if the name field isn't empty.
    private void ValidateAddForm()
    {
        AddCategoryDialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(CategoryNameTextBox.Text);
    }

    // Enables the 'Update' button only if the name field isn't empty.
    private void ValidateEditForm()
    {
        EditCategoryDialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(EditCategoryNameTextBox.Text);
    }

    // Helper to show a self-closing notification on the InfoBar.
    private void ShowNotification(InfoBar infoBar, string title, string message, InfoBarSeverity severity)
    {
        infoBar.Title = title;
        infoBar.Message = message;
        infoBar.Severity = severity;
        infoBar.IsOpen = true;

        // A timer to automatically close the notification after a few seconds.
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(NotificationTimeoutSeconds) };
        timer.Tick += (s, e) =>
        {
            infoBar.IsOpen = false;
            timer.Stop();
        };
        timer.Start();
    }

    // Toggles a button's state between normal and busy (e.g., for downloads).
    private void SetButtonBusyState(Button button, UIElement content, SfBusyIndicator indicator, bool isBusy)
    {
        button.IsEnabled = !isBusy;
        content.Visibility = isBusy ? Visibility.Collapsed : Visibility.Visible;
        indicator.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        indicator.IsActive = isBusy;
    }

    // Unsubscribe from ViewModel events to prevent memory leaks when leaving the page.
    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.ViewRequested -= OnViewRequested;
            ViewModel.EditRequested -= OnEditRequested;
        }
    }
}