using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syncfusion.UI.Xaml.DataGrid;
using Syncfusion.UI.Xaml.DataGrid.Helpers;

namespace App_v1.Demo
{
    public sealed partial class BlankPage : Page
    {
        public BlankPage()
        {
            this.InitializeComponent();
        }

        //private void sfDataGrid_RowTapped(object sender, RowTappedEventArgs e)
        //{
        //    if (e.RowData != null)
        //    {
        //        // Get the flyout from resources
        //        var rowFlyout = (CommandBarFlyout)this.Resources["RowFlyout"];

        //        // Show the flyout at the tapped row
        //        var rowControl = e.RowColumnIndex.RowIndex >= 0
        //            ? sfDataGrid.GetVisualContainer().GetContainerFromIndex(e.RowColumnIndex.RowIndex)
        //            : null;

        //        rowFlyout?.ShowAt(rowControl ?? sfDataGrid);
        //    }
        //}

        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            var selected = sfDataGrid.SelectedItem;
            // Edit logic here
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            var selected = sfDataGrid.SelectedItem;
            // Delete logic here
        }

        private void ShareRow_Click(object sender, RoutedEventArgs e)
        {
            var selected = sfDataGrid.SelectedItem;
            // Share logic here
        }
    }
}
