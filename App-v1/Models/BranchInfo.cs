/*
This is my main data model for a single branch.
I implemented INotifyPropertyChanged so that the UI automatically
updates whenever a property like 'Manager' or 'City' changes. This is
essential for data binding to work smoothly.
*/
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App_v1.Models;

public partial class BranchInfo : INotifyPropertyChanged
{
    private string branch_code = string.Empty;
    private string branch_manager = string.Empty;
    private string branch_city = string.Empty;
    private string email = string.Empty;
    private string phone_number = string.Empty;
    private string address = string.Empty;

    /*
    Using JsonPropertyName to map the API's snake_case
    fields to my PascalCase properties.
    */
    [JsonPropertyName("branch_code")]
    public string Id
    {
        get => branch_code;
        set
        {
            /*
            I check if the value has actually changed before raising
            the PropertyChanged event to avoid unnecessary UI updates.
            The '?? string.Empty' ensures I never assign a null value.
            */
            if (branch_code != value)
            {
                branch_code = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("branch_manager")]
    public string Manager
    {
        get => branch_manager;
        set
        {
            if (branch_manager != value)
            {
                branch_manager = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("branch_city")]
    public string City
    {
        get => branch_city;
        set
        {
            if (branch_city != value)
            {
                branch_city = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("email")]
    public string Email
    {
        get => email;
        set
        {
            if (email != value)
            {
                email = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("phone_number")]
    public string Phone
    {
        get => phone_number;
        set
        {
            if (phone_number != value)
            {
                phone_number = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("address")]
    public string Address
    {
        get => address;
        set
        {
            if (address != value)
            {
                address = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /*
    I'm using [CallerMemberName] here. It's a handy attribute that
    automatically passes the name of the calling property (e.g., "Manager"),
    so I don't have to type it manually and risk making a typo.
    */
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}