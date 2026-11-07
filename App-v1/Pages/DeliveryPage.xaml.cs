using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Syncfusion.UI.Xaml.Grids;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using App_v1.Models;


namespace App_v1.Pages;


public sealed partial class DeliveryPage : Page
{
    public DeliveryPage()
    {
        this.InitializeComponent();
    }


    private void Add_Delivery_Click(object sender, RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(DeliveryEntryForm));
    }

    private async void ViewDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Delivery delivery)
        {
            // Use a StackPanel to hold multiple elements
            var contentPanel = new StackPanel { Spacing = 10 };

            // --- Grid for DELIVERED Items (Your existing code) ---
            var deliveredHeader = new TextBlock { Text = "Products Received in this Delivery", FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            var deliveredGrid = new Syncfusion.UI.Xaml.DataGrid.SfDataGrid
            {
                AutoGenerateColumns = false,
                ItemsSource = delivery.Items,
                GridLinesVisibility = GridLinesVisibility.Both,


            };
            deliveredGrid.Columns.Add(new Syncfusion.UI.Xaml.DataGrid.GridTextColumn { MappingName = "item_id", HeaderText = "Product Code" });
            deliveredGrid.Columns.Add(new Syncfusion.UI.Xaml.DataGrid.GridTextColumn { MappingName = "itemName", HeaderText = "Product Name", TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap });
            deliveredGrid.Columns.Add(new Syncfusion.UI.Xaml.DataGrid.GridTextColumn { MappingName = "quantity", HeaderText = "Quantity Received" });
            deliveredGrid.Columns.Add(new Syncfusion.UI.Xaml.DataGrid.GridTextColumn { MappingName = "comments", HeaderText = "Comments", TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap });

            contentPanel.Children.Add(deliveredHeader);
            contentPanel.Children.Add(deliveredGrid);

            // --- Grid for MISSING Items (The new logic) ---
            if (delivery.MissingItems != null && delivery.MissingItems.Any())
            {
                var missingHeader = new TextBlock { Text = "Missing Products for this Order", FontWeight = Microsoft.UI.Text.FontWeights.Bold, Margin = new Thickness(0, 15, 0, 0) };
                var missingGrid = new Syncfusion.UI.Xaml.DataGrid.SfDataGrid
                {
                    AutoGenerateColumns = false,
                    ItemsSource = delivery.MissingItems,
                    GridLinesVisibility = GridLinesVisibility.Both,

                };
                missingGrid.Columns.Add(new Syncfusion.UI.Xaml.DataGrid.GridTextColumn { MappingName = "itemcode", HeaderText = "Product Code" });
                missingGrid.Columns.Add(new Syncfusion.UI.Xaml.DataGrid.GridTextColumn { MappingName = "itemName", HeaderText = "Product Name", TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap });
                missingGrid.Columns.Add(new Syncfusion.UI.Xaml.DataGrid.GridTextColumn { MappingName = "missing_quantity", HeaderText = "Quantity Missing" });

                contentPanel.Children.Add(missingHeader);
                contentPanel.Children.Add(missingGrid);
            }

            // --- Show the Dialog ---
            var dialog = new ContentDialog
            {
                Title = $"Delivery {delivery.DeliveryId} - Details",
                Content = contentPanel, // Set the StackPanel as the content
                PrimaryButtonText = "Close",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}