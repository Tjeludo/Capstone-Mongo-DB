using App_v1.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace App_v1.Pages;

public sealed partial class Branches : Page
{
    public BranchesViewModel ViewModel { get; set; }
    public Branches()
    {
        InitializeComponent();
        ViewModel = (BranchesViewModel)this.DataContext;
    }

    private async void AddBranchClick(object sender, RoutedEventArgs e)
    {
        // ... (code inside this method doesn't need to change much, but the type of newBranch does)
        BranchInfoBar.IsOpen = false;
        AddBranchDialog.XamlRoot = this.XamlRoot;
        // ...
        try
        {
            var result = await AddBranchDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // --- Validation ---
                if (string.IsNullOrWhiteSpace(locationNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(ContactDetailsTextBox.Text) ||
                    string.IsNullOrWhiteSpace(ManagerTextBox.Text))
                {
                    ShowInfoBar("Validation Error", "All fields are required.", InfoBarSeverity.Error);
                    return;
                }

                // Update the type here from "new Branch" to "new Models.Branch"
                var newBranch = new Models.Branch
                {
                    Location = locationNameTextBox.Text.Trim(),
                    ContactDetails = ContactDetailsTextBox.Text.Trim(),
                    Manager = ManagerTextBox.Text.Trim()
                };
                var (success, message) = await ViewModel.AddBranch(newBranch);
                // ... (rest of the method is fine)
                if (success)
                {
                    ShowInfoBar("Success", message, InfoBarSeverity.Success);
                    ClearAddBranchForm();
                }
                else
                {
                    ShowInfoBar("Error", message ?? "An unknown error occurred.", InfoBarSeverity.Error);
                    ClearAddBranchForm();
                }
            }
        }
        finally
        {
            ClearAddBranchForm();
        }

    }

    private async void ShowInfoBar(string title, string message, InfoBarSeverity severity)
    {
        // ... (this method is fine)
        BranchInfoBar.Title = title;
        BranchInfoBar.Message = message;
        BranchInfoBar.Severity = severity;
        BranchInfoBar.IsOpen = true;

        // For success or informational messages, close the bar after 5 seconds.
        if (severity == InfoBarSeverity.Success || severity == InfoBarSeverity.Informational)
        {
            await Task.Delay(5000);
            BranchInfoBar.IsOpen = false;
        }

    }

    private void ClearAddBranchForm()
    {
        locationNameTextBox.Text = string.Empty;
        ContactDetailsTextBox.Text = string.Empty;
        ManagerTextBox.Text = string.Empty;
    }

    private async void EditBranchClick(object sender, RoutedEventArgs e)
    {
        BranchInfoBar.IsOpen = false;

        if (BranchesDataGrid.SelectedItem == null)
        {
            ShowInfoBar("Selection Required", "Please select a branch from the grid to edit.", InfoBarSeverity.Warning);
            return;
        }

        // CRITICAL CHANGE: Cast to your model "Branch", not the page "Branches"
        var selectedBranch = (Branch)BranchesDataGrid.SelectedItem;
        editLocationNameTextBox.Text = selectedBranch.Location;
        editContactDetailsTextBox.Text = selectedBranch.ContactDetails;
        editManagerTextBox.Text = selectedBranch.Manager;

        EditBranchDialog.XamlRoot = this.XamlRoot;
        var result = await EditBranchDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(editLocationNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(editContactDetailsTextBox.Text) ||
                string.IsNullOrWhiteSpace(editManagerTextBox.Text))
            {
                ShowInfoBar("Validation Error", "All fields are required.", InfoBarSeverity.Error);
                return;
            }

            // Update the type here as well
            var updatedBranch = new Branch
            {
                BranchID = selectedBranch.BranchID,
                Location = editLocationNameTextBox.Text.Trim(),
                ContactDetails = editContactDetailsTextBox.Text.Trim(),
                Manager = editManagerTextBox.Text.Trim()
            };

            var (success, message) = await ViewModel.UpdateBranch(updatedBranch);

            if (success)
            {
                ShowInfoBar("Success", message, InfoBarSeverity.Success);
            }
            else
            {
                ShowInfoBar("Error", message ?? "An unknown error occurred.", InfoBarSeverity.Error);
            }
        }
    }
}