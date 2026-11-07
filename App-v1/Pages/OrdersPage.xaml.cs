using App_v1.Models;
using App_v1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using App_v1.Helpers;

namespace App_v1.Pages;

// This is the code-behind for the OrdersPage. It mainly handles UI events
// and calls methods on the ViewModel to do the actual work.
public sealed partial class OrdersPage : Page
{
    // A shortcut to access the ViewModel.
    private OrdersViewModel ViewModel => (OrdersViewModel)DataContext;

    public OrdersPage()
    {
        this.InitializeComponent();
        // Listen for requests from the ViewModel to open dialogs.
        ViewModel.ViewRequested += OnViewRequested;
        ViewModel.EditRequested += OnEditRequested;
        ViewModel.CancelRequested += OnCancelRequested;
    }

    // Navigates to the page for placing a new order.
    private void PlaceOrder_Click(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(ItemPage));
    }

    // These event handlers are for the buttons inside the DataGrid's action column.
    private async void ViewOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: OrdersInfo order })
        {
            await OnViewRequested(order);
        }
    }

    private async void EditOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: OrdersInfo order })
        {
            await OnEditRequested(order);
        }
    }

    private async void CancelOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: OrdersInfo order })
        {
            await OnCancelRequested(order);
        }
    }

    // This is called when the ViewModel wants to view an order's details.
    private Task OnViewRequested(OrdersInfo order)
    {
        // Store the selected order ID so the next page knows what to load.
        AppState.SelectedOrderId = order.Id;
        this.Frame.Navigate(typeof(OrderItemsPage));
        return Task.CompletedTask;
    }

    // This is called when the ViewModel wants to edit an order. It prepares and shows the edit dialog.
    private async Task OnEditRequested(OrdersInfo order)
    {
        // Store the order object in the dialog's Tag so we can get it back later.
        EditOrderDialog.Tag = order;

        EditOrderIdTextBox.Text = order.Id;

        // Pre-select the order's current status in the ComboBox.
        var currentStatusItem = EditOrderStatusComboBox.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => i.Content.ToString() == order.Status);
        EditOrderStatusComboBox.SelectedItem = currentStatusItem;

        ValidateEditForm();

        EditOrderDialog.XamlRoot = this.XamlRoot;
        await EditOrderDialog.ShowAsync();
    }

    // Fired when the "Update Status" button in the dialog is clicked.
    private async void EditOrderDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();

        if (!AreEditFormFieldsValid())
        {
            ShowNotification(NotificationInfoBar, "Validation Error", "Please select a new status.", InfoBarSeverity.Error);
            args.Cancel = true; // Prevents the dialog from closing.
            deferral.Complete();
            return;
        }

        if (sender.Tag is OrdersInfo order && EditOrderStatusComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var newStatus = selectedItem.Content.ToString();
            // Tell the ViewModel to perform the update.
            var (success, message) = await ViewModel.UpdateOrderStatusAsync(order.Id, newStatus);
            if (success)
            {
                ShowNotification(NotificationInfoBar, "Success", message, InfoBarSeverity.Success);
            }
            else
            {
                ShowNotification(NotificationInfoBar, "Error", message, InfoBarSeverity.Error);
                args.Cancel = true; // Keep dialog open if update failed.
            }
        }
        deferral.Complete();
    }

    // This shows the confirmation dialog for canceling an order.
    private async Task OnCancelRequested(OrdersInfo order)
    {
        var dialog = new ContentDialog
        {
            Title = "Confirm Cancellation",
            Content = $"Are you sure you want to cancel order '{order.Id}'?\nThis action cannot be undone.",
            PrimaryButtonText = "Cancel Order",
            SecondaryButtonText = "Keep Order",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var (success, message) = await ViewModel.CancelOrderAsync(order.Id);
            if (success)
            {
                ShowNotification(NotificationInfoBar, "Success", message, InfoBarSeverity.Success);
            }
            else
            {
                ShowNotification(NotificationInfoBar, "Error", message, InfoBarSeverity.Error);
            }
        }
    }

    private void EditForm_InputChanged(object sender, SelectionChangedEventArgs e) => ValidateEditForm();

    // Enables or disables the "Update Status" button based on form validity.
    private void ValidateEditForm()
    {
        EditOrderDialog.IsPrimaryButtonEnabled = AreEditFormFieldsValid();
    }

    // Simple validation: just make sure a status is selected.
    private bool AreEditFormFieldsValid()
    {
        return EditOrderStatusComboBox.SelectedItem != null;
    }

    // Handles the logic for downloading the orders CSV.
    private async void DownloadCSV_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowDownloadBusyState(true);
            var csvData = await ViewModel.DownloadCsvAsync();

            if (csvData != null && csvData.Length > 0)
            {
                var savePicker = new FileSavePicker();
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance));
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV Files", new[] { ".csv" });
                savePicker.SuggestedFileName = $"orders_{DateTime.Now:dd_MM_yyyy_HHmmss}.csv";

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteBytesAsync(file, csvData);
                    ShowNotification(NotificationInfoBar, "Download Complete", "The CSV file has been saved successfully.", InfoBarSeverity.Success);
                }
            }
            else
            {
                ShowNotification(NotificationInfoBar, "No Data", "No data was available to download.", InfoBarSeverity.Warning);
            }
        }
        catch (Exception ex)
        {
            ShowNotification(NotificationInfoBar, "Download Error", $"An error occurred while downloading the CSV: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            ShowDownloadBusyState(false);
        }
    }

    // A helper to show a notification bar for a few seconds.
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

    // Manages the UI state of the download button (showing/hiding the busy indicator).
    private void ShowDownloadBusyState(bool isBusy)
    {
        DownloadButton.IsEnabled = !isBusy;
        DownloadContent.Visibility = isBusy ? Visibility.Collapsed : Visibility.Visible;
        DownloadBusyIndicator.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        DownloadBusyIndicator.IsActive = isBusy;
    }

    // Clean up event subscriptions when navigating away from the page to prevent memory leaks.
    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.ViewRequested -= OnViewRequested;
            ViewModel.EditRequested -= OnEditRequested;
            ViewModel.CancelRequested -= OnCancelRequested;
        }
    }
}
