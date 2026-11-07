// CategoryInfo.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App_v1.Models;

// The data model for a single category.
// Used for API data, UI binding, and notifying the UI of changes.
public class CategoryInfo : INotifyPropertyChanged
{
    // Private backing fields for properties.
    private string category_code = string.Empty;
    private string category_name = string.Empty;
    private string description = string.Empty;
    private int products;

    // Maps the 'Id' property to the 'category_code' JSON field from the API.
    [JsonPropertyName("category_code")]
    public string Id
    {
        get => category_code; set
        {
            category_code = value; OnPropertyChanged();
        }
    }

    // Maps the 'Name' property to the 'category_name' JSON field.
    [JsonPropertyName("category_name")]
    public string Name
    {
        get => category_name; set
        {
            category_name = value; OnPropertyChanged();
        }
    }

    // Maps the 'Description' property to the 'description' JSON field.
    [JsonPropertyName("description")]
    public string Description
    {
        get => description; set
        {
            description = value; OnPropertyChanged();
        }
    }

    // Maps the 'Products' property to the 'products' JSON field.
    [JsonPropertyName("products")]
    public int Products
    {
        get => products; set
        {
            products = value; OnPropertyChanged();
        }
    }

    // This event allows the UI to automatically update when a property changes.
    public event PropertyChangedEventHandler? PropertyChanged;

    // Raises the PropertyChanged event.
    // [CallerMemberName] automatically uses the name of the property that called it.
    private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}