using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace App_v1.Models
{
    // In Models/Distributions.cs

    public class DistributionItem
    {
        
        [JsonPropertyName("product_code")]
        public string ProductCode { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("branch")]
        public string ToBranch { get; set; }
        public DistributionItem() { }

        
        public DistributionItem(string itemCode,string productname, int quantity, string toBranch)
        {
            this.ProductCode = itemCode;
            this.Quantity = quantity;
            this.ToBranch = toBranch;
            this.ProductName = productname;
        }
    }
    // In Models/Distributions.cs

    public class Distributions
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("distributionID")]
        public string DistributionID { get; set; }
    }

    public class BranchDistribution
    {
        [JsonPropertyName("branchID")]
        public string BranchID { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    // Represents the entire request body to be sent to the API
    public class MakeDistributionRequest
    {
        [JsonPropertyName("itemCode")]
        public string ItemCode { get; set; }

        [JsonPropertyName("distributions")]
        public List<BranchDistribution> Distributions { get; set; }
    }

    // A generic class to handle the API's success/failure response
    public class ApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
