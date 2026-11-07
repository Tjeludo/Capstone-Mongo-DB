using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App_v1.Models
{
    internal class BranchIDResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public List<string> data { get; set; } = new List<string>();
    }
}
