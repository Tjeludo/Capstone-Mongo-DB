// In Models/DistributionsDetailsViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace App_v1.Models
{
    public class DistributionsDetailsViewModel
    {
        // Renamed to be more descriptive and match the XAML binding
        public ObservableCollection<DistributionItem> DistributionItems { get; set; }

        public DistributionsDetailsViewModel()
        {
            DistributionItems = new ObservableCollection<DistributionItem>();
        }

        public async Task LoadDistributionDetailsAsync(string distributionId)
        {
            if (string.IsNullOrEmpty(distributionId)) return;

            using var client = new HttpClient();
            try
            {
                // In DistributionsDetailsViewModel.cs
                var url = $"http://127.0.0.1:8000/distributionDetails?distributionID={distributionId}";
                var response = await client.GetFromJsonAsync<DistributionDetailResponse>(url);
                if (response?.Data != null && response.Success)
                {
                    DistributionItems.Clear();
                    foreach (var item in response.Data)
                    {
                        DistributionItems.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error (e.g., log it)
                System.Diagnostics.Debug.WriteLine($"Error loading distribution details: {ex.Message}");
            }
        }
    }
}