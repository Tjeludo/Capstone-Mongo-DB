// App_v1/Pages/RolesPage.xaml.cs

using App_v1.Models;
using App_v1.ViewModels;
using App_v1.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace App_v1.Pages;

public sealed partial class RolesPage : Page
{
    private RolesViewModel ViewModel => (RolesViewModel)DataContext;
    public Frame ContentFrame => Frame;

    public RolesPage()
    {
        this.InitializeComponent();
        ViewModel.ViewRequested += OnViewRequested;
        ViewModel.EditRequested += OnEditRequested;
        ViewModel.DeleteRequested += OnDeleteRequested;
    }

    private async void ViewRole_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: RolesInfo role })
        {
            await OnViewRequested(role);
        }
    }

    private async void EditRole_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: RolesInfo role })
        {
            await OnEditRequested(role);
        }
    }

    private async void DeleteRole_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: RolesInfo role })
        {
            await OnDeleteRequested(role);
        }
    }

    private async void AddRole_Click(object sender, RoutedEventArgs e)
    {
        RoleNameTextBox.Text = string.Empty;
        RoleAbrvTextBox.Text = string.Empty;
        // REMOVED: RoleDescriptionTextBox.Text = string.Empty;
        RoleStatusComboBox.SelectedIndex = 0;

        AddRoleDialog.XamlRoot = this.XamlRoot;
        var result = await AddRoleDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(RoleNameTextBox.Text))
            {
                await ShowErrorDialog("Please enter a role name.");
                return;
            }
            if (string.IsNullOrWhiteSpace(RoleAbrvTextBox.Text))
            {
                await ShowErrorDialog("Please enter a role abbreviation.");
                return;
            }

            var newRole = new RolesInfo
            {
                RoleName = RoleNameTextBox.Text.Trim(),
                RoleAbbreviation = RoleAbrvTextBox.Text.Trim(),
                // REMOVED: Description property assignment
                Status = ((ComboBoxItem)RoleStatusComboBox.SelectedItem)?.Content?.ToString()?.ToLower() ?? "active"
            };

            var (success, message) = await ViewModel.AddRoleAsync(newRole);
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

    private async Task OnViewRequested(RolesInfo role)
    {
        AppState.SelectedRoleCode = role.RoleCode;
        AppState.SelectedRoleName = role.RoleName;
        this.ContentFrame.Navigate(typeof(RolePermissionsPage));
        await Task.CompletedTask;
    }

    private async Task OnEditRequested(RolesInfo role)
    {
        EditRoleCodeTextBox.Text = role.RoleCode.ToString();
        EditRoleNameTextBox.Text = role.RoleName;
        EditRoleAbrvTextBox.Text = role.RoleAbbreviation;
        // REMOVED: EditRoleDescriptionTextBox.Text = role.Description;
        EditRoleStatusComboBox.SelectedIndex = role.Status.Equals("active", StringComparison.OrdinalIgnoreCase) ? 0 : 1;

        EditRoleDialog.XamlRoot = this.XamlRoot;
        var result = await EditRoleDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(EditRoleNameTextBox.Text))
            {
                await ShowErrorDialog("Please enter a role name.");
                return;
            }
            if (string.IsNullOrWhiteSpace(EditRoleAbrvTextBox.Text))
            {
                await ShowErrorDialog("Please enter a role abbreviation.");
                return;
            }

            role.RoleName = EditRoleNameTextBox.Text.Trim();
            role.RoleAbbreviation = EditRoleAbrvTextBox.Text.Trim();
            // REMOVED: Description property assignment
            role.Status = ((ComboBoxItem)EditRoleStatusComboBox.SelectedItem)?.Content?.ToString()?.ToLower() ?? "active";

            var (success, message) = await ViewModel.UpdateRoleAsync(role);
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

    private async Task OnDeleteRequested(RolesInfo role)
    {
        var dialog = new ContentDialog
        {
            Title = "Confirm Delete",
            Content = $"Are you sure you want to delete the role '{role.RoleName}'?\n\nThis action cannot be undone.",
            PrimaryButtonText = "Delete",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var (success, message) = await ViewModel.DeleteRoleAsync(role.RoleCode);
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

    // --- (All other methods for CSV, Busy State, and Dialogs remain unchanged) ---
    private async void DownloadCSV_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowDownloadBusyState(true);
            var csvData = await ViewModel.DownloadCsvAsync();

            if (csvData is { Length: > 0 })
            {
                var savePicker = new FileSavePicker();
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance));
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV Files", [".csv"]);
                savePicker.SuggestedFileName = $"roles_{DateTime.Now:dd_MM_yyyy_HHmmss}.csv";

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteBytesAsync(file, csvData);
                    await ShowSuccessDialog("Download successful & complete");
                }
            }
            else
            {
                await ShowErrorDialog("No data available to download.");
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
                var fileProperties = await file.GetBasicPropertiesAsync();
                if (fileProperties.Size > 10 * 1024 * 1024) // 10MB limit
                {
                    await ShowErrorDialog("File size too large. Please select a file smaller than 10MB.");
                    return;
                }

                ShowUploadBusyState(true);
                try
                {
                    var (success, message) = await ViewModel.UploadCsvAsync(file);
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
        var dialog = new ContentDialog
        {
            Title = "Success",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
    private async Task ShowErrorDialog(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
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