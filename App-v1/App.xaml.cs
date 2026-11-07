using System.Collections.ObjectModel;
using App_v1.Models;
using Microsoft.UI.Xaml;

namespace App_v1;

public partial class App : Application
{
    public static Window? MainWindowInstance
    {
        get; private set;
    }

    private Window? _window;

    // Static cart accessible globally, now using CartInfo
    public static ObservableCollection<CartInfo> Cart { get; } = new();

    public App()
    {
        InitializeComponent();
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXZfcnRTQmRYVUNxWEtWYEg=");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindowInstance = new MainWindow();
        _window = MainWindowInstance;
        _window.Activate();

        // Set the initial theme to Light Mode
        if (_window.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = ElementTheme.Light;
        }
    }
}