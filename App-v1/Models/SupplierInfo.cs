using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App_v1.Models;

/// <summary>
/// This defines the "shape" of a single supplier.
/// It's used to hold data from the API and to bind to the UI.
/// INotifyPropertyChanged is what allows the UI to update automatically
/// when a supplier's details change.
/// </summary>
public class SupplierInfo : INotifyPropertyChanged
{
    // These are the private variables that hold the actual data.
    private string supplier_id = string.Empty;
    private string supplier_name = string.Empty;
    private string email = string.Empty;
    private string phone_number = string.Empty;
    private string address = string.Empty;
    private string sales_rep = string.Empty;

    // The [JsonPropertyName] attribute tells the JSON serializer how to map
    // the snake_case names from the Python API to my PascalCase C# properties.

    [JsonPropertyName("supplier_id")]
    public string ID
    {
        get => supplier_id;
        set
        {
            supplier_id = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("supplier_name")]
    public string Name
    {
        get => supplier_name;
        set
        {
            supplier_name = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("email")]
    public string Email
    {
        get => email;
        set
        {
            email = value; OnPropertyChanged();
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

    [JsonPropertyName("sales_rep")]
    public string SalesRep
    {
        get => sales_rep;
        set
        {
            sales_rep = value; OnPropertyChanged();
        }
    }

    // This is the core mechanism for INotifyPropertyChanged.
    public event PropertyChangedEventHandler? PropertyChanged;

    // When a property's 'set' is called, this method is triggered to
    // raise the PropertyChanged event, telling the UI to refresh.
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}