// FileName: ItemInfo.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App_v1.Models;

public class ItemInfo : INotifyPropertyChanged
{
    private string _supplierId = string.Empty;
    private string _productCode = string.Empty;
    private string _description = string.Empty;
    private double _price;
    private string _lastUpdated = string.Empty;

    [JsonPropertyName("supplier_id")]
    public string SupplierId
    {
        get => _supplierId;
        set
        {
            if (_supplierId != value) { _supplierId = value; OnPropertyChanged(); }
        }
    }

    [JsonPropertyName("product_code")]
    public string ProductCode
    {
        get => _productCode;
        set
        {
            if (_productCode != value) { _productCode = value; OnPropertyChanged(); }
        }
    }

    [JsonPropertyName("description")]
    public string Description
    {
        get => _description;
        set
        {
            if (_description != value) { _description = value; OnPropertyChanged(); }
        }
    }

    [JsonPropertyName("price")]
    public double Price
    {
        get => _price;
        set
        {
            if (_price != value) { _price = value; OnPropertyChanged(); }
        }
    }

    [JsonPropertyName("last_updated")]
    public string LastUpdated
    {
        get => _lastUpdated;
        set
        {
            if (_lastUpdated != value) { _lastUpdated = value; OnPropertyChanged(); }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}