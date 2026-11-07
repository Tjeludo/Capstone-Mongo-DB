using App_v1.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace App_v1.Pages
{
    public sealed partial class StockTakeForm : Page
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string apiBaseUrl = "http://127.0.0.1:8000";

        private ObservableCollection<InventoryItem> _fullInventoryList = new ObservableCollection<InventoryItem>();
        private ObservableCollection<InventoryItem> _filteredInventoryList = new ObservableCollection<InventoryItem>();

        // Flag to track if the user has made any changes.
        private bool _isFormDirty = false;

        public StockTakeForm()
        {
            this.InitializeComponent();
            this.Loaded += StockTakeForm_Loaded;
            InventoryItemsListView.ItemsSource = _filteredInventoryList;
        }

        private async void StockTakeForm_Loaded(object sender, RoutedEventArgs e)
        {
            _isFormDirty = false; // The form is clean on load.
            StockTakeDatePicker.SelectedDate = DateTimeOffset.Now;
            await FetchAndFillStockTakeID();
        }

        private async Task FetchAndFillStockTakeID()
        {
            try
            {
                var response = await client.GetStringAsync($"{apiBaseUrl}/generate_stocktake_number");
                var result = JsonSerializer.Deserialize<StockTakeNumberResponse>(response);
                StockIDTextBox.Text = result?.stockTakeNum ?? $"ST-{DateTime.Now:yyyyMMddHHmmss}";
            }
            catch
            {
                StockIDTextBox.Text = $"ST-{DateTime.Now:yyyyMMddHHmmss}";
            }
        }

        private async void ShowInfoBar(string title, string message, InfoBarSeverity severity)
        {
            PageInfoBar.Title = title;
            PageInfoBar.Message = message;
            PageInfoBar.Severity = severity;
            PageInfoBar.IsOpen = true;

            if (severity == InfoBarSeverity.Success)
            {
                await Task.Delay(5000);
                PageInfoBar.IsOpen = false;
            }

            else if (severity == InfoBarSeverity.Error)
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

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var missingFields = new List<string>();

            if (BranchComboBox.SelectedValue == null || string.IsNullOrWhiteSpace(BranchComboBox.SelectedValue.ToString()))
            {
                missingFields.Add("Branch");
            }

            if (!_fullInventoryList.Any(item => item.CountedQuantity > 0))
            {
                missingFields.Add("Counted Items");
            }

            if (missingFields.Any())
            {
                string fieldList = string.Join(", ", missingFields);
                string message = $"Please fill in all required fields ({fieldList})";
                ShowInfoBar("Input Required", message, InfoBarSeverity.Warning);
                return;
            }

            foreach (var item in _fullInventoryList)
            {
                if (item.CountedQuantity > item.QuantityOnHand)
                {
                    ShowInfoBar(
                        "Data Error",
                        $"Item '{item.itemName}' count ({item.CountedQuantity}) cannot exceed quantity on hand ({item.QuantityOnHand}).",
                        InfoBarSeverity.Error
                    );
                    return;
                }
            }

            try
            {
                string stockTakeNum = StockIDTextBox.Text;
                string branchID = BranchComboBox.SelectedValue?.ToString() ?? "";
                string stockTakeDate = StockTakeDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");

                var items = _fullInventoryList
                    .Where(i => i.CountedQuantity > 0)
                    .Select(item => new
                    {
                        itemID = item.itemID,
                        itemName = item.itemName,
                        previousQuantity = item.QuantityOnHand,
                        quantityOnHand = item.CountedQuantity
                    }).ToList();

                var stockData = new
                {
                    stockTakeNum,
                    branchId = branchID,
                    date = stockTakeDate,
                    items
                };

                string json = JsonSerializer.Serialize(stockData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync($"{apiBaseUrl}/save_stocktake", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
                    string successMessage = $"Stock Take saved successfully! Stock Take Number: {result?["stockTakeNum"] ?? stockTakeNum}";
                    ShowInfoBar("Success", successMessage, InfoBarSeverity.Success);
                    ResetForm();
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    ShowInfoBar("Error", $"Failed to save Stock Take: {response.StatusCode}\n{errorContent}", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar("Error", $"An error occurred: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private async void ResetForm()
        {
            _fullInventoryList.Clear();
            _filteredInventoryList.Clear();
            BranchComboBox.SelectedItem = "";
            SearchTextBox.Text = "";
            StockTakeDatePicker.SelectedDate = DateTimeOffset.Now;
            await FetchAndFillStockTakeID();
            _isFormDirty = false; // The form is clean after a save/reset.
        }

        private async Task FetchInventoryItemsAsync(string branchID)
        {
            if (string.IsNullOrWhiteSpace(branchID))
            {
                _fullInventoryList.Clear();
                _filteredInventoryList.Clear();
                return;
            }

            try
            {
                string requestUrl = $"{apiBaseUrl}/get_inventory_by_branch?branchId={Uri.EscapeDataString(branchID)}";
                HttpResponseMessage response = await client.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var items = JsonSerializer.Deserialize<List<InventoryItem>>(responseBody, options) ?? new List<InventoryItem>();

                    _fullInventoryList.Clear();
                    foreach (var item in items)
                        _fullInventoryList.Add(item);

                    ApplyFilter(SearchTextBox.Text);
                }
                else
                {
                    _fullInventoryList.Clear();
                    _filteredInventoryList.Clear();
                    string errorContent = await response.Content.ReadAsStringAsync();
                    ShowInfoBar("Not Found", $"Could not find stock take items: {response.StatusCode}\n{errorContent}", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                _fullInventoryList.Clear();
                _filteredInventoryList.Clear();
                ShowInfoBar("Error", $"An error occurred while fetching items: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void BranchComboBox_SelectionChanged(object sender, Syncfusion.UI.Xaml.Editors.ComboBoxSelectionChangedEventArgs e)
        {
            if (BranchComboBox.SelectedValue != null)
            {
                _isFormDirty = true; // Mark form as dirty when branch is selected.
                string selectedBranchID = BranchComboBox.SelectedValue.ToString();
                _ = FetchInventoryItemsAsync(selectedBranchID);
            }
            else
            {
                _fullInventoryList.Clear();
                _filteredInventoryList.Clear();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(SearchTextBox.Text);
        }

        private void ApplyFilter(string searchText)
        {
            searchText = searchText?.Trim().ToLower() ?? "";

            _filteredInventoryList.Clear();
            var filtered = string.IsNullOrEmpty(searchText)
                ? _fullInventoryList
                : _fullInventoryList.Where(i => i.itemID.ToLower().Contains(searchText) || i.itemName.ToLower().Contains(searchText.ToLower()));

            foreach (var item in filtered)
                _filteredInventoryList.Add(item);
        }

        private async void CountedQuantity_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is NumberBox numberBox)
            {
                await ValidateCountedQuantity(numberBox);
            }
        }

        private async void CountedQuantity_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (sender is NumberBox numberBox)
                {
                    await ValidateCountedQuantity(numberBox);
                    e.Handled = true;
                }
            }
        }

        private async Task ValidateCountedQuantity(NumberBox numberBox)
        {
            _isFormDirty = true; // Mark form as dirty when a quantity is edited.
            if (numberBox.DataContext is not InventoryItem item) return;

            await Task.CompletedTask;

            if (double.TryParse(numberBox.Text, out double inputValue))
            {
                if (inputValue < 0)
                {
                    ShowInfoBar("Validation Error", "The quantity entered needs to be positive.", InfoBarSeverity.Error);
                }
                else if (inputValue > item.QuantityOnHand)
                {
                    ShowInfoBar("Validation Error", "Counted quantity exceeds the quantity on hand.", InfoBarSeverity.Error);
                }
            }
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if the user has made any changes.
            if (_isFormDirty)
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

                // If the user clicks "Leave Page", then navigate back.
                if (result == ContentDialogResult.Primary)
                {
                    this.Frame.GoBack();
                }
                // Otherwise, do nothing and stay on the page.
            }
            else
            {
                // If the form is not dirty, navigate back without a prompt.
                this.Frame.GoBack();
            }
        }
    }
}