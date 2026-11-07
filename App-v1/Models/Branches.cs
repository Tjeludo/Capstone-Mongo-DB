using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace App_v1.Models
{
    public class Branch
    {
        [JsonPropertyName("branchID")]
        public string BranchID { get; set; }

        [JsonPropertyName("location")]
        public string Location { get; set; }

        [JsonPropertyName("contactDetails")]
        public string ContactDetails { get; set; }

        [JsonPropertyName("manager")]
        public string Manager { get; set; }
    }

}

