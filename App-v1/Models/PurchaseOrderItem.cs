// In App_v1.Models/PurchaseOrderItem.cs

using Microsoft.UI.Xaml.Controls; // Add this for InfoBarSeverity
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml;

namespace App_v1.Models
{
    public class PurchaseOrderItem : INotifyPropertyChanged
    {
        [JsonPropertyName("itemId")]
        public string ItemId { get; set; }

        [JsonPropertyName("itemName")]
        public string ItemName { get; set; }

        [JsonPropertyName("totalRequired")]
        public int TotalRequired { get; set; }

        [JsonPropertyName("alreadyDelivered")]
        public int AlreadyDelivered { get; set; }

        [JsonPropertyName("missingQuantity")]
        public int MissingQuantity { get; set; }

        private int? _quantityReceived;

        [JsonPropertyName("quantityReceived")]
        public int? QuantityReceived
        {
            get => _quantityReceived;
            set
            {
                if (_quantityReceived != value)
                {
                    _quantityReceived = value;
                    OnPropertyChanged();

                    if (value.HasValue)
                    {
                        if (value.Value < 0)
                        {
                            ValidationMessage = "Quantity cannot be negative.";
                            IsInvalid = true;
                        }
                        else if (value.Value > MissingQuantity)
                        {
                            string quantityType = AlreadyDelivered == 0 ? "required" : "missing";
                            ValidationMessage = $"Cannot exceed the {quantityType} quantity of {MissingQuantity}.";
                            IsInvalid = true;
                        }
                        else
                        {
                            ValidationMessage = string.Empty;
                            IsInvalid = false;
                        }
                    }
                    else
                    {
                        ValidationMessage = string.Empty;
                        IsInvalid = false;
                    }

                    OnPropertyChanged(nameof(IsInvalid));
                    OnPropertyChanged(nameof(ValidationMessage));
                }
            }
        }

        [JsonPropertyName("comments")]
        public string Comments { get; set; }

        [JsonIgnore]
        public string QuantityLabel => AlreadyDelivered == 0 ? "Required Quantity" : "Missing Quantity";

        [JsonIgnore]
        public bool IsInvalid { get; private set; }

        [JsonIgnore]
        public string ValidationMessage { get; private set; }

        // Property to control the InfoBar's appearance
        [JsonIgnore]
        public InfoBarSeverity Severity => InfoBarSeverity.Error;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DeliveryNumberResponse
    {
        public string deliveryNum { get; set; }
    }
}