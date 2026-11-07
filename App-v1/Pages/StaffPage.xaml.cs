using App_v1.Helpers;
using App_v1.Models;
using App_v1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace App_v1.Pages
{
    /// <summary>
    /// This is the "code-behind" file for my StaffPage.xaml.
    /// It contains the C# logic that handles user interactions and events from the UI (like button clicks).
    /// It works closely with the StaffViewModel to process data and update the UI.
    /// </summary>
    public sealed partial class StaffPage : Page
    {
        // A private property to easily and safely access the page's ViewModel.
        private StaffViewModel ViewModel => (StaffViewModel)DataContext;

        /// <summary>
        /// The constructor is the first method called when the page is created.
        /// It initializes the UI and subscribes to events from the ViewModel.
        /// This lets the ViewModel tell the page when to open dialogs.
        /// </summary>
        public StaffPage()
        {
            this.InitializeComponent();

            ViewModel.PromoteRequested += OnPromoteRequested;
            ViewModel.TransferRequested += OnTransferRequested;
            ViewModel.EditRequested += OnEditRequested;
            ViewModel.DeleteRequested += OnDeleteRequested;
        }

        // --- Grid Action Button Click Handlers ---

        private async void PromoteStaff_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: StaffInfo staff })
            {
                await OnPromoteRequested(staff);
            }
        }

        private async void TransferStaff_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: StaffInfo staff })
            {
                await OnTransferRequested(staff);
            }
        }

        private async void EditStaff_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: StaffInfo staff })
            {
                await OnEditRequested(staff);
            }
        }

        private async void DeleteStaff_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: StaffInfo staff })
            {
                await OnDeleteRequested(staff);
            }
        }

        // --- Add Staff Dialog Logic ---

        private async void AddStaff_Click(object sender, RoutedEventArgs e)
        {
            // Do some checks before showing the form.
            if (ViewModel.Branches == null || !ViewModel.Branches.Any())
            {
                ShowNotification("Error", "No branches available. Please add branches first.", InfoBarSeverity.Error);
                return;
            }
            if (ViewModel.Roles == null || !ViewModel.Roles.Any())
            {
                ShowNotification("Error", "No roles available. Please add roles first.", InfoBarSeverity.Error);
                return;
            }

            // Always start with a clean form.
            NotificationInfoBar.IsOpen = false;
            StaffBranchCodeComboBox.SelectedItem = null;
            StaffRoleCodeComboBox.SelectedItem = null;
            StaffFirstNameTextBox.Text = string.Empty;
            StaffSurnameTextBox.Text = string.Empty;
            StaffGenderComboBox.SelectedIndex = -1;
            StaffDOBPicker.SelectedDate = null;
            StaffEmailTextBox.Text = string.Empty;
            StaffCountryCodeComboBox.SelectedIndex = -1;
            StaffPhoneTextBox.Text = string.Empty;
            StaffStreetTextBox.Text = string.Empty;
            StaffSuburbTextBox.Text = string.Empty;
            StaffCityTextBox.Text = string.Empty;
            StaffCountryTextBox.Text = string.Empty;
            StaffZipCodeTextBox.Text = string.Empty;
            StaffStatusComboBox.SelectedIndex = -1;
            StaffStartDatePicker.SelectedDate = DateTime.Now;

            ValidateAddForm(); // This will disable the "Save" button initially.

            AddStaffDialog.XamlRoot = this.XamlRoot;
            await AddStaffDialog.ShowAsync();
        }

        private async void AddStaffDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();
            args.Cancel = true; // Keep dialog open by default. I'll close it manually on success.

            // --- Full validation sequence. If one fails, it stops. ---
            var validatedFirstName = Validator.ValidateFirstName(StaffFirstNameTextBox.Text, NotificationInfoBar);
            if (validatedFirstName == null) { deferral.Complete(); return; }

            var validatedSurname = Validator.ValidateSurname(StaffSurnameTextBox.Text, NotificationInfoBar);
            if (validatedSurname == null) { deferral.Complete(); return; }

            var validatedDob = Validator.ValidateDateOfBirth(StaffDOBPicker.SelectedDate, NotificationInfoBar);
            if (validatedDob == null) { deferral.Complete(); return; }

            var validatedEmail = Validator.ValidateEmail(StaffEmailTextBox.Text, NotificationInfoBar);
            if (validatedEmail == null) { deferral.Complete(); return; }

            var validatedPhone = Validator.ValidatePhoneNumber(StaffPhoneTextBox.Text, NotificationInfoBar);
            if (validatedPhone == null) { deferral.Complete(); return; }

            var validatedStreet = Validator.ValidateStreet(StaffStreetTextBox.Text, NotificationInfoBar);
            if (validatedStreet == null) { deferral.Complete(); return; }

            var validatedCity = Validator.ValidateCity(StaffCityTextBox.Text, NotificationInfoBar);
            if (validatedCity == null) { deferral.Complete(); return; }

            var validatedCountry = Validator.ValidateCountry(StaffCountryTextBox.Text, NotificationInfoBar);
            if (validatedCountry == null) { deferral.Complete(); return; }

            var validatedZipCode = Validator.ValidateZipCode(StaffZipCodeTextBox.Text, NotificationInfoBar);
            if (validatedZipCode == null) { deferral.Complete(); return; }

            var validatedStartDate = Validator.ValidateStartDate(StaffStartDatePicker.SelectedDate, NotificationInfoBar);
            if (validatedStartDate == null) { deferral.Complete(); return; }

            // --- If validation passes, build the new staff object ---
            var countryCode = (StaffCountryCodeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            string[] addressParts = { validatedStreet, StaffSuburbTextBox.Text.Trim(), validatedCity, validatedCountry, validatedZipCode };
            var fullAddress = string.Join(", ", addressParts.Where(part => !string.IsNullOrWhiteSpace(part)));

            var newStaff = new StaffInfo
            {
                BranchCode = (StaffBranchCodeComboBox.SelectedItem as BranchInfo)?.Id ?? string.Empty,
                RoleCode = (StaffRoleCodeComboBox.SelectedItem as RolesInfo)?.RoleAbbreviation ?? string.Empty,
                FirstName = validatedFirstName,
                LastName = validatedSurname,
                Gender = (StaffGenderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                DateOfBirth = validatedDob,
                PersonalEmail = validatedEmail,
                PhoneNumber = $"{countryCode}{validatedPhone}",
                Address = fullAddress,
                Status = (StaffStatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.ToLower() ?? string.Empty,
                StartDate = validatedStartDate
            };

            var (success, message) = await ViewModel.AddStaffAsync(newStaff);
            if (success)
            {
                ShowNotification("Success", message, InfoBarSeverity.Success);
                args.Cancel = false; // It worked, so let the dialog close.
            }
            else
            {
                ShowNotification("Error", message, InfoBarSeverity.Error);
            }
            deferral.Complete();
        }

        // --- Edit Staff Dialog Logic ---

        private async Task OnEditRequested(StaffInfo staff)
        {
            NotificationInfoBar.IsOpen = false;
            EditStaffDialog.Tag = staff; // Store the staff object in the dialog's Tag so I can get it back on save.

            // Pre-fill the form with the staff member's current info.
            EditStaffIdTextBox.Text = staff.Id;
            EditStaffBranchCodeTextBox.Text = staff.BranchCode;
            EditStaffRoleCodeTextBox.Text = staff.RoleCode;
            EditStaffFirstNameTextBox.Text = staff.FirstName;
            EditStaffSurnameTextBox.Text = staff.LastName;
            SelectComboBoxItemByContent(EditStaffGenderComboBox, staff.Gender);

            // Pre-fill Date of Birth
            if (DateTime.TryParseExact(staff.DateOfBirth, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
            {
                EditStaffDOBPicker.SelectedDate = dob;
            }

            // Pre-fill Staff Email
            EditStaffEmailTextBox.Text = staff.Email;

            // --- Logic to split the phone number into code and number for the UI ---
            var phone = staff.PhoneNumber ?? "";
            if (phone.StartsWith("+27"))
            {
                EditStaffCountryCodeComboBox.SelectedIndex = 0; // +27
                EditStaffPhoneTextBox.Text = phone.Substring(3);
            }
            else if (phone.StartsWith("+263"))
            {
                EditStaffCountryCodeComboBox.SelectedIndex = 1; // +263
                EditStaffPhoneTextBox.Text = phone.Substring(4);
            }
            else
            {
                EditStaffCountryCodeComboBox.SelectedIndex = -1; // No recognized code
                EditStaffPhoneTextBox.Text = phone;
            }

            EditStaffAddressTextBox.Text = staff.Address;
            SelectComboBoxItemByContent(EditStaffStatusComboBox, staff.Status, true); // Use ignoreCase for 'Active' vs 'active'

            ValidateEditForm();
            EditStaffDialog.XamlRoot = this.XamlRoot;
            await EditStaffDialog.ShowAsync();
        }

        private async void EditStaffDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();
            args.Cancel = true;

            if (sender.Tag is StaffInfo staff)
            {
                // --- Apply the same validation logic as the 'Add' form to the 'Edit' form ---
                var validatedFirstName = Validator.ValidateFirstName(EditStaffFirstNameTextBox.Text, NotificationInfoBar);
                if (validatedFirstName == null) { deferral.Complete(); return; }

                var validatedSurname = Validator.ValidateSurname(EditStaffSurnameTextBox.Text, NotificationInfoBar);
                if (validatedSurname == null) { deferral.Complete(); return; }

                // ADDED: Date of Birth validation for the edit form.
                var validatedDob = Validator.ValidateDateOfBirth(EditStaffDOBPicker.SelectedDate, NotificationInfoBar);
                if (validatedDob == null) { deferral.Complete(); return; }

                var validatedPhone = Validator.ValidatePhoneNumber(EditStaffPhoneTextBox.Text, NotificationInfoBar);
                if (validatedPhone == null) { deferral.Complete(); return; }

                var validatedAddress = Validator.ValidateAddress(EditStaffAddressTextBox.Text, NotificationInfoBar);
                if (validatedAddress == null) { deferral.Complete(); return; }

                // --- If validation passes, update the original staff object ---
                var countryCode = (EditStaffCountryCodeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

                staff.FirstName = validatedFirstName;
                staff.LastName = validatedSurname;
                staff.Gender = (EditStaffGenderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
                staff.DateOfBirth = validatedDob; // Update the Date of Birth
                staff.PhoneNumber = $"{countryCode}{validatedPhone}";
                staff.Address = validatedAddress;
                staff.Status = (EditStaffStatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.ToLower() ?? string.Empty;

                var (success, message) = await ViewModel.UpdateStaffAsync(staff);
                if (success)
                {
                    ShowNotification("Success", message, InfoBarSeverity.Success);
                    args.Cancel = false; // Close the dialog on success.
                }
                else
                {
                    ShowNotification("Error", message, InfoBarSeverity.Error);
                }
            }
            deferral.Complete();
        }

        // --- Delete Staff Logic ---

        private async Task OnDeleteRequested(StaffInfo staff)
        {
            var dialog = new ContentDialog
            {
                Title = "Confirm Deletion",
                Content = $"Are you sure you want to delete staff member '{staff.FirstName} {staff.LastName}'?\n\nThis action cannot be undone.",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var (success, message) = await ViewModel.DeleteStaffAsync(staff.Id);
                ShowNotification(success ? "Success" : "Error", message, success ? InfoBarSeverity.Success : InfoBarSeverity.Error);
            }
        }

        // --- Other dialog logic for Transfer and Promote ---

        private async Task OnTransferRequested(StaffInfo staff)
        {
            if (ViewModel.Branches == null || ViewModel.Branches.Count < 2)
            {
                ShowNotification("Error", "Not enough branches available to perform a transfer.", InfoBarSeverity.Error);
                return;
            }

            TransferStaffDialog.Tag = staff;
            TransferStaffIdTextBox.Text = staff.Id;
            TransferCurrentBranchTextBox.Text = staff.BranchCode;
            TransferNewBranchComboBox.SelectedItem = null;

            ValidateTransferForm();

            TransferStaffDialog.XamlRoot = this.XamlRoot;
            await TransferStaffDialog.ShowAsync();
        }

        private async void TransferStaffDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();
            args.Cancel = true;

            if (sender.Tag is StaffInfo staff && TransferNewBranchComboBox.SelectedItem is BranchInfo selectedBranch)
            {
                if (selectedBranch.Id == staff.BranchCode)
                {
                    ShowNotification("Validation Error", "New branch must be different from the current branch.", InfoBarSeverity.Error);
                    deferral.Complete();
                    return;
                }

                var (success, message) = await ViewModel.TransferStaffAsync(staff, selectedBranch.Id);
                if (success)
                {
                    ShowNotification("Success", message, InfoBarSeverity.Success);
                    args.Cancel = false;
                }
                else
                {
                    ShowNotification("Error", message, InfoBarSeverity.Error);
                }
            }
            deferral.Complete();
        }

        private async Task OnPromoteRequested(StaffInfo staff)
        {
            if (ViewModel.Roles == null || !ViewModel.Roles.Any())
            {
                ShowNotification("Error", "No roles are available for promotion.", InfoBarSeverity.Error);
                return;
            }

            PromoteStaffDialog.Tag = staff;
            PromoteStaffIdTextBox.Text = staff.Id;
            PromoteCurrentRoleTextBox.Text = staff.RoleCode;
            PromoteNewRoleComboBox.SelectedItem = null;

            ValidatePromoteForm();

            PromoteStaffDialog.XamlRoot = this.XamlRoot;
            await PromoteStaffDialog.ShowAsync();
        }

        private async void PromoteStaffDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();
            args.Cancel = true;

            if (sender.Tag is StaffInfo staff && PromoteNewRoleComboBox.SelectedItem is RolesInfo selectedRole)
            {
                if (selectedRole.RoleAbbreviation == staff.RoleCode)
                {
                    ShowNotification("Validation Error", "New role must be different from the current role.", InfoBarSeverity.Error);
                    deferral.Complete();
                    return;
                }

                var (success, message) = await ViewModel.PromoteStaffAsync(staff, selectedRole.RoleAbbreviation);
                if (success)
                {
                    ShowNotification("Success", message, InfoBarSeverity.Success);
                    args.Cancel = false;
                }
                else
                {
                    ShowNotification("Error", message, InfoBarSeverity.Error);
                }
            }
            deferral.Complete();
        }


        // --- Real-Time Form Validation (to enable/disable save buttons) ---

        private void AddForm_InputChanged(object sender, object e) => ValidateAddForm();
        private void EditForm_InputChanged(object sender, object e) => ValidateEditForm();
        private void TransferForm_InputChanged(object sender, SelectionChangedEventArgs e) => ValidateTransferForm();
        private void PromoteForm_InputChanged(object sender, SelectionChangedEventArgs e) => ValidatePromoteForm();

        private void ValidateAddForm() => AddStaffDialog.IsPrimaryButtonEnabled = AreAddFormFieldsValid();
        private bool AreAddFormFieldsValid() =>
            StaffBranchCodeComboBox.SelectedItem != null &&
            StaffRoleCodeComboBox.SelectedItem != null &&
            !string.IsNullOrWhiteSpace(StaffFirstNameTextBox.Text) &&
            !string.IsNullOrWhiteSpace(StaffSurnameTextBox.Text) &&
            StaffGenderComboBox.SelectedItem != null &&
            StaffDOBPicker.SelectedDate != null &&
            !string.IsNullOrWhiteSpace(StaffEmailTextBox.Text) &&
            StaffCountryCodeComboBox.SelectedItem != null &&
            !string.IsNullOrWhiteSpace(StaffPhoneTextBox.Text) &&
            !string.IsNullOrWhiteSpace(StaffStreetTextBox.Text) &&
            !string.IsNullOrWhiteSpace(StaffCityTextBox.Text) &&
            !string.IsNullOrWhiteSpace(StaffCountryTextBox.Text) &&
            !string.IsNullOrWhiteSpace(StaffZipCodeTextBox.Text) &&
            StaffStatusComboBox.SelectedItem != null &&
            StaffStartDatePicker.SelectedDate != null;

        private void ValidateEditForm() => EditStaffDialog.IsPrimaryButtonEnabled = AreEditFormFieldsValid();
        private bool AreEditFormFieldsValid() =>
            !string.IsNullOrWhiteSpace(EditStaffFirstNameTextBox.Text) &&
            !string.IsNullOrWhiteSpace(EditStaffSurnameTextBox.Text) &&
            EditStaffGenderComboBox.SelectedItem != null &&
            EditStaffCountryCodeComboBox.SelectedItem != null &&
            !string.IsNullOrWhiteSpace(EditStaffPhoneTextBox.Text) &&
            !string.IsNullOrWhiteSpace(EditStaffAddressTextBox.Text) &&
            EditStaffStatusComboBox.SelectedItem != null;

        private void ValidateTransferForm() => TransferStaffDialog.IsPrimaryButtonEnabled = TransferNewBranchComboBox.SelectedItem != null;
        private void ValidatePromoteForm() => PromoteStaffDialog.IsPrimaryButtonEnabled = PromoteNewRoleComboBox.SelectedItem != null;

        // --- CSV and Utility Methods ---

        private async void DownloadCSV_Click(object sender, RoutedEventArgs e)
        {
            SetButtonBusyState(DownloadButton, DownloadContent, DownloadBusyIndicator, true);
            try
            {
                var csvData = await ViewModel.DownloadCsvAsync();

                if (csvData is { Length: > 0 })
                {
                    var savePicker = new FileSavePicker();
                    WinRT.Interop.InitializeWithWindow.Initialize(savePicker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance));

                    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    savePicker.FileTypeChoices.Add("CSV Files", new List<string> { ".csv" });
                    savePicker.SuggestedFileName = $"staff_{DateTime.Now:dd_MM_yyyy_HHmmss}.csv";

                    StorageFile file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        await FileIO.WriteBytesAsync(file, csvData);
                        ShowNotification("Download Complete", "The CSV file has been saved successfully.", InfoBarSeverity.Success);
                    }
                }
                else
                {
                    ShowNotification("No Data", "No staff data available to download.", InfoBarSeverity.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowNotification("Download Error", $"An error occurred: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                SetButtonBusyState(DownloadButton, DownloadContent, DownloadBusyIndicator, false);
            }
        }

        private async void UploadCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance));
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".csv");

                StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    var fileProperties = await file.GetBasicPropertiesAsync();
                    if (fileProperties.Size > 10 * 1024 * 1024) // 10MB limit
                    {
                        ShowNotification("File Too Large", "Please select a file smaller than 10MB.", InfoBarSeverity.Error);
                        return;
                    }

                    SetButtonBusyState(UploadButton, UploadContent, UploadBusyIndicator, true);
                    try
                    {
                        var (success, message) = await ViewModel.UploadCsvAsync(file);
                        ShowNotification(success ? "Upload Complete" : "Upload Error", message, success ? InfoBarSeverity.Success : InfoBarSeverity.Error);
                    }
                    finally
                    {
                        SetButtonBusyState(UploadButton, UploadContent, UploadBusyIndicator, false);
                    }
                }
            }
            catch (Exception ex)
            {
                SetButtonBusyState(UploadButton, UploadContent, UploadBusyIndicator, false);
                ShowNotification("Error", $"An unexpected error occurred: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        // A reusable helper to show a notification that closes itself.
        private void ShowNotification(string title, string message, InfoBarSeverity severity)
        {
            NotificationInfoBar.Title = title;
            NotificationInfoBar.Message = message;
            NotificationInfoBar.Severity = severity;
            NotificationInfoBar.IsOpen = true;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (s, e) => {
                NotificationInfoBar.IsOpen = false;
                timer.Stop();
            };
            timer.Start();
        }

        // A helper to manage the busy state of the CSV buttons
        private void SetButtonBusyState(Button button, FrameworkElement content, UIElement indicator, bool isBusy)
        {
            button.IsEnabled = !isBusy;
            content.Visibility = isBusy ? Visibility.Collapsed : Visibility.Visible;
            indicator.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            if (indicator is Microsoft.UI.Xaml.Controls.ProgressRing ring)
            {
                ring.IsActive = isBusy;
            }
        }


        // A helper to select an item in a ComboBox based on its string content.
        private void SelectComboBoxItemByContent(ComboBox comboBox, string content, bool ignoreCase = false)
        {
            if (string.IsNullOrEmpty(content)) return;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var item = comboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(c => c.Content.ToString().Equals(content, comparison));
            if (item != null)
            {
                comboBox.SelectedItem = item;
            }
        }

        // This is important to prevent memory leaks when I navigate away from the page.
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.TransferRequested -= OnTransferRequested;
                ViewModel.EditRequested -= OnEditRequested;
                ViewModel.DeleteRequested -= OnDeleteRequested;
                ViewModel.PromoteRequested -= OnPromoteRequested;
            }
        }
    }
}