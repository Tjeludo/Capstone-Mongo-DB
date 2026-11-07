using App_v1.Demo;
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
    public class TransferViewModel
    {
        private ObservableCollection<Transfer> _transfers;
        public ObservableCollection<Transfer> Transfers
        {
            get { return _transfers; }
            set { _transfers = value; }
        }

        public TransferViewModel()
        {
            _transfers = new ObservableCollection<Transfer>();
            // call load Transfers
            _ = LoadTransfers();
        }

        public async Task LoadTransfers()
        {
            using var client = new HttpClient();
            // This endpoint is defined in TransfersAPI.py
            var response = await client.GetFromJsonAsync<TransferAPIResponse<Transfer>>("http://127.0.0.1:8000/transfer");

            try
            {
                if (response?.data != null && response.success)
                {
                    Transfers.Clear();
                    foreach (var item in response.data)
                    {
                        Transfers.Add(item);
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

        public async Task<(bool success, string message)> makeTransfer(MakeTransfer newTransfer)
        {
            try
            {
                using var client = new HttpClient();
                var requestData = new { itemCode = newTransfer.itemCode, fromBranch = newTransfer.fromBranch, toBranch = newTransfer.toBranch, quantity = newTransfer.quantity };
                var response = await client.PostAsJsonAsync("http://127.0.0.1:8000/addTransfer", requestData);

                // ▼▼▼ CHANGE WAS MADE IN THIS BLOCK ▼▼▼
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        // 1. WHAT CHANGED: The JSON is now deserialized into the correct 'TransferAPIResponse' object.
                        //    The original code had an incorrect deserialization attempt that didn't work.
                        // 2. WHY: This allows us to access the 'success' and 'message' fields sent from the Python backend,
                        //    such as "Not enough inventory to transfer".
                        var responseData = JsonSerializer.Deserialize<TransferAPIResponse>(responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        // 3. WHAT CHANGED: We now check the 'success' flag from the backend's response.
                        // 4. WHY: The grid should only be refreshed if the transfer was truly successful on the server.
                        if (responseData.success)
                        {
                            await LoadTransfers(); // Refresh the grid data
                        }

                        // 5. WHAT CHANGED: Return the actual success status and message from the backend.
                        // 6. WHY: This passes the real result (e.g., "Successful" or an error message) back to the UI to be displayed.
                        //    The original code returned a hardcoded "Transfer successfully" message, ignoring the backend's response.
                        return (responseData.success, responseData.message);
                    }
                    catch (JsonException ex)
                    {
                        // This catch block handles cases where the server response is not valid JSON.
                        return (false, $"Failed to parse server response: {ex.Message}");
                    }
                }
                // ▲▲▲ END OF CHANGE ▲▲▲
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return (false, $"Failed to transfer: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Exception occurred: {ex.Message}");
            }
        }
    }


}