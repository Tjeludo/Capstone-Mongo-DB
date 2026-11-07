using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App_v1.Models;

public partial class SupplierProductInfo : INotifyPropertyChanged
{
    private string supplier_id = string.Empty;
    private string product_code = string.Empty;
    private double price;
    private string description = string.Empty;
    private string last_updated = string.Empty;

    [JsonPropertyName("supplier_id")]
    public string SupplierId
    {
        get => supplier_id;
        set
        {
            supplier_id = value ?? string.Empty;
            OnPropertyChanged(nameof(SupplierId));
        }
    }

    [JsonPropertyName("product_code")]
    public string ProductCode
    {
        get => product_code;
        set
        {
            product_code = value ?? string.Empty;
            OnPropertyChanged(nameof(ProductCode));
        }
    }

    [JsonPropertyName("price")]
    public double Price
    {
        get => price;
        set
        {
            price = value;
            OnPropertyChanged(nameof(Price));
        }
    }

    [JsonPropertyName("description")]
    public string Description
    {
        get => description;
        set
        {
            description = value ?? string.Empty;
            OnPropertyChanged(nameof(Description));
        }
    }

    [JsonPropertyName("last_updated")]
    public string LastUpdated
    {
        get => last_updated;
        set
        {
            last_updated = value ?? string.Empty;
            OnPropertyChanged(nameof(LastUpdated));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}