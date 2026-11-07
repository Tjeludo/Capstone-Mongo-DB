using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App_v1.Models
{
    public class BranchesAPIResponse<T>
    {
        public bool success { get; set; }
        public string message { get; set; }
        public List<T> data { get; set; } = new List<T>();
    }
}
