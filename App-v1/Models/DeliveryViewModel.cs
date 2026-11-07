using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;

namespace App_v1.Models
{
    public class DeliveryViewModel
    {

        public ObservableCollection<Delivery> Deliveries
        {
            get; set;
        }


        public DeliveryViewModel()
        {
            Deliveries = new ObservableCollection<Delivery>();
            LoadDeliveries();

        }

        public async void LoadDeliveries()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var deliveries = await client.GetFromJsonAsync<List<Delivery>>("http://localhost:8000/get_deliveries");

                    // Sort the list using LINQ after fetching it
                    var sortedDeliveries = deliveries
                        .OrderByDescending(d => d.Date)
                        .ThenByDescending(d => d.DeliveryId);

                    Deliveries.Clear();
                    foreach (var d in sortedDeliveries)
                    {
                        Deliveries.Add(d);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error or display message to user
                System.Diagnostics.Debug.WriteLine($"Error loading deliveries: {ex.Message}");
            }
        }
    }

    public class MissingItem
    {
        public string itemcode
        {
            get; set;
        }
        public string itemName
        {
            get; set;
        }
        public int missing_quantity
        {
            get; set;
        }
    }
}