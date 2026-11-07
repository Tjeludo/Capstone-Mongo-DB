/*
This is the "code-behind" for my PermissionsPage view. My goal here is to handle
UI-specific tasks, like showing dialogs. All the actual
business logic is kept in the ViewModel. This file's only job is to get data
from the UI and pass it over to the ViewModel.
*/
using System;
using System.Threading.Tasks;
using App_v1.Models;
using App_v1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace App_v1.Pages;

public sealed partial class PermissionsPage : Page
{
    private PermissionsViewModel ViewModel => (PermissionsViewModel)DataContext;

    public PermissionsPage()
    {
        InitializeComponent();
    }

    private async void AddPermission_Click(object sender, RoutedEventArgs e)
    {
        SectionTextBox.Text = string.Empty;
        AddPermissionDialog.XamlRoot = this.XamlRoot;
        var result = await AddPermissionDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var section = SectionTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(section))
            {
                await ShowErrorDialog("Section name cannot be empty.");
                return;
            }

            var (success, message) = await ViewModel.AddPermissionsAsync(section);
            if (success) await ShowSuccessDialog(message);
            else await ShowErrorDialog(message);
        }
    }

    private async void GrantButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PermissionsInfo perm })
        {
            await ShowGrantRevokeDialog(perm, isGranting: true);
        }
    }

    private async void RevokeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PermissionsInfo perm })
        {
            await ShowGrantRevokeDialog(perm, isGranting: false);
        }
    }

    private async Task ShowGrantRevokeDialog(PermissionsInfo perm, bool isGranting)
    {
        GrantRevokeDialog.Title = isGranting ? $"Grant Permission: {perm.Name}" : $"Revoke Permission: {perm.Name}";
        GrantRevokeDialog.PrimaryButtonText = isGranting ? "Grant" : "Revoke";
        RoleNameTextBox.Text = string.Empty;

        GrantRevokeDialog.XamlRoot = this.XamlRoot;
        var result = await GrantRevokeDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var roleName = RoleNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(roleName))
            {
                await ShowErrorDialog("Role name cannot be empty.");
                return;
            }

            var (success, message) = isGranting
                ? await ViewModel.GrantPermissionAsync(perm.Id, roleName)
                : await ViewModel.RevokePermissionAsync(perm.Id, roleName);

            if (success) await ShowSuccessDialog(message);
            else await ShowErrorDialog(message);
        }
    }

    /*
    Reusable methods for showing success and error dialogs.
    */
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
        // Unsubscribe from events to prevent memory leaks.
    }
}