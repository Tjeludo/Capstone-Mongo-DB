using App_v1.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace App_v1.Services;

/// <summary>
/// This service is my single point of contact for the backend Staff API.
/// It keeps all the messy HTTP and JSON stuff in one place, so the rest of my app stays clean.
/// </summary>
public class StaffApiService
{
    // I use one HttpClient for the whole app. It's more efficient.
    private readonly HttpClient _httpClient;
    // My API's base URL. Easy to change here if I deploy it somewhere else.
    private const string BaseUrl = "http://127.0.0.1:8000";
    //private const string BaseUrl = "https://capstone-project.azurewebsites.net";

    public StaffApiService()
    {
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Gets the full list of staff from the API.
    /// </summary>
    public async Task<ApiResponse<List<StaffInfo>>> GetStaffAsync()
    {
        try
        {
            // This one line sends the GET request and converts the JSON response into my C# objects.
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<StaffInfo>>>($"{BaseUrl}/staff");
            return response ?? new ApiResponse<List<StaffInfo>> { Success = false, Message = "No data returned from server." };
        }
        catch (Exception ex)
        {
            // If the server is down or there's a network issue, this will catch it.
            Debug.WriteLine($"API Error (GetStaff): {ex.Message}");
            return new ApiResponse<List<StaffInfo>> { Success = false, Message = $"Could not connect to the server. Please check the connection." };
        }
    }

    /// <summary>
    /// Sends a new staff member's data to the API to be created.
    /// </summary>
    public async Task<ApiResponse<object>> AddStaffAsync(StaffInfo newStaff)
    {
        try
        {
            // PostAsJsonAsync handles converting my StaffInfo object to a JSON string.
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/staff/add", newStaff);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API Error (AddStaff): {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
        }
    }

    /// <summary>
    /// Sends updated details for a staff member to the API.
    /// </summary>
    public async Task<ApiResponse<object>> UpdateStaffAsync(StaffInfo staff)
    {
        try
        {
            // The API expects specific field names, so I create an anonymous object that matches it.
            var requestData = new
            {
                staff_id = staff.Id,
                firstname = staff.FirstName,
                lastname = staff.LastName,
                gender = staff.Gender,
                phone_number = staff.PhoneNumber,
                address = staff.Address,
                status = staff.Status
            };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/staff/update", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API Error (UpdateStaff): {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
        }
    }

    /// <summary>
    /// Removes a staff member from the server using their ID.
    /// </summary>
    public async Task<ApiResponse<object>> DeleteStaffAsync(string staffId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/staff/{staffId}");
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API Error (DeleteStaff): {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
        }
    }

    /// <summary>
    /// Moves a staff member to a different branch.
    /// </summary>
    public async Task<ApiResponse<object>> TransferStaffAsync(string staffId, string newBranchCode)
    {
        try
        {
            var requestData = new { id = staffId, branch_code = newBranchCode };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/staff/transfer", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API Error (TransferStaff): {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
        }
    }

    /// <summary>
    /// Changes a staff member's role.
    /// </summary>
    public async Task<ApiResponse<object>> PromoteStaffAsync(string staffId, string newRole)
    {
        try
        {
            var requestData = new { id = staffId, role = newRole };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/staff/promote", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API Error (PromoteStaff): {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
        }
    }

    /// <summary>
    /// Gets a CSV file of all staff data from the server.
    /// </summary>
    public async Task<byte[]?> DownloadCsvAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/staff/download");
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API Error (DownloadCsv): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sends a CSV file to the server for bulk staff import.
    /// </summary>
    public async Task<ApiResponse<object>> UploadCsvAsync(StorageFile file)
    {
        try
        {
            // MultipartFormDataContent is the standard way to send files in an HTTP request.
            using var content = new MultipartFormDataContent();
            var fileBytes = (await FileIO.ReadBufferAsync(file)).ToArray();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");

            // "file" is the field name the FastAPI server expects for the upload.
            content.Add(fileContent, "file", file.Name);

            var response = await _httpClient.PostAsync($"{BaseUrl}/staff/upload", content);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API Error (UploadCsv): {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An unexpected error occurred: {ex.Message}" };
        }
    }

    /// <summary>
    /// A private helper to handle the response from the API. It saves me from writing the same try-catch block everywhere.
    /// </summary>
    private async Task<ApiResponse<T>> ProcessResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            // Even an error response from my API might have a useful JSON message in the body.
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content);

            // If deserialization results in null, create a fallback response.
            if (apiResponse == null)
            {
                return new ApiResponse<T>
                {
                    Success = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "Operation successful." : $"API returned an error: {response.ReasonPhrase}"
                };
            }

            return apiResponse;
        }
        catch (JsonException ex)
        {
            // If the response isn't valid JSON, this will catch it.
            Debug.WriteLine($"JSON Deserialization Error: {ex.Message}. Response content: {content}");
            return new ApiResponse<T> { Success = false, Message = $"Failed to parse server response. Status: {response.StatusCode}" };
        }
    }
}
