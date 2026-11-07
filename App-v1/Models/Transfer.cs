using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace App_v1.Models
{
    public class Transfer
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("transferID")]
        public string TransferID { get; set; }

        // CORRECTED: from_branch
        [JsonPropertyName("from_branch")]
        public string FromBranch { get; set; }

        // CORRECTED: to_branch
        [JsonPropertyName("to_branch")]
        public string ToBranch { get; set; }

        [JsonPropertyName("product_code")]
        public string ItemCode { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; }

        // CORRECTED: quantity
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    public class MakeTransfer
    {
        public string itemCode { get; set; }
        public string fromBranch { get; set; }
        public string toBranch { get; set; }
        public int quantity { get; set; }
    }
}
