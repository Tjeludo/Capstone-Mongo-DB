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
using System.Text.Json.Serialization;


namespace App_v1.Models
{
    public class DistributionViewModel
    {
        private ObservableCollection<Distributions> distributions;
        public ObservableCollection<Distributions> Distributions
        {
            get { return distributions; }
            set { distributions = value; }
        }

        public DistributionViewModel()
        {
            distributions = new ObservableCollection<Distributions>();
            _= LoadDistribibutions();
        }

        public async Task LoadDistribibutions()
        {
            using var client = new HttpClient();
            var response = await client.GetFromJsonAsync<DistributionAPIResponse<Distributions>>("http://127.0.0.1:8000/distributions");

            try
            {
                if (response?.data != null && response.success)
                {
                    Distributions.Clear();
                    foreach (var item in response.data)
                    {
                        Distributions.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
        }
        public async Task<List<string>> GetProductNamesAsync()
        {
            using var client = new HttpClient();
            try
            {
                var response = await client.GetFromJsonAsync<ProductCodeResponse>(
                    "http://127.0.0.1:8000/getproductcode"
                );

                if (response?.data != null && response.success)
                {
                    return response.data; // List of product_name
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading product names: {ex.Message}");
            }

            return new List<string>();
        }



        public async Task<List<string>> GetBranchID()
        {
            using var client = new HttpClient();
            try
            {
                var response = await client.GetFromJsonAsync<BranchIDResponse>(
                    "http://127.0.0.1:8000/getbranchID"
                );

                if (response?.data != null && response.success)
                {
                    return response.data; // List of branchID
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading branch ID: {ex.Message}");
            }

            return new List<string>();
        }

        public async Task<(bool, string)> SubmitDistributionAsync(MakeDistributionRequest request)
        {
            using var client = new HttpClient();
            try
            {
                
                var response = await client.PostAsJsonAsync("http://127.0.0.1:8000/makeDistribution", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
                    return (result.Success, result.Message);
                }
                else
                {
                    
                    var errorResult = await response.Content.ReadFromJsonAsync<ApiResponse>();
                    return (false, errorResult?.Message ?? "An error occurred.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error submitting distribution: {ex.Message}");
                return (false, "Could not connect to the server.");
            }
        }

    }
}
