using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App_v1.Models;

/// <summary>
/// This is my data "model" for a staff member.
/// It's just a class to hold all the info for one person.
/// INotifyPropertyChanged is important - it lets the UI automatically update
/// when I change a value in the code (like changing a staff member's status).
/// </summary>
public class StaffInfo : INotifyPropertyChanged
{
    // These are the private backing fields that hold the actual data.
    // The public properties below provide controlled access to them.
    private string staff_id = string.Empty;
    private string branch_code = string.Empty;
    private string role = string.Empty;
    private string firstname = string.Empty;
    private string lastname = string.Empty;
    private string date_of_birth = string.Empty;
    private string gender = string.Empty;
    private string email = string.Empty;
    private string staff_email = string.Empty;
    private string phone_number = string.Empty;
    private string address = string.Empty;
    private string status = string.Empty;
    private string start_date = string.Empty;

    // The [JsonPropertyName("...")] attribute is key. It tells the JSON tool how to match
    // the "snake_case" names from my Python API to my "PascalCase" C# properties.
    [JsonPropertyName("staff_id")]
    public string Id
    {
        get => staff_id;
        // The setter updates the private field and then calls OnPropertyChanged.
        // OnPropertyChanged notifies the UI that this property has changed, so it can redraw if needed.
        set
        {
            staff_id = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("branch_code")]
    public string BranchCode
    {
        get => branch_code;
        set
        {
            branch_code = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("role")]
    public string RoleCode
    {
        get => role;
        set
        {
            role = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("firstname")]
    public string FirstName
    {
        get => firstname;
        set
        {
            firstname = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("lastname")]
    public string LastName
    {
        get => lastname;
        set
        {
            lastname = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("date_of_birth")]
    public string DateOfBirth
    {
        get => date_of_birth;
        set
        {
            date_of_birth = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("gender")]
    public string Gender
    {
        get => gender;
        set
        {
            gender = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("email")] // This is the personal email.
    public string PersonalEmail
    {
        get => email;
        set
        {
            email = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("staff_email")] // This is the official staff email, used for display in the grid.
    public string Email
    {
        get => staff_email;
        set
        {
            staff_email = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("phone_number")]
    public string PhoneNumber
    {
        get => phone_number;
        set
        {
            phone_number = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("address")]
    public string Address
    {
        get => address;
        set
        {
            address = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("status")]
    public string Status
    {
        get => status;
        set
        {
            status = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("start_date")]
    public string StartDate
    {
        get => start_date;
        set
        {
            start_date = value; OnPropertyChanged();
        }
    }

    // This event is part of the INotifyPropertyChanged interface.
    // The UI subscribes to this event to listen for property changes.
    public event PropertyChangedEventHandler? PropertyChanged;

    // This helper method raises the PropertyChanged event.
    // [CallerMemberName] automatically gets the name of the property that called this method (e.g., "FirstName"),
    // so I don't have to type it manually.
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
