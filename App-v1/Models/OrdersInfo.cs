using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App_v1.Models;

// Represents a single order record. This class is used for data binding in the UI.
// The INotifyPropertyChanged interface lets the UI know when a property has changed.
public class OrdersInfo : INotifyPropertyChanged
{
    private string _orderId = string.Empty;
    private string _supplierId = string.Empty;
    private List<object> _products = new();
    private double _totalCost;
    private string _status = string.Empty;
    private string _orderDate = string.Empty;
    private string _orderedBy = string.Empty;
    private string _expectDelivery = string.Empty;

    // The JsonPropertyName attribute maps the C# property name (Id)
    // to the JSON field name from the API (order_id).
    [JsonPropertyName("order_id")]
    public string Id
    {
        get => _orderId;
        set
        {
            _orderId = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("supplier_id")]
    public string SupplierId
    {
        get => _supplierId;
        set
        {
            _supplierId = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("products")]
    public List<object> Products
    {
        get => _products;
        set
        {
            _products = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("total_cost(R)")]
    public double TotalCost
    {
        get => _totalCost;
        set
        {
            _totalCost = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("status")]
    public string Status
    {
        get => _status;
        set
        {
            _status = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("order_date")]
    public string OrderDate
    {
        get => _orderDate;
        set
        {
            _orderDate = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("ordered_by")]
    public string OrderedBy
    {
        get => _orderedBy;
        set
        {
            _orderedBy = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("expect_delivery")]
    public string ExpectDelivery
    {
        get => _expectDelivery;
        set
        {
            _expectDelivery = value; OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    // This method fires the PropertyChanged event, telling the UI to update.
    // The [CallerMemberName] attribute automatically fills in the property's name.
    private void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}