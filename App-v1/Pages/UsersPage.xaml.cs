// App_v1/Pages/UsersPage.xaml.cs

using App_v1.Models;
using App_v1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Threading.Tasks;

namespace App_v1.Pages;

public sealed partial class UsersPage : Page
{
    private UserViewModel ViewModel => (UserViewModel)DataContext;

    public UsersPage()
    {
        this.InitializeComponent();
    }

    private async void Status_Toggled(object sender, RoutedEventArgs e)
    {
        // Check if the sender is a ToggleSwitch and has a valid UserInfo object.
        if (sender is ToggleSwitch toggleSwitch && toggleSwitch.DataContext is UserInfo user)
        {
            // --- THE FIX ---
            // 1. Unsubscribe from the Toggled event to prevent a feedback loop.
            toggleSwitch.Toggled -= Status_Toggled;

            var (success, message) = await ViewModel.UpdateUserStatusAsync(user);

            if (!success)
            {
                // If the API call fails, the local object's status was not changed in the ViewModel.
                // We just need to visually revert the toggle switch to match the unchanged local data.
                toggleSwitch.IsOn = user.Status.Equals("active", StringComparison.OrdinalIgnoreCase);
                await ShowErrorDialog(message);
            }

            // --- THE FIX ---
            // 2. Re-subscribe to the event so it works for the next manual click by the user.
            toggleSwitch.Toggled += Status_Toggled;
        }
    }

    private async Task ShowErrorDialog(string message)
    {
        var dialog = new ContentDialog { Title = "Error", Content = message, CloseButtonText = "OK", XamlRoot = this.XamlRoot };
        await dialog.ShowAsync();
    }
}

public class StatusToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is string status && status.Equals("active", StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is bool isOn && isOn ? "active" : "inactive";
    }
}