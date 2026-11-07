using App_v1.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;

namespace App_v1.Pages;

public sealed partial class WebViewPage : Page
{
    public WebViewViewModel ViewModel
    {
        get;
    }

    public WebViewPage()
    {
        ViewModel = new WebViewViewModel();
        InitializeComponent();
        // Pass the WebView2's navigation methods to the ViewModel
        ViewModel.Initialize(
            () => WebView.GoBack(),
            () => WebView.GoForward(),
            () => WebView.CoreWebView2?.Reload()
        );
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // Subscribe to WebView2 events to update the ViewModel's state
        WebView.NavigationStarting += OnNavigationStarting;
        WebView.NavigationCompleted += OnNavigationCompleted;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        // Unsubscribe from events to prevent memory leaks when navigating away
        WebView.NavigationStarting -= OnNavigationStarting;
        WebView.NavigationCompleted -= OnNavigationCompleted;
    }

    private void OnNavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        ViewModel.IsLoading = true;
        ViewModel.HasFailures = false;
    }

    private void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        ViewModel.IsLoading = false;

        // Check the boolean 'IsSuccess' property instead of the error status enum.
        if (!args.IsSuccess)
        {
            // If navigation was not successful, check the error status.
            // This is where you could log args.WebErrorStatus if you wanted to know the specific error.
            ViewModel.HasFailures = true;
        }
        else
        {
            // Explicitly set HasFailures to false on a successful navigation.
            ViewModel.HasFailures = false;
        }

        // Update the CanExecute state of the navigation commands in the ViewModel
        ViewModel.UpdateBrowserCanGoBack(sender.CanGoBack);
        ViewModel.UpdateBrowserCanGoForward(sender.CanGoForward);
    }
}
