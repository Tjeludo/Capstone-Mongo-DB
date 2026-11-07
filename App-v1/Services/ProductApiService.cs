using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using App_v1.Models;
using Windows.Storage;

namespace App_v1.Services;

/*
  This is my one-stop-shop for talking to the backend Products API.
  
  Its job is to:
  - Handle all the HTTP requests (GET, POST, DELETE, etc.).
  - Convert my C# objects to JSON to send to the server.
  - Convert JSON from the server back into C# objects.
  - Deal with file uploads and downloads.
  - Centralize error handling for API calls so I don't repeat code everywhere.
*/
public class ProductApiService
{
    // I use a single HttpClient for the whole app for better performance.
    private readonly HttpClient _httpClient;
    // The root URL for my API. I can swap this to point to a live server later.
    private const string BaseUrl = "http://127.0.0.1:8000";

    public ProductApiService()
    {
        _httpClient = new HttpClient();
    }

    // Gets the full list of products from the `/products` endpoint.
    public async Task<ApiResponse<List<ProductInfo>>> GetProductsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ProductInfo>>>($"{BaseUrl}/products");
            return response ?? new ApiResponse<List<ProductInfo>> { Success = false, Message = "No data returned from server." };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting products: {ex.Message}");
            return new ApiResponse<List<ProductInfo>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }

    // Gets the list of categories, mostly for populating dropdowns.
    public async Task<ApiResponse<List<CategoryInfo>>> GetCategoriesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<CategoryInfo>>>($"{BaseUrl}/categories");
            return response ?? new ApiResponse<List<CategoryInfo>> { Success = false, Message = "No category data returned from server." };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting categories: {ex.Message}");
            return new ApiResponse<List<CategoryInfo>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }

    // Sends a new product to the `/products/add` endpoint.
    public async Task<ApiResponse<object>> AddProductAsync(ProductInfo newProduct)
    {
        try
        {
            // The API expects a specific JSON structure, so I build an anonymous object that matches it.
            var requestData = new
            {
                name = newProduct.Name,
                category_code = newProduct.Category,
                price = newProduct.Price,
                size = newProduct.Size,
                colour = newProduct.Colour
            };

            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/products/add", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding product: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    // Sends updated product data to the `/products/update` endpoint.
    public async Task<ApiResponse<object>> UpdateProductAsync(ProductInfo product)
    {
        try
        {
            // The update endpoint only needs the ID and the fields that can be changed.
            var requestData = new
            {
                product_code = product.Id,
                name = product.Name,
                price = product.Price
            };

            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/products/update", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating product: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    // Deletes a product using its ID.
    public async Task<ApiResponse<object>> DeleteProductAsync(string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            return new ApiResponse<object> { Success = false, Message = "Invalid product ID" };
        }

        try
        {
            // For a DELETE request, the ID is usually part of the URL.
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/products/delete/{productId}");
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting product: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    // Downloads the CSV export from the `/products/download` endpoint.
    public async Task<byte[]?> DownloadCsvAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/products/download");
            // If the request was successful, I return the file's raw bytes. Otherwise, null.
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error downloading CSV: {ex.Message}");
            return null;
        }
    }

    // Uploads a CSV file for bulk import.
    public async Task<ApiResponse<object>> UploadCsvAsync(StorageFile file)
    {
        try
        {
            // Sending files requires a specific format called `MultipartFormDataContent`.
            using var content = new MultipartFormDataContent();

            var fileBytes = (await FileIO.ReadBufferAsync(file)).ToArray();
            var fileContent = new ByteArrayContent(fileBytes);
            // It's important to set the correct `Content-Type` so the server knows what it's getting.
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");

            // The API expects the file to be in a form field named "file".
            content.Add(fileContent, "file", file.Name);

            var response = await _httpClient.PostAsync($"{BaseUrl}/products/upload", content);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error uploading CSV: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    // A private helper to avoid repeating the same response-handling logic in every method.
    private async Task<ApiResponse<T>> ProcessResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        try
        {
            if (response.IsSuccessStatusCode)
            {
                // If the call was successful, try to read the standard API response structure.
                return JsonSerializer.Deserialize<ApiResponse<T>>(content)
                    ?? new ApiResponse<T> { Success = true, Message = "Operation successful." };
            }

            // If the call failed, the body might contain a JSON error message from the API.
            return JsonSerializer.Deserialize<ApiResponse<T>>(content)
                ?? new ApiResponse<T> { Success = false, Message = $"API returned an error: {response.ReasonPhrase}" };
        }
        catch (JsonException ex)
        {
            // This catches cases where the server returns something that isn't valid JSON (e.g., an HTML error page).
            Debug.WriteLine($"Error deserializing response: {ex.Message}");
            return new ApiResponse<T> { Success = false, Message = $"Failed to parse server response. Status: {response.StatusCode}" };
        }
    }
}