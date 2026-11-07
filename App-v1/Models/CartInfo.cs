using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace App_v1.Models;

public class CartInfo : OrderItem, INotifyPropertyChanged
{
    private int _quantity;
    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Subtotal));
            }
        }
    }

    public decimal Subtotal => Price * Quantity;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}