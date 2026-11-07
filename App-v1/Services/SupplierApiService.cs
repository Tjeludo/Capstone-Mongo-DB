using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using App_v1.Models;
using Windows.Storage;

namespace App_v1.Services;

/// <summary>
/// Handles all supplier-related API communication.
/// Centralizes HTTP logic for easier management.
/// </summary>
public class SupplierApiService
{
    /* Single HttpClient instance for efficiency */
    private readonly HttpClient _httpClient;

    /* Base URL for API (switchable between dev and production) */
    private const string BaseUrl = "http://127.0.0.1:8000";
    // private const string BaseUrl = "https://capstone-project.azurewebsites.net";

    /* Constructor initializes HttpClient */
    public SupplierApiService() => _httpClient = new HttpClient();

    /* Fetches the full supplier list from the API */
    public async Task<ApiResponse<List<SupplierInfo>>> GetSuppliersAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<SupplierInfo>>>($"{BaseUrl}/suppliers");
            return response ?? new ApiResponse<List<SupplierInfo>> { Success = false, Message = "No data returned from server." };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API connection error (GetSuppliers): {ex.Message}");
            return new ApiResponse<List<SupplierInfo>> { Success = false, Message = "Could not connect to the server." };
        }
    }

    /* Fetches products for a specific supplier */
    public async Task<ApiResponse<List<SupplierProductInfo>>> GetSupplierItemsAsync(string supplierId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<SupplierProductInfo>>>($"{BaseUrl}/suppliers/{supplierId}/items");
            return response ?? new ApiResponse<List<SupplierProductInfo>> { Success = false, Message = "No data returned from server." };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API connection error (GetSupplierItems): {ex.Message}");
            return new ApiResponse<List<SupplierProductInfo>> { Success = false, Message = "Could not connect to the server." };
        }
    }

    /* Adds a new supplier via POST request */
    public async Task<ApiResponse<object>> AddSupplierAsync(SupplierInfo newSupplier)
    {
        try
        {
            var requestData = new
            {
                name = newSupplier.Name,
                email = newSupplier.Email,
                phone_number = newSupplier.PhoneNumber,
                address = newSupplier.Address,
                sales_rep = newSupplier.SalesRep
            };

            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/suppliers/add", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API connection error (AddSupplier): {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"Unexpected error: {ex.Message}" };
        }
    }

    /* Updates an existing supplier via POST request */
    public async Task<ApiResponse<object>> UpdateSupplierAsync(SupplierInfo supplier)
    {
        try
        {
            var requestData = new
            {
                id = supplier.ID,
                name = supplier.Name,
                email = supplier.Email,
                phone_number = supplier.PhoneNumber,
                address = supplier.Address,
                sales_rep = supplier.SalesRep
            };

            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/suppliers/update", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API connection error (UpdateSupplier): {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"Unexpected error: {ex.Message}" };
        }
    }

    /* Deletes a supplier by ID */
    public async Task<ApiResponse<object>> DeleteSupplierAsync(string? supplierId)
    {
        if (string.IsNullOrEmpty(supplierId))
            return new ApiResponse<object> { Success = false, Message = "Supplier ID not provided." };

        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/suppliers/{supplierId}");
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API connection error (DeleteSupplier): {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"Unexpected error: {ex.Message}" };
        }
    }

    /* Downloads supplier CSV as byte array */
    public async Task<byte[]?> DownloadCsvAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/suppliers/download");
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API connection error (DownloadCsv): {ex.Message}");
            return null;
        }
    }

    /* Uploads a CSV file for bulk supplier import */
    public async Task<ApiResponse<object>> UploadCsvAsync(StorageFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileBytes = (await FileIO.ReadBufferAsync(file)).ToArray();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
            content.Add(fileContent, "file", file.Name);

            var response = await _httpClient.PostAsync($"{BaseUrl}/suppliers/upload", content);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API connection error (UploadCsv): {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"Unexpected error: {ex.Message}" };
        }
    }

    /* Helper to standardize API response handling */
    private async Task<ApiResponse<T>> ProcessResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content);
            if (apiResponse != null)
            {
                apiResponse.Success = response.IsSuccessStatusCode && apiResponse.Success;
                return apiResponse;
            }

            return new ApiResponse<T>
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Operation successful." : $"API error: {response.ReasonPhrase}"
            };
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"JSON parse error: {ex.Message}. Content: {content}");
            return new ApiResponse<T> { Success = false, Message = $"Failed to parse server response. Status: {response.StatusCode}" };
        }
    }
}
