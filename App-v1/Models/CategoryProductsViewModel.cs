using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace App_v1.Models;

public class CategoryProductsViewModel
{
    private const string BaseUrl = "http://127.0.0.1:8000"; // Corrected BaseUrl

    public ObservableCollection<ProductInfo> CategoryProducts
    {
        get; set;
    }
    public string CategoryName { get; private set; } = string.Empty;
    public string CategoryCode { get; private set; } = string.Empty;

    public CategoryProductsViewModel()
    {
        CategoryProducts = new ObservableCollection<ProductInfo>();
    }

    public async Task LoadCategoryProductsAsync(string categoryCode)
    {
        try
        {
            CategoryCode = categoryCode;

            using var client = new HttpClient();

            // Corrected the URL to use the BaseUrl constant
            var response = await client.GetFromJsonAsync<ApiResponse<List<ProductInfo>>>($"{BaseUrl}/categories/view?id={categoryCode}");

            if (response?.Data != null)
            {
                CategoryProducts.Clear();
                foreach (var product in response.Data)
                {
                    CategoryProducts.Add(product);
                }

                // Set category name from first product if available
                if (CategoryProducts.Count > 0)
                {
                    CategoryName = GetCategoryNameFromCode(categoryCode);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading category products: {ex.Message}");
        }
    }

    public async Task<byte[]?> DownloadCsvAsync()
    {
        try
        {
            using var client = new HttpClient();

            // Create request body with category ID for category-specific download
            var requestData = new { id = CategoryCode };
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"{BaseUrl}/categories/view/download", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error downloading category CSV: {ex.Message}");
            return null;
        }
    }

    private string GetCategoryNameFromCode(string categoryCode)
    {
        // This could be enhanced by loading the category info from the API
        // For now, just return a formatted version
        return $"Category {categoryCode}";
    }
}