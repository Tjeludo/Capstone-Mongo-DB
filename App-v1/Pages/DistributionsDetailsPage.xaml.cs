using App_v1.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App_v1.Pages
{
    public sealed partial class DistributionsDetailsPage : Page
    {
        public DistributionsDetailsViewModel ViewModel { get; set; }

        public DistributionsDetailsPage()
        {
            this.InitializeComponent();
            ViewModel = this.DataContext as DistributionsDetailsViewModel;
        }

        // This method is called when the page is navigated to
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 'e.Parameter' contains the object passed during navigation
            if (e.Parameter is string distributionId)
            {
                PageTitle.Text = $"Distribution Details: {distributionId}";
                // Tell the ViewModel to load the data for this specific ID
                await ViewModel.LoadDistributionDetailsAsync(distributionId);
            }
        }
    }
}