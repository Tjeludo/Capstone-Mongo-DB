// LoginPage.xaml.cs

using App_v1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace App_v1;

public sealed partial class LoginPage : Page
{
    public LoginViewModel ViewModel => (LoginViewModel)DataContext;

    public LoginPage()
    {
        InitializeComponent();
        ViewModel.TwoFactorAuthRequested += Show2FADialog;
        ViewModel.LoginSuccess += NavigateToDashboard;
        ViewModel.ShowDialogRequested += ShowDialog;
    }

    private async Task<string?> Show2FADialog(Func<Task<bool>> resendAction)
    {
        var inputTextBox = new TextBox { PlaceholderText = "Enter 6-digit code" };
        var resendLink = new HyperlinkButton { Content = "Didn't receive OTP? Resend" };
        var statusTextBlock = new TextBlock { Visibility = Visibility.Collapsed };

        var contentStack = new StackPanel
        {
            Spacing = 12,
            Children = { inputTextBox, resendLink, statusTextBlock }
        };

        var dialog = new ContentDialog
        {
            Title = "Two-Factor Authentication",
            Content = contentStack,
            PrimaryButtonText = "Verify",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        resendLink.Click += async (s, e) =>
        {
            // Show a waiting message and disable controls
            resendLink.IsEnabled = false;
            inputTextBox.IsEnabled = false;
            dialog.IsPrimaryButtonEnabled = false;
            statusTextBlock.Text = "Sending a new code...";
            statusTextBlock.Visibility = Visibility.Visible;

            bool success = await resendAction();

            // Update UI based on the result
            if (success)
            {
                statusTextBlock.Text = "A new code has been sent.";
            }
            else
            {
                statusTextBlock.Text = "Failed to send a new code. Please try again.";
            }

            // Re-enable controls
            resendLink.IsEnabled = true;
            inputTextBox.IsEnabled = true;
            dialog.IsPrimaryButtonEnabled = true;
        };

        var result = await dialog.ShowAsync();

        return result == ContentDialogResult.Primary ? inputTextBox.Text : null;
    }

    private void NavigateToDashboard()
    {
        Frame.Navigate(typeof(DashboardPage));
    }

    private async Task ShowDialog(string title, string content)
    {
        // Determine the severity based on the title
        bool isError = title.Contains("Failed") || title.Contains("Error");
        LoginInfoBar.Severity = isError ? InfoBarSeverity.Error : InfoBarSeverity.Informational;

        // Set the message and open the InfoBar
        LoginInfoBar.Title = title;
        LoginInfoBar.Message = content;
        LoginInfoBar.IsOpen = true;

        // Wait for 5 seconds
        await Task.Delay(5000);

        // Close the InfoBar
        LoginInfoBar.IsOpen = false;
    }

    private void ForgotPassword_Click(object sender, RoutedEventArgs e)
    { /* ... */
    }

    private void GuestLogin_Click(object sender, RoutedEventArgs e)
    { /* ... */
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.TwoFactorAuthRequested -= Show2FADialog;
            ViewModel.LoginSuccess -= NavigateToDashboard;
            ViewModel.ShowDialogRequested -= ShowDialog;
        }
    }
}