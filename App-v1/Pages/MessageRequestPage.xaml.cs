// Pages/MessageRequestPage.xaml.cs

using App_v1.Models;
using App_v1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace App_v1.Pages;

public sealed partial class MessageRequestPage : Page
{
    private MessageRequestViewModel ViewModel => (MessageRequestViewModel)DataContext;

    public MessageRequestPage()
    {
        this.InitializeComponent();
    }

    private async void CreateRequest_Click(object sender, RoutedEventArgs e)
    {
        var titleTextBox = new TextBox { Header = "Title *", PlaceholderText = "e.g., Request for leave" };
        var bodyTextBox = new TextBox { Header = "Body *", PlaceholderText = "Please provide details...", Height = 120, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true };

        var dialog = new ContentDialog
        {
            Title = "Create New Message Request",
            PrimaryButtonText = "Submit",
            CloseButtonText = "Cancel",
            Content = new StackPanel { Spacing = 12, Children = { titleTextBox, bodyTextBox } }
        };

        dialog.XamlRoot = this.XamlRoot;
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(titleTextBox.Text) || string.IsNullOrWhiteSpace(bodyTextBox.Text))
            {
                ShowNotification("Validation Error", "Title and Body are required.", InfoBarSeverity.Error);
                return;
            }

            var (success, message) = await ViewModel.CreateRequestAsync(titleTextBox.Text, bodyTextBox.Text);
            ShowNotification(success ? "Success" : "Error", message, success ? InfoBarSeverity.Success : InfoBarSeverity.Error);
        }
    }

    private async void Approve_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MessageRequestInfo request })
        {
            var (success, message) = await ViewModel.ApproveRequestAsync(request.Id);
            ShowNotification(success ? "Success" : "Error", message, success ? InfoBarSeverity.Success : InfoBarSeverity.Error);
        }
    }

    private async void Decline_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MessageRequestInfo request })
        {
            var (success, message) = await ViewModel.DeclineRequestAsync(request.Id);
            ShowNotification(success ? "Success" : "Error", message, success ? InfoBarSeverity.Success : InfoBarSeverity.Error);
        }
    }

    private async void ShowNotification(string title, string message, InfoBarSeverity severity)
    {
        NotificationInfoBar.Title = title;
        NotificationInfoBar.Message = message;
        NotificationInfoBar.Severity = severity;
        NotificationInfoBar.IsOpen = true;
        await Task.Delay(3000);
        NotificationInfoBar.IsOpen = false;
    }
}