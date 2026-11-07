using App_v1.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;

namespace App_v1.Services;

// This service handles all HTTP communication with the backend Category API.
public class CategoryApiService
{
    // A single, reusable HttpClient is more efficient.
    private readonly HttpClient _httpClient;
    // The root URL for the API. Easy to swap between local and production.
    private const string BaseUrl = "http://127.0.0.1:8000";
    //private const string BaseUrl = "https://capstone-project.azurewebsites.net";

    public CategoryApiService()
    {
        _httpClient = new HttpClient();
    }

    // Fetches all categories from the API.
    public async Task<ApiResponse<List<CategoryInfo>>> GetCategoriesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<CategoryInfo>>>($"{BaseUrl}/categories");
            return response ?? new ApiResponse<List<CategoryInfo>> { Success = false, Message = "No data returned from server." };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting categories: {ex.Message}");
            return new ApiResponse<List<CategoryInfo>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }

    // Posts a new category to the API for creation.
    public async Task<ApiResponse<object>> AddCategoryAsync(CategoryInfo newCategory)
    {
        try
        {
            // The API expects a JSON object with 'name' and 'description'.
            var requestData = new { name = newCategory.Name, description = newCategory.Description };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/categories/add", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding category: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    // Sends an updated category to the API to be saved.
    public async Task<ApiResponse<object>> UpdateCategoryAsync(CategoryInfo category)
    {
        try
        {
            // Include the ID so the API knows which category to update.
            var requestData = new { id = category.Id, name = category.Name, description = category.Description };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/categories/update", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating category: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    // Downloads the category list as a CSV file from the API.
    public async Task<byte[]?> DownloadCsvAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/categories/download");
            // Return the file bytes on success, otherwise null.
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error downloading CSV: {ex.Message}");
            return null;
        }
    }

    // Uploads a CSV file to the API for bulk import.
    public async Task<ApiResponse<object>> UploadCsvAsync(StorageFile file)
    {
        try
        {
            // MultipartFormDataContent is the standard way to send files.
            using var content = new MultipartFormDataContent();
            var fileBytes = (await FileIO.ReadBufferAsync(file)).ToArray();
            var fileContent = new ByteArrayContent(fileBytes);
            // The server needs to know it's receiving a CSV file.
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
            // "file" is the field name the API expects.
            content.Add(fileContent, "file", file.Name);

            var response = await _httpClient.PostAsync($"{BaseUrl}/categories/upload", content);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error uploading CSV: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    // A reusable helper to process all API responses.
    private async Task<ApiResponse<T>> ProcessResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            // Attempt to deserialize both success and error responses into our standard format.
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content);

            if (response.IsSuccessStatusCode)
            {
                return apiResponse ?? new ApiResponse<T> { Success = true, Message = "Operation successful." };
            }
            return apiResponse ?? new ApiResponse<T> { Success = false, Message = $"API returned an error: {response.ReasonPhrase}" };
        }
        catch (JsonException ex)
        {
            // This catches cases where the server returns something that isn't valid JSON.
            Debug.WriteLine($"Error deserializing response: {ex.Message}");
            return new ApiResponse<T> { Success = false, Message = $"Failed to parse server response. Status: {response.StatusCode}" };
        }
    }
}