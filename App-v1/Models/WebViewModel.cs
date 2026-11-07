using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace App_v1.ViewModels;

// Corrected the class name to match its usage in the View
public partial class WebViewViewModel : ObservableObject
{
    // The default URL to be loaded in the WebView2
    [ObservableProperty]
    private Uri source = new("https://gitlab.cs.uct.ac.za/");

    [ObservableProperty]
    private bool isLoading = true;

    [ObservableProperty]
    private bool hasFailures;

    [ObservableProperty]
    // Corrected: Used string literal for the generated command name
    [NotifyCanExecuteChangedFor("BrowserBackCommand")]
    private bool canGoBack;

    [ObservableProperty]
    // Corrected: Used string literal for the generated command name
    [NotifyCanExecuteChangedFor("BrowserForwardCommand")]
    private bool canGoForward;

    // Actions to be provided by the View to control the WebView2
    private Action? _browserBackAction;
    private Action? _browserForwardAction;
    private Action? _reloadAction;

    // This method is called from the code-behind to link ViewModel commands to the WebView2 control
    public void Initialize(Action browserBack, Action browserForward, Action reload)
    {
        _browserBackAction = browserBack;
        _browserForwardAction = browserForward;
        _reloadAction = reload;
    }

    // These methods are called by the code-behind to update the command's CanExecute state
    public void UpdateBrowserCanGoBack(bool value) => CanGoBack = value;
    public void UpdateBrowserCanGoForward(bool value) => CanGoForward = value;

    // Corrected: Used string literal for the generated property name
    [RelayCommand(CanExecute = "CanGoBack")]
    private void BrowserBack() => _browserBackAction?.Invoke();

    // Corrected: Used string literal for the generated property name
    [RelayCommand(CanExecute = "CanGoForward")]
    private void BrowserForward() => _browserForwardAction?.Invoke();

    [RelayCommand]
    private void Reload() => _reloadAction?.Invoke();

    [RelayCommand]
    private async Task OpenInBrowser()
    {
        if (Source != null)
        {
            await Launcher.LaunchUriAsync(Source);
        }
    }
}