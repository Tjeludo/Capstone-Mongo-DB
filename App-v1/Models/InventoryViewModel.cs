using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Input;
using Windows.Storage;

namespace App_v1.Models
{
    class InventoryViewModel
    {
        private ObservableCollection<Inventory> inventories;
        public ObservableCollection<Inventory> Inventories
        {
            get { return inventories; }
            set { inventories = value; }
        }

        public InventoryViewModel()
        {
           inventories = new ObservableCollection<Inventory>();
            // call the load inventory method
            _ = LoadInventory();

        }

        public async Task LoadInventory()
        {
            using var client = new HttpClient();
            var response = await client.GetFromJsonAsync<InventoryAPIResponse<Inventory>>("http://127.0.0.1:8000/inventory");

            try
            {
                if (response?.data != null && response.success)
                {
                    Inventories.Clear();
                    foreach (var item in response.data)
                    {
                        Inventories.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
        }
    }
}
