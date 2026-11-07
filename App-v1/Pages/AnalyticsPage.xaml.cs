using App_v1.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace App_v1.Pages;

public sealed partial class AnalyticsPage : Page
{
    // This public property exposes the ViewModel to the XAML through {x:Bind}
    public AnalyticsViewModel ViewModel
    {
        get;
    }

    public AnalyticsPage()
    {
        this.InitializeComponent();
        // Instantiate the ViewModel
        ViewModel = new AnalyticsViewModel();
    }
}
