using Microsoft.UI.Xaml;

namespace App_v1;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Navigate to LoginPage when the app starts
        RootFrame.Navigate(typeof(LoginPage));
    }
}