using App_v1.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App_v1.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Distributions : Page
{
    public DistributionViewModel ViewModel { get; set; }
    public Distributions()
    {
        InitializeComponent();
        ViewModel = this.DataContext as DistributionViewModel;
    }

    private List<string> items = new List<string>();
    private List<string> branches = new List<string>();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        items = await ViewModel.GetProductNamesAsync();
        branches = await ViewModel.GetBranchID();
        await ViewModel.LoadDistribibutions(); // Ensure data is loaded
    }

    private async void MakeDistributionClick(object sender, RoutedEventArgs e)
    {
        // Hide previous status messages
        StatusInfoBar.IsOpen = false;

        // Generate dynamic inputs for branches before showing the dialog
        GenerateBranchQuantityInputs();

        MakeDistributionDialog.XamlRoot = this.XamlRoot;

        try
        {
            var result = await MakeDistributionDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // --- 1. Gather and Validate Data from UI ---
                var userInput = ItemSuggestBox.Text.Trim(); // Get user input and remove whitespace

                // 1a. Validate that an item was entered at all
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    ShowStatusMessage("Error", "Please select an item to distribute.", InfoBarSeverity.Error);
                    return;
                }

                // 1b. Find a case-insensitive match from the master list of product codes
                var matchedItem = items.FirstOrDefault(i => i.Equals(userInput, StringComparison.OrdinalIgnoreCase));

                // 1c. Validate that the entered item exists in the list
                if (matchedItem == null)
                {
                    ShowStatusMessage("Validation Error", $"The item code '{userInput}' is not valid. Please choose from the suggestion list.", InfoBarSeverity.Error);
                    return; // Stop execution if the item is not valid
                }

                // --- If validation passes, proceed to build the request ---
                var request = new MakeDistributionRequest
                {
                    ItemCode = matchedItem, // Use the correctly cased item code from our list
                    Distributions = new List<BranchDistribution>()
                };

                // Loop through the dynamically created NumberBoxes
                foreach (var row in BranchQuantitiesPanel.Children.OfType<StackPanel>())
                {
                    var numberBox = row.Children.OfType<NumberBox>().FirstOrDefault();
                    if (numberBox != null && numberBox.Value > 0)
                    {
                        request.Distributions.Add(new BranchDistribution
                        {
                            BranchID = numberBox.Tag.ToString(),
                            Quantity = (int)numberBox.Value
                        });
                    }
                }

                if (request.Distributions.Count == 0)
                {
                    ShowStatusMessage("Validation Error", "Please enter a quantity for at least one branch.", InfoBarSeverity.Error);
                    return;
                }

                // --- 2. Call the ViewModel ---
                var (success, message) = await ViewModel.SubmitDistributionAsync(request);

                // --- 3. Handle the Result ---
                if (success)
                {
                    ShowStatusMessage("Success", message, InfoBarSeverity.Success);
                    await ViewModel.LoadDistribibutions(); // Refresh the data grid
                }
                else
                {
                    ShowStatusMessage("Error", message, InfoBarSeverity.Error);
                }
            }
        }
        finally
        {
            // This is guaranteed to run, clearing the form in all cases.
            ClearDistributionForm();
        }
    }

    private void DetailsButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        // FIX: The model for a single distribution item is likely 'Distribution', not 'Distributions'
        if (button?.Tag is App_v1.Models.Distributions distribution)
        {
            this.Frame.Navigate(typeof(DistributionsDetailsPage), distribution.DistributionID);
        }
    }

    private void GenerateBranchQuantityInputs()
    {
        BranchQuantitiesPanel.Children.Clear();
        foreach (var branch in branches)
        {
            if (branch != "B001") // Only create input for non-main branches
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                var label = new TextBlock { Text = branch, Width = 100, VerticalAlignment = VerticalAlignment.Center };
                var numberBox = new NumberBox
                {
                    Width = 150,
                    Value = 0,
                    Minimum = 0,
                    Maximum = 1000,
                    SmallChange = 1,
                    SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                    Tag = branch // Use Tag to store the branch ID
                };
                row.Children.Add(label);
                row.Children.Add(numberBox);
                BranchQuantitiesPanel.Children.Add(row);
            }
        }
    }

    private void ClearDistributionForm()
    {
        ItemSuggestBox.Text = string.Empty;
        BranchQuantitiesPanel.Children.Clear();
    }

    // Helper method to show messages in the InfoBar
    private async void ShowStatusMessage(string title, string message, InfoBarSeverity severity)
    {
        StatusInfoBar.Title = title;
        StatusInfoBar.Message = message;
        StatusInfoBar.Severity = severity;
        StatusInfoBar.IsOpen = true;

        // Auto-close for success messages
        if (severity == InfoBarSeverity.Success)
        {
            await Task.Delay(5000);
            StatusInfoBar.IsOpen = false;
        }
    }

    #region AutoSuggestBox Handlers
    private void Control2_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var suggestions = items.Where(i => i.StartsWith(sender.Text, StringComparison.OrdinalIgnoreCase)).ToList();
            sender.ItemsSource = suggestions;
        }
    }

    private void Control2_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        sender.Text = args.SelectedItem.ToString();
    }

    private void Control2_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        System.Diagnostics.Debug.WriteLine($"User searched for: {args.QueryText}");
    }
    #endregion
}