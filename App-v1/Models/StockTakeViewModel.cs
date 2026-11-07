using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;

namespace App_v1.Models
{
    public class StockTakeViewModel
    {
        public ObservableCollection<LocationBranch> Branches
        {
            get; set;
        }
        public ObservableCollection<StockTake> StockTakes
        {
            get; set;
        }

        public StockTakeViewModel()
        {
            Branches = new ObservableCollection<LocationBranch>();
            StockTakes = new ObservableCollection<StockTake>();
            LoadBranches();
            LoadStockTakes();
        }

        public async void LoadStockTakes()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var stockTakes = await client.GetFromJsonAsync<List<StockTake>>("http://localhost:8000/get_stocktakes");
                    StockTakes.Clear();

                    // Sort by date descending (most recent first)
                    var sortedStockTakes = stockTakes.OrderByDescending(st => DateTime.Parse(st.Date))
                                            .ThenByDescending(st => st.StockTakeNum);

                    foreach (var st in sortedStockTakes)
                    {
                        StockTakes.Add(st);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading stock takes: {ex.Message}");
            }
        }

        public async void LoadBranches()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var branches = await client.GetFromJsonAsync<List<LocationBranch>>("http://localhost:8000/get_branches");
                    Branches.Clear();
                    foreach (var b in branches)
                    {
                        Branches.Add(b);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error or display message to user
                System.Diagnostics.Debug.WriteLine($"Error loading branches: {ex.Message}");
            }
        }
    }

    public class LocationBranch
    {
        [JsonPropertyName("branchId")]
        public string BranchCode
        {
            get; set;
        }

        [JsonPropertyName("branchName")]
        public string BranchCity
        {
            get; set;
        }
    }

    public class StockTake
    {
        [JsonPropertyName("stockTakeNum")]
        public string StockTakeNum
        {
            get; set;
        }

        [JsonPropertyName("branchID")]
        public string BranchID
        {
            get; set;
        }

        [JsonPropertyName("date")]
        public string Date
        {
            get; set;
        }

        [JsonPropertyName("items")]
        public List<StockTakeItem> Items { get; set; } = new();
    }

    public class StockTakeItem
    {
        [JsonPropertyName("itemID")]
        public string ItemID
        {
            get; set;
        }
        [JsonPropertyName("itemName")]
        public string ItemName
        {
            get; set;
        }
        [JsonPropertyName("previousQuantity")]
        public int PreviousQuantity
        {
            get; set;
        }
        [JsonPropertyName("quantityOnHand")]
        public int quantityOnHand
        {
            get; set;
        }
    }
}