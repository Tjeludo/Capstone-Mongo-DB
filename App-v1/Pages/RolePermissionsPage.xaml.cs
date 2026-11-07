// Pages/RolePermissionsPage.xaml.cs
using App_v1.Helpers;
using App_v1.Models;
using App_v1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace App_v1.Pages
{
    public sealed partial class RolePermissionsPage : Page
    {
        private RolePermissionsViewModel ViewModel => (RolePermissionsViewModel)DataContext;

        public RolePermissionsPage()
        {
            this.InitializeComponent();
            this.DataContext = new RolePermissionsViewModel();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (!string.IsNullOrEmpty(AppState.SelectedRoleName))
            {
                await ViewModel.LoadPermissionsAsync(AppState.SelectedRoleCode, AppState.SelectedRoleName);
                PageTitle.Text = $"Permissions for '{ViewModel.RoleName}'";
            }
        }        
    }
}