using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System;
using System.Threading.Tasks;

namespace App_v1.Models
{
    public class BranchesViewModel
    {
        // Change from ObservableCollection<Branches> to ObservableCollection<Branch>
        private ObservableCollection<Branch> branches;
        public ObservableCollection<Branch> Branches
        {
            get { return branches; }
            set { branches = value; }
        }

        public BranchesViewModel()
        {
            // Change here as well
            branches = new ObservableCollection<Branch>();
            _ = LoadBranches();
        }

        public async Task LoadBranches()
        {
            using var client = new HttpClient();
            // Update the generic type here
            var response = await client.GetFromJsonAsync<BranchesAPIResponse<Branch>>("http://127.0.0.1:8000/branch");

            try
            {
                if (response?.data != null && response.success)
                {
                    Branches.Clear();
                    foreach (var item in response.data)
                    {
                        Branches.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading branches: {ex.Message}");
            }
        }

        // Update the parameter type here
        public async Task<(bool success, string message)> AddBranch(Branch newBranch)
        {
            try
            {
                using var client = new HttpClient();
                // We can now send the whole object, but your API expects a specific structure, so we keep this
                var requestData = new { location = newBranch.Location, contactDetails = newBranch.ContactDetails, manager = newBranch.Manager };
                var response = await client.PostAsJsonAsync("http://127.0.0.1:8000/addBranch", requestData);
                if (response.IsSuccessStatusCode)
                {
                    await LoadBranches();
                    return (true, "Branch added successfully");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return (false, $"Failed to add branch: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Exception occurred: {ex.Message}");
            }
        }

        // Update the parameter type here
        public async Task<(bool success, string message)> UpdateBranch(Branch updatedBranch)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.PutAsJsonAsync("http://127.0.0.1:8000/updateBranch", updatedBranch);

                if (response.IsSuccessStatusCode)
                {
                    await LoadBranches();
                    return (true, "Branch updated successfully.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorDoc = JsonDocument.Parse(errorContent);
                        if (errorDoc.RootElement.TryGetProperty("message", out var messageElement))
                        {
                            return (false, messageElement.GetString());
                        }
                    }
                    catch { /* Fallback */ }
                    return (false, $"Failed to update branch: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"An exception occurred: {ex.Message}");
            }
        }
    }
}