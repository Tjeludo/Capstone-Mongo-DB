using App_v1.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App_v1.Pages
{
    public sealed partial class Transfers : Page
    {
        public TransferViewModel ViewModel { get; set; }
        public Transfers()
        {
            InitializeComponent();
            ViewModel = this.DataContext as TransferViewModel;
        }
        private List<string> items = new List<string>();
        private List<string> branches = new List<string>();
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            items = await ViewModel.GetProductNamesAsync();
            branches = await ViewModel.GetBranchID();
        }

        private async void MakeTransferClick(object sender, RoutedEventArgs e)
        {
            // Close the InfoBar if it's open from a previous action
            TransferInfoBar.IsOpen = false;

            MakeTransferDialog.XamlRoot = this.XamlRoot;

            try
            {
                var result = await MakeTransferDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // --- 1. Gather and Validate All Inputs ---
                    string itemInput = itemCode.Text?.Trim();
                    string toBranchInput = toBranch.Text?.Trim();
                    string fromBranchInput = fromBranch.Text?.Trim();
                    double quantityValue = quantity.Value;

                    // 1a. Validate Item Code
                    if (string.IsNullOrWhiteSpace(itemInput))
                    {
                        ShowInfoBar("Validation Error", "Please enter an item code.", InfoBarSeverity.Error);
                        return;
                    }
                    var matchedItem = items.FirstOrDefault(i => i.Equals(itemInput, StringComparison.OrdinalIgnoreCase));
                    if (matchedItem == null)
                    {
                        ShowInfoBar("Validation Error", $"The item code '{itemInput}' is not valid.", InfoBarSeverity.Error);
                        return;
                    }

                    // 1b. Validate "To" Branch
                    if (string.IsNullOrWhiteSpace(toBranchInput))
                    {
                        ShowInfoBar("Validation Error", "Please enter a destination branch.", InfoBarSeverity.Error);
                        return;
                    }
                    var matchedToBranch = branches.FirstOrDefault(b => b.Equals(toBranchInput, StringComparison.OrdinalIgnoreCase));
                    if (matchedToBranch == null)
                    {
                        ShowInfoBar("Validation Error", $"The destination branch '{toBranchInput}' is not valid.", InfoBarSeverity.Error);
                        return;
                    }

                    // 1c. Validate "From" Branch
                    if (string.IsNullOrWhiteSpace(fromBranchInput))
                    {
                        ShowInfoBar("Validation Error", "Please enter a source branch.", InfoBarSeverity.Error);
                        return;
                    }
                    var matchedFromBranch = branches.FirstOrDefault(b => b.Equals(fromBranchInput, StringComparison.OrdinalIgnoreCase));
                    if (matchedFromBranch == null)
                    {
                        ShowInfoBar("Validation Error", $"The source branch '{fromBranchInput}' is not valid.", InfoBarSeverity.Error);
                        return;
                    }

                    // 1d. Validate that branches are not the same
                    if (matchedFromBranch.Equals(matchedToBranch, StringComparison.OrdinalIgnoreCase))
                    {
                        ShowInfoBar("Validation Error", "The source and destination branches cannot be the same.", InfoBarSeverity.Error);
                        return;
                    }

                    // 1e. Validate Quantity
                    if (quantityValue <= 0)
                    {
                        ShowInfoBar("Validation Error", "Please enter a quantity greater than 0.", InfoBarSeverity.Error);
                        return;
                    }

                    // --- 2. Build the Request with Validated Data ---
                    var newTransfer = new MakeTransfer
                    {
                        itemCode = matchedItem,       // Use the correctly cased item
                        toBranch = matchedToBranch,   // Use the correctly cased branch
                        fromBranch = matchedFromBranch, // Use the correctly cased branch
                        quantity = (int)quantityValue
                    };

                    // --- 3. Call the ViewModel ---
                    try
                    {
                        var (success, message) = await ViewModel.makeTransfer(newTransfer);

                        if (success)
                        {
                            ShowInfoBar("Success", message, InfoBarSeverity.Success);
                        }
                        else
                        {
                            ShowInfoBar("Transfer Failed", message ?? "An unexpected error occurred.", InfoBarSeverity.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowInfoBar("Application Error", $"An error occurred: {ex.Message}", InfoBarSeverity.Error);
                    }
                }
            }
            finally
            {
                // This code is guaranteed to run, clearing the form in all cases.
                ClearTransferForm();
            }
        }
        /// <summary>
        /// Displays a message in the InfoBar control and automatically closes it after a delay for non-error messages.
        /// </summary>
        private async void ShowInfoBar(string title, string message, InfoBarSeverity severity)
        {
            TransferInfoBar.Title = title;
            TransferInfoBar.Message = message;
            TransferInfoBar.Severity = severity;
            TransferInfoBar.IsOpen = true;

            // For success or informational messages, close the bar after 5 seconds.
            if (severity == InfoBarSeverity.Success || severity == InfoBarSeverity.Informational)
            {
                await Task.Delay(5000);
                TransferInfoBar.IsOpen = false;
            }
        }

        private void ClearTransferForm()
        {
            itemCode.Text = string.Empty;
            toBranch.Text = string.Empty;
            fromBranch.Text = string.Empty;
            quantity.Value = 1;
        }

        private void EditButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // TODO: Implement edit logic here
        }

        // Items Suggestive Box methods 
        private void Item_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var suggestions = items
                    .Where(i => i.StartsWith(sender.Text, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                sender.ItemsSource = suggestions;
            }
        }
        private void Item_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = args.SelectedItem.ToString();
        }
        private void Item_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string query = args.QueryText;
            System.Diagnostics.Debug.WriteLine($"User searched for: {query}");
        }

        // Branches Suggestive Box methods 
        private void Branches_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var suggestions = branches
                    .Where(i => i.StartsWith(sender.Text, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                sender.ItemsSource = suggestions;
            }
        }
        private void Branches_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = args.SelectedItem.ToString();
        }
        private void Branches_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string query = args.QueryText;
            System.Diagnostics.Debug.WriteLine($"User searched for: {query}");
        }
    }
}