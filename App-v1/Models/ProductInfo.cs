using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App_v1.Models;

/*
  A simple class that represents a single product.
  
  This is my data model for a product. It's used for:
  - Turning JSON from the API into a C# object.
  - Binding to the DataGrid columns in the UI.
  - INotifyPropertyChanged lets the UI automatically update if a property (like Name or Price) changes.
*/
public class ProductInfo : INotifyPropertyChanged
{
    // These are the private variables that hold the actual data.
    private string product_code = string.Empty;
    private string product_name = string.Empty;
    private string category_code = string.Empty;
    private decimal price;
    private string size = string.Empty;
    private string colour = string.Empty;

    // These attributes tell the JSON serializer how to match the API's field names
    // (like "product_code") to my nicer property names (like "Id").
    [JsonPropertyName("product_code")]
    public string Id
    {
        get => product_code;
        set
        {
            product_code = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("product_name")]
    public string Name
    {
        get => product_name;
        set
        {
            product_name = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("category_code")]
    public string Category
    {
        get => category_code;
        set
        {
            category_code = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("price(ZAR)")]
    public decimal Price
    {
        get => price;
        set
        {
            price = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("size")]
    public string Size
    {
        get => size;
        set
        {
            size = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("colour")]
    public string Colour
    {
        get => colour;
        set
        {
            colour = value; OnPropertyChanged();
        }
    }

    /*
      This is the magic for INotifyPropertyChanged.
      When a property's `set` is called, it also calls OnPropertyChanged.
      This raises an event that the UI is listening for, telling it to refresh.
    */
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}