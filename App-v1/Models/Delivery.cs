using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace App_v1.Models
{
    public class Delivery
    {
        string deliveryId;
        string supplierId;
        string supplierName;
        string order_id;
        string date;
        string checkedBy;
        string comments;

        public string DeliveryId
        {
            get
            {
                return deliveryId;
            }
            set
            {
                deliveryId = value;
            }
        }
        public string SupplierId
        {
            get
            {
                return supplierId;
            }
            set
            {
                supplierId = value;
            }
        }
        public string SupplierName
        {
            get
            {
                return supplierName;
            }
            set
            {
                supplierName = value;
            }
        }
        public string Order_id
        {
            get
            {
                return order_id;
            }
            set
            {
                order_id = value;
            }
        }
        public string CheckedBy
        {
            get
            {
                return checkedBy;
            }
            set
            {
                checkedBy = value;
            }
        }
        public string Date
        {
            get
            {
                return date;
            }
            set
            {
                date = value;
            }
        }
        public string Comments
        {
            get
            {
                return comments;
            }
            set
            {
                comments = value;
            }
        }

        [JsonPropertyName("DeliveryItems")]
        public List<DeliveryItem> Items { get; set; } = new();
        public List<MissingItem> MissingItems { get; set; } = new();

        public bool IsComplete => MissingItems == null || MissingItems.Count == 0;

        public Visibility CompleteVisibility => IsComplete ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PartialVisibility => IsComplete ? Visibility.Collapsed : Visibility.Visible;
    }

    public class DeliveryItem
    {
        public string item_id
        {
            get; set;
        }
        public string itemName
        {
            get; set;
        }
        public int quantity
        {
            get; set;
        }
        public string comments
        {
            get; set;
        }
    }
}