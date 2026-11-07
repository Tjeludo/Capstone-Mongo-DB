using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;

namespace App_v1.Helpers;

// A static helper class for all input validation across the app.
public static class Validator
{
    // Validates a category name.
    public static string ValidateCategoryName(string name, InfoBar infoBar)
    {
        // Must be 2-50 chars, allowing letters, spaces, and some special characters.
        const string pattern = @"^[A-Za-z &\-/'.]{2,50}$";
        return ValidateUsingRegex(name, pattern, "Category Name", "Name must be 2-50 characters.\nMust contain valid letters and symbols.", infoBar);
    }

    // Validates a description.
    public static string ValidateDescription(string description, InfoBar infoBar)
    {
        // Description is optional, so an empty string is fine.
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        // If provided, it must be between 1 and 255 chars.
        const string pattern = @"^[A-Za-z &\-/'.\n\r]{1,255}$";
        return ValidateUsingRegex(description, pattern, "Description", "Description must be 1-255 characters.\nMust contain valid letters and symbols.", infoBar, true, false);
    }

    // Validates a product name.
    public static string ValidateProductName(string name, InfoBar infoBar)
    {
        // Similar to category name but allows numbers.
        const string pattern = @"^[A-Za-z &\-/'.]{2,50}$";
        return ValidateUsingRegex(name, pattern, "Product Name", "Name must be 2-50 characters.\nMust contain valid letters and symbols.", infoBar);
    }

    // Validates a supplier's name.
    public static string ValidateSupplierName(string name, InfoBar infoBar)
    {
        // Allows letters, spaces, apostrophes, and hyphens.
        const string pattern = @"^[A-Za-z '-]{2,50}$";
        return ValidateUsingRegex(name, pattern, "Supplier Name", "Name must be 2-50 characters.\nMust contain valid letters and symbols.", infoBar);
    }

    // Validates a staff member's first name.
    public static string ValidateFirstName(string name, InfoBar infoBar)
    {
        const string pattern = @"^[A-Za-z'-]{2,50}$";
        return ValidateUsingRegex(name, pattern, "First Name", "First Name must be 2-50 characters.\nMust contain valid letters and symbols.", infoBar);
    }

    // Validates a staff member's surname.
    public static string ValidateSurname(string name, InfoBar infoBar)
    {
        const string pattern = @"^[A-Za-z'-]{2,50}$";
        return ValidateUsingRegex(name, pattern, "Surname", "Surname must be 2-50 characters.\nMust contain valid letters and symbols.", infoBar);
    }

    // Validates the name of a sales representative.
    public static string ValidateSalesRep(string name, InfoBar infoBar)
    {
        const string pattern = @"^[A-Za-z '-]{2,100}$";
        return ValidateUsingRegex(name, pattern, "Sales Rep Name", "Name must be 2-100 characters.\nMust contain valid letters and symbols.", infoBar);
    }

    // Validates an email address.
    public static string ValidateEmail(string email, InfoBar infoBar)
    {
        // A standard email regex pattern, case-insensitive.
        const string pattern = @"^(?i)[a-z0-9_.-]+@[a-z0-9-]+\.[a-z-.]{2,}$";
        return ValidateUsingRegex(email, pattern, "Email", "Please enter a valid email address.\nMust contain valid letters and symbols.", infoBar, false);
    }

    // Validates a 9-digit phone number (cannot start with zero).
    public static string ValidatePhoneNumber(string phone, InfoBar infoBar)
    {
        const string pattern = @"^[1-9]\d{8}$";
        return ValidateUsingRegex(phone, pattern, "Phone Number", "Phone number must be exactly 9 digits.\nMust not start with 0.", infoBar);
    }

    // Validates a street name and number.
    public static string ValidateStreet(string street, InfoBar infoBar)
    {
        // Allows a mix of numbers, letters, and common street punctuation.
        const string pattern = @"^[A-Za-z0-9\s.'-]{4,100}$";
        return ValidateUsingRegex(street, pattern, "Street/Road Name", "Street must be 4-100 characters.\nMust contain valid letters and symbols.", infoBar);
    }

    // Validates a city name.
    public static string ValidateCity(string city, InfoBar infoBar)
    {
        const string pattern = @"^[A-Za-z '-]{2,50}$";
        return ValidateUsingRegex(city, pattern, "City", "City name must be 2-50 characters.\nMust contain valid letters and symbols.", infoBar);
    }

    // Validates a country name.
    public static string ValidateCountry(string country, InfoBar infoBar)
    {
        const string pattern = @"^[A-Za-z '-]{4,56}$";
        return ValidateUsingRegex(country, pattern, "Country", "Country name must be 4-56 characters.\nMust contain valid letters and symbols.", infoBar);
    }

    // Validates a 4-digit zip code.
    public static string ValidateZipCode(string zipCode, InfoBar infoBar)
    {
        const string pattern = @"^\d{4}$";
        return ValidateUsingRegex(zipCode, pattern, "Zip Code", "Zip code must be exactly 4 digits.", infoBar);
    }

    // Validates address.
    public static string ValidateAddress(string address, InfoBar infoBar)
    {
        // Description is optional, so an empty string is fine.
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.Empty;
        }

        // If provided, it must be between 1 and 255 chars.
        const string pattern = @"^[A-Za-z0-9 ,\-/'.\n\r]{4,255}$";
        return ValidateUsingRegex(address, pattern, "Address", "Address must be 4-255 characters.\nMust contain valid letters and symbols.", infoBar, true, false);
    }

    // Checks if the selected date of birth makes the person at least 18 years old.
    public static string? ValidateDateOfBirth(DateTimeOffset? dob, InfoBar infoBar)
    {
        if (dob == null)
        {
            ShowValidationError(infoBar, "Date of Birth is Required", "Please select a date of birth.");
            return null;
        }

        if (dob.Value.Date > DateTime.Now.AddYears(-18).Date)
        {
            ShowValidationError(infoBar, "Invalid Date of Birth", "Staff member must be at least 18 years old.");
            return null;
        }

        return dob.Value.ToString("dd-MM-yyyy");
    }


    // Checks if the selected start date is not in the past.
    public static string? ValidateStartDate(DateTimeOffset? startDate, InfoBar infoBar)
    {
        if (startDate == null)
        {
            ShowValidationError(infoBar, "Start Date is Required", "Please select a start date.");
            return null;
        }

        if (startDate.Value.Date < DateTime.Now.Date)
        {
            ShowValidationError(infoBar, "Invalid Start Date", "Start date cannot be in the past.");
            return null;
        }

        return startDate.Value.ToString("dd-MM-yyyy");
    }

    // --- Private Helper Methods ---

    // The core validation engine. Reduces code duplication.
    private static string ValidateUsingRegex(string input, string pattern, string fieldName, string errorMessage, InfoBar infoBar, bool trim = true, bool isRequired = true)
    {
        try
        {
            // Fail if a required field is left empty.
            if (isRequired && string.IsNullOrWhiteSpace(input))
            {
                ShowValidationError(infoBar, $"{fieldName} is required", $"Please provide a value for {fieldName}.");
                return null;
            }

            // If a field is optional and empty, it's valid.
            if (!isRequired && string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            // Clean up whitespace before validation.
            var processedInput = trim ? input.Trim() : input;

            // If the input matches the pattern, return the cleaned input.
            if (Regex.IsMatch(processedInput, pattern))
            {
                return processedInput;
            }
            // Otherwise, show an error and return null.
            else
            {
                ShowValidationError(infoBar, $"Invalid {fieldName}", errorMessage);
                return null;
            }
        }
        catch (System.Exception)
        {
            ShowValidationError(infoBar, "System Error", "An unexpected error occurred during validation.");
            return null;
        }
    }

    // Displays an error message will now close automatically after 6 seconds on the InfoBar.
    private static void ShowValidationError(InfoBar infoBar, string title, string message)
    {
        if (infoBar != null)
        {
            infoBar.Title = title;
            infoBar.Message = message;
            infoBar.Severity = InfoBarSeverity.Error;
            infoBar.IsOpen = true;

            // Create a timer to close the InfoBar automatically.
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
            timer.Tick += (s, e) =>
            {
                infoBar.IsOpen = false;
                timer.Stop();
            };
            timer.Start();
        }
    }
}