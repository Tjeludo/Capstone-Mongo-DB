// Services/SupplierProductApiService.cs

using App_v1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace App_v1.Services;

/*
 - This service talks to the Supplier Products API
 - It has methods for managing supplier-product relationships (Read, Update, Delete)
 - HTTP calls and JSON handling are wrapped here so the rest of the app doesn't deal with them
 - Includes file operations for CSV download and upload
*/
public class SupplierProductApiService
{
    // private = only this class can use it
    // readonly = it's set once in the constructor and can't be replaced later
    // HttpClient = the object used to send HTTP requests and receive responses
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:8000";
    //private const string BaseUrl = "https://capstone-project.azurewebsites.net";

    public SupplierProductApiService()
    {
        // Initialize the HttpClient once so it can be reused for all requests
        _httpClient = new HttpClient();
    }

    /*
     - GetSupplierProductsAsync retrieves all products for a *specific* supplier from the server
     - It returns Task<ApiResponse<List<SupplierProductInfo>>>
     - Task means this is asynchronous: it won't block the program while waiting for the server
     - ApiResponse<List<SupplierProductInfo>> is the actual result that comes back once the Task finishes
    */
    public async Task<ApiResponse<List<SupplierProductInfo>>> GetSupplierProductsAsync(string supplierId)
    {
        if (string.IsNullOrEmpty(supplierId))
        {
            return new ApiResponse<List<SupplierProductInfo>> { Success = true, Data = new List<SupplierProductInfo>(), Message = "No supplier selected." };
        }

        try
        {
            // Call the new endpoint with the supplierId in the URL path
            var url = $"{BaseUrl}/supplier/products/{supplierId}";
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<SupplierProductInfo>>>(url);

            // If response is null, return failure with error message
            return response ?? new ApiResponse<List<SupplierProductInfo>> { Success = false, Message = "No data returned from server." };
        }
        catch (Exception ex)
        {
            // If anything unexpected goes wrong, log it and wrap exception message into response
            System.Diagnostics.Debug.WriteLine($"Error getting supplier products: {ex.Message}");
            return new ApiResponse<List<SupplierProductInfo>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }

    /*
     - UpdateSupplierProductAsync modifies an existing supplier product relationship
     - It takes a SupplierProductInfo object and converts it to the format the API expects
     - Returns Task<ApiResponse<object>> since we don't need specific data back, just success/failure
    */
    public async Task<ApiResponse<object>> UpdateSupplierProductAsync(SupplierProductInfo product)
    {
        try
        {
            // CHANGE: Create an anonymous object with only the fields the API expects for a price update.
            var requestData = new
            {
                supplier_id = product.SupplierId,
                product_code = product.ProductCode,
                price = product.Price
            };

            // Send POST request with JSON body
            // PostAsJsonAsync converts requestData to JSON before sending
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/supplier/products/update", requestData);

            // Use helper method to standardize response processing
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            // If anything unexpected goes wrong, log it and wrap exception message into response
            System.Diagnostics.Debug.WriteLine($"Error updating product: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    /*
     - DeleteSupplierProductAsync removes a supplier-product relationship from the server
     - It uses HTTP DELETE method and includes both supplierId and productCode in the URL path
     - Both parameters are required to uniquely identify the relationship to delete
    */
    public async Task<ApiResponse<object>> DeleteSupplierProductAsync(string? supplierId, string? productCode)
    {
        // Check if both supplierId and productCode are valid before making the request
        if (string.IsNullOrEmpty(supplierId) || string.IsNullOrEmpty(productCode))
        {
            return new ApiResponse<object> { Success = false, Message = "Invalid supplier ID or product code" };
        }

        try
        {
            // HTTP DELETE request with both supplierId and productCode in URL path
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/supplier/products/{supplierId}/{productCode}");
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting product: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    /*
     - DownloadCsvAsync gets a CSV file containing all supplier product data
     - Returns byte array that can be saved as a file, or null if the download fails
    */
    public async Task<byte[]?> DownloadCsvAsync(string supplierId)
    {
        if (string.IsNullOrEmpty(supplierId)) return null;
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/supplier/products/download/{supplierId}");
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error downloading CSV: {ex.Message}");
            return null;
        }
    }

    // UPDATED to accept a supplierId
    public async Task<ApiResponse<object>> UploadCsvAsync(StorageFile file, string supplierId)
    {
        if (string.IsNullOrEmpty(supplierId))
        {
            return new ApiResponse<object> { Success = false, Message = "Supplier ID is missing." };
        }
        try
        {
            using var content = new MultipartFormDataContent();
            var fileBytes = (await FileIO.ReadBufferAsync(file)).ToArray();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
            content.Add(fileContent, "file", file.Name);

            var response = await _httpClient.PostAsync($"{BaseUrl}/supplier/products/upload-prices/{supplierId}", content);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error uploading CSV: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    /*
     - ProcessResponse is a private helper method to avoid repeating the same logic
     - It handles both successful and error responses from the API
     - Generic method <T> means it works with any type of response data
     - Static method since it doesn't need access to instance members
    */
    private static async Task<ApiResponse<T>> ProcessResponse<T>(HttpResponseMessage response)
    {
        // Read the response body as a string first
        var content = await response.Content.ReadAsStringAsync();

        try
        {
            // If response status code is in success range (200–299)
            if (response.IsSuccessStatusCode)
            {
                // Try to deserialize the JSON response
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content);

                // Return the deserialized response, or a default success response if null
                return apiResponse ?? new ApiResponse<T> { Success = true, Message = "Operation successful." };
            }

            // If not successful, try to deserialize any error message from the API
            var errorResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content);
            return errorResponse ?? new ApiResponse<T> { Success = false, Message = $"API returned an error: {response.ReasonPhrase}" };
        }
        catch (JsonException ex)
        {
            // If JSON deserialization fails, return a generic error message
            System.Diagnostics.Debug.WriteLine($"Error deserializing response: {ex.Message}");
            return new ApiResponse<T> { Success = false, Message = $"Failed to parse server response. Status: {response.StatusCode}" };
        }
    }
}