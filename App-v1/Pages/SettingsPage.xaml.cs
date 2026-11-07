// SettingsPage.xaml.cs

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using App_v1.Services; // Added to access UserSession and UserApiService

namespace App_v1.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();

        // Set the correct radio button based on the current theme
        if (App.MainWindowInstance?.Content is FrameworkElement rootElement)
        {
            switch (rootElement.RequestedTheme)
            {
                case ElementTheme.Light:
                    if (FindName("LightThemeRadio") is RadioButton lightRadio) lightRadio.IsChecked = true;
                    break;
                case ElementTheme.Dark:
                    if (FindName("DarkThemeRadio") is RadioButton darkRadio) darkRadio.IsChecked = true;
                    break;
            }
        }
    }

    #region Theme Radio Buttons

    private void LightThemeRadio_Checked(object sender, RoutedEventArgs e)
    {
        ApplyTheme("Light");
    }

    private void DarkThemeRadio_Checked(object sender, RoutedEventArgs e)
    {
        ApplyTheme("Dark");
    }

    private void ApplyTheme(string theme)
    {
        // Get the root element (the main Grid) from MainWindowInstance
        if (App.MainWindowInstance?.Content is FrameworkElement rootElement)
        {
            switch (theme)
            {
                case "Light":
                    rootElement.RequestedTheme = ElementTheme.Light;
                    break;
                case "Dark":
                    rootElement.RequestedTheme = ElementTheme.Dark;
                    break;
                default:
                    rootElement.RequestedTheme = ElementTheme.Default;
                    break;
            }
        }
    }

    #endregion

    #region Change Password Dialog
    private async void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        var staffIdTextBox = new TextBox { Text = UserSession.StaffId ?? "N/A", IsReadOnly = true, Margin = new Thickness(0, 0, 0, 8) };
        var oldPasswordBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 8) };
        var newPasswordBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 8) };

        var dialog = new ContentDialog
        {
            Title = "Change Password",
            PrimaryButtonText = "Update",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = "Staff ID" },
                    staffIdTextBox,
                    new TextBlock { Text = "Old Password" },
                    oldPasswordBox,
                    new TextBlock { Text = "New Password" },
                    newPasswordBox
                }
            }
        };

        // Attach the event handler to the button
        dialog.PrimaryButtonClick += ChangePasswordDialog_PrimaryButtonClick;

        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }

    private async void ChangePasswordDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        args.Cancel = true; // Keep the dialog open during async operations

        if (sender.Content is StackPanel panel)
        {
            // Extract values from the dialog's controls
            var staffId = (panel.Children[1] as TextBox)?.Text;
            var oldPassword = (panel.Children[3] as PasswordBox)?.Password;
            var newPassword = (panel.Children[5] as PasswordBox)?.Password;

            // Basic validation
            if (string.IsNullOrEmpty(staffId) || string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
            {
                ShowNotification("Validation Error", "All fields are required.", InfoBarSeverity.Error);
                deferral.Complete();
                return;
            }

            // Call the new API service
            var apiService = new UserApiService();
            var response = await apiService.ChangePasswordAsync(staffId, oldPassword, newPassword);

            if (response.Success)
            {
                ShowNotification("Success", response.Message, InfoBarSeverity.Success);
                args.Cancel = false; // Allow the dialog to close on success
            }
            else
            {
                ShowNotification("Error", response.Message, InfoBarSeverity.Error);
                // Keep the dialog open on failure by leaving args.Cancel as true
            }
        }
        deferral.Complete();
    }
    #endregion

    #region Profile Dialog
    private async void Profile_Click(object sender, RoutedEventArgs e)
    {
        // Get user data directly from the UserSession
        var staffId = UserSession.StaffId ?? "Not available";
        var name = UserSession.StaffName ?? "Not available";
        var sname = UserSession.StaffSurname ?? "Not available";
        var role = UserSession.Role ?? "Not available";
        var email = UserSession.StaffEmail ?? "Not available";
        var phone = UserSession.PhoneNumber ?? "Not available";

        var dialog = new ContentDialog
        {
            Title = "Profile",
            CloseButtonText = "Close",
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = $"Staff ID: {staffId}" },
                    new TextBlock { Text = $"Name: {name}" },
                    new TextBlock { Text = $"Surname: {sname}" },
                    new TextBlock { Text = $"Role: {role}" },
                    new TextBlock { Text = $"Email: {email}" },
                    new TextBlock { Text = $"Phone Number: {phone}" }
                }
            }
        };

        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }
    #endregion

    private void ShowNotification(string title, string message, InfoBarSeverity severity)
    {
        NotificationInfoBar.Title = title;
        NotificationInfoBar.Message = message;
        NotificationInfoBar.Severity = severity;
        NotificationInfoBar.IsOpen = true;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += (sender, e) =>
        {
            NotificationInfoBar.IsOpen = false;
            timer.Stop();
        };
        timer.Start();
    }
}