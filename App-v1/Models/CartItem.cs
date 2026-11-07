using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace App_v1.Models;

public class CartItem : INotifyPropertyChanged
{
    private int _quantity;
    public string SupplierId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Price
    {
        get; set;
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(TotalText));
            }
        }
    }

    public decimal Total => Price * Quantity;

    public string PriceText => Price.ToString("C");
    public string TotalText => Total.ToString("C");

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
