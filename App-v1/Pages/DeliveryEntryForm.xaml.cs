using App_v1.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace App_v1.Pages
{
    public class OrderDetailsResponse
    {
        public string SupplierId { get; set; }
        public string SupplierName { get; set; }
        public List<PurchaseOrderItem> Items { get; set; }
    }

    public sealed partial class DeliveryEntryForm : Page
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string apiBaseUrl = "http://127.0.0.1:8000";

        private ObservableCollection<PurchaseOrderItem> _fullOrderList = new ObservableCollection<PurchaseOrderItem>();
        private ObservableCollection<PurchaseOrderItem> _filteredOrderList = new ObservableCollection<PurchaseOrderItem>();

        public DeliveryEntryForm()
        {
            this.InitializeComponent();
            this.Loaded += DeliveryEntryForm_Loaded;
            OrderItemsListView.ItemsSource = _filteredOrderList;
        }

        private async void DeliveryEntryForm_Loaded(object sender, RoutedEventArgs e)
        {
            DeliveryDatePicker.SelectedDate = DateTimeOffset.Now;
            await FetchAndFillDeliveryID();
        }

        private async Task FetchAndFillDeliveryID()
        {
            try
            {
                var response = await client.GetStringAsync($"{apiBaseUrl}/generate_delivery_number");
                var result = JsonSerializer.Deserialize<DeliveryNumberResponse>(response);
                if (result != null)
                {
                    DeliveryIdTextBox.Text = result.deliveryNum;
                }
            }
            catch (Exception)
            {
                DeliveryIdTextBox.Text = $"DEL-{DateTime.Now:yyyyMMddHHmmss}";
            }
        }

        private async Task ShowInfoBar(string title, string message, InfoBarSeverity severity)
        {
            PageInfoBar.Title = title;
            PageInfoBar.Message = message;
            PageInfoBar.Severity = severity;
            PageInfoBar.IsOpen = true;

            if (severity == InfoBarSeverity.Error)
            {
                await Task.Delay(5000);
                PageInfoBar.IsOpen = false;
            }
            else if(severity == InfoBarSeverity.Warning)
            {
                await Task.Delay(5000);
                PageInfoBar.IsOpen = false;
            }
            else
            {
                await Task.Delay(5000);
                PageInfoBar.IsOpen = false;
            }
        }

        private void PageInfoBar_CloseButtonClick(InfoBar sender, object args)
        {
            sender.IsOpen = false;
        }

        private async Task FetchOrderItemsAsync(string orderId)
        {
            _fullOrderList.Clear();
            _filteredOrderList.Clear();
            SupplierIDTextBox.Text = "";
            SupplierNameTextBox.Text = "";
            SupplierIDTextBox.IsReadOnly = false;

            if (string.IsNullOrWhiteSpace(orderId)) return;

            try
            {
                var requestData = new { orderId };
                string json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync($"{apiBaseUrl}/get_order_items", content);

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var orderDetails = await response.Content.ReadFromJsonAsync<OrderDetailsResponse>(options);

                    if (orderDetails?.Items != null)
                    {
                        SupplierIDTextBox.Text = orderDetails.SupplierId;
                        SupplierNameTextBox.Text = orderDetails.SupplierName;
                        SupplierIDTextBox.IsReadOnly = true;

                        foreach (var item in orderDetails.Items)
                            _fullOrderList.Add(item);
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    var error = JsonSerializer.Deserialize<Dictionary<string, string>>(errorContent);
                    ShowInfoBar("Order Error", error["detail"], InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("API Error", $"An error occurred: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                ApplyFilter(SearchTextBox.Text);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OrderIDTextBox.Text) ||
                string.IsNullOrWhiteSpace(SupplierIDTextBox.Text) ||
                string.IsNullOrWhiteSpace(SupplierNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(CheckedByTextBox.Text))
            {
                ShowInfoBar("Input Required", "Please fill in all required fields (Supplier ID, Name, Order ID, Checked By).", InfoBarSeverity.Warning);
                return;
            }

            foreach (var item in _fullOrderList)
            {
                if (item.IsInvalid)
                {
                    ShowInfoBar("Validation Error", $"Please correct the quantity for '{item.ItemName}'. {item.ValidationMessage}", InfoBarSeverity.Error);
                    return;
                }
            }

            try
            {
                var deliveryData = new
                {
                    deliveryId = DeliveryIdTextBox.Text,
                    supplierId = SupplierIDTextBox.Text,
                    supplierName = SupplierNameTextBox.Text,
                    orderId = OrderIDTextBox.Text,
                    checkedBy = CheckedByTextBox.Text,
                    comments = overallDeliveryTextBox.Text,
                    deliveryItems = _fullOrderList
                        .Where(item => item.QuantityReceived.HasValue && item.QuantityReceived.Value > 0)
                        .Select(item => new
                        {
                            itemId = item.ItemId,
                            itemName = item.ItemName,
                            quantityReceived = item.QuantityReceived.Value,
                            comments = item.Comments
                        }).ToList()
                };

                if (!deliveryData.deliveryItems.Any())
                {
                    ShowInfoBar("Input Needed", "Please enter a received quantity for at least one item.", InfoBarSeverity.Warning);
                    return;
                }

                string json = JsonSerializer.Serialize(deliveryData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync($"{apiBaseUrl}/save_delivery", content);

                if (response.IsSuccessStatusCode)
                {
                    ShowInfoBar("Success", "Delivery saved successfully!", InfoBarSeverity.Success);
                    ResetForm();
                    
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    ShowInfoBar("Save Error", $"Failed to save delivery: {errorContent}", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("Save Error", $"An error occurred during save: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private async void ResetForm()
        {
            _fullOrderList.Clear();
            _filteredOrderList.Clear();
            OrderIDTextBox.Text = "";
            SearchTextBox.Text = "";
            DeliveryDatePicker.SelectedDate = DateTimeOffset.Now;
            SupplierIDTextBox.Text = "";
            SupplierNameTextBox.Text = "";
            overallDeliveryTextBox.Text = ""; 
            CheckedByTextBox.Text = "";
            await FetchAndFillDeliveryID();
            
        }

        private void OrderIDTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _ = FetchOrderItemsAsync(OrderIDTextBox.Text);
                e.Handled = true;
            }
        }

        private async void Back_Click(object sender, RoutedEventArgs e)
        {
            // Determine if the form has any user-entered data.
            bool isFormDirty = !string.IsNullOrWhiteSpace(OrderIDTextBox.Text) ||
                               !string.IsNullOrWhiteSpace(CheckedByTextBox.Text) ||
                               !string.IsNullOrWhiteSpace(overallDeliveryTextBox.Text) ||
                               _fullOrderList.Any(item => item.QuantityReceived.HasValue && item.QuantityReceived.Value > 0);

            if (isFormDirty)
            {
                var dialog = new ContentDialog
                {
                    Title = "Unsaved Changes",
                    Content = "You have unsaved changes. Are you sure you want to leave?",
                    PrimaryButtonText = "Leave Page",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // User confirmed, so navigate.
                    this.Frame.Navigate(typeof(DeliveryPage));
                }
                // If user cancels, do nothing.
            }
            else
            {
                // Form is clean, navigate immediately.
                this.Frame.Navigate(typeof(DeliveryPage));
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(SearchTextBox.Text);
        }

        private void ApplyFilter(string searchText)
        {
            _filteredOrderList.Clear();
            if (_fullOrderList.Any())
            {
                var filtered = string.IsNullOrWhiteSpace(searchText)
                   ? _fullOrderList
                   : _fullOrderList.Where(i =>
                       i.ItemId.ToLower().Contains(searchText.ToLower()) ||
                       i.ItemName.ToLower().Contains(searchText.ToLower()));

                foreach (var item in filtered)
                    _filteredOrderList.Add(item);
            }
        }
    }
}