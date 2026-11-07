using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace App_v1.Models
{
    class Inventory
    {// These properties are mapped directly to the JSON keys
        // coming from the FastAPI backend.
        [JsonPropertyName("product_code")]
        public string ProductCode { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; }

        [JsonPropertyName("branchID")]
        public string BranchID { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        /*
        // ⚠️ Commented out on purpose:
        // If we define only this custom constructor,
        // the compiler removes the default (parameterless) constructor.
        // System.Text.Json needs a parameterless constructor to create
        // objects during deserialization, so leaving this uncommented
        // would break JSON → Inventory mapping.
        
        public Inventory(string product_code, string product_name, string branchID, int quantity)
        {
            ProductCode = product_code;
            ProductName = product_name;
            BranchID = branchID;
            Quantity = quantity;
        }
        */
    }
}
