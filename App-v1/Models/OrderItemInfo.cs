// Models/OrderItemInfo.cs

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App_v1.Models;

public class OrderItemInfo : INotifyPropertyChanged
{
    private string _supplierId = string.Empty;
    private string _productCode = string.Empty;
    private string _description = string.Empty;
    private decimal _price;
    private int _quantity;
    private decimal _subtotal;

    [JsonPropertyName("supplier_id")]
    public string SupplierId
    {
        get => _supplierId;
        set
        {
            _supplierId = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("product_code")]
    public string ProductCode
    {
        get => _productCode;
        set
        {
            _productCode = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("description")]
    public string Description
    {
        get => _description;
        set
        {
            _description = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("price")]
    public decimal Price
    {
        get => _price;
        set
        {
            _price = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("quantity")]
    public int Quantity
    {
        get => _quantity;
        set
        {
            _quantity = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal
    {
        get => _subtotal;
        set
        {
            _subtotal = value; OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}