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
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class IAMPage : Page
    {
        public IAMPage()
        {
            InitializeComponent();
            // Set the default selected item (Users)
            SelectorBarItemUsers.IsSelected = true;
            // The SelectionChanged event will handle navigation
        }

        // Ensure this method matches the delegate signature:
        // TypedEventHandler<SelectorBar, SelectorBarSelectionChangedEventArgs>
        private void SelectorBar2_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            // Use sender.SelectedItem instead of args.SelectedItem
            var selectedItem = sender.SelectedItem as SelectorBarItem;
            if (selectedItem == SelectorBarItemUsers)
            {
                ContentFrame.Navigate(typeof(UsersPage));
            }
            else if (selectedItem == SelectorBarItemRoles)
            {
                ContentFrame.Navigate(typeof(RolesPage));
            }
            else if (selectedItem == SelectorBarItemPermissions)
            {
                ContentFrame.Navigate(typeof(PermissionsPage));
            }
            else if (selectedItem == SelectorBarItemGuests)
            {
                ContentFrame.Navigate(typeof(GuestPage));
            }
            else if (selectedItem == SelectorBarItemLogs)
            {
                ContentFrame.Navigate(typeof(LogsPage));
            }
        }
    }
}
