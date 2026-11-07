// Services/BranchApiService.cs

using App_v1.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Linq;

namespace App_v1.Services;

/*
 - This service talks to the Branch API
 - It has methods for CRUD operations (Create, Read, Update, Delete) on branches
 - HTTP calls and JSON handling are wrapped here so the rest of the app doesn't deal with them
 - Includes file operations for CSV download and upload
*/
public class BranchApiService
{
    // private = only this class can use it
    // readonly = it's set once in the constructor and can't be replaced later
    // HttpClient = the object used to send HTTP requests and receive responses
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:8000";
    //private const string BaseUrl = "https://capstone-project.azurewebsites.net";

    public BranchApiService()
    {
        // Initialize the HttpClient once so it can be reused for all requests
        _httpClient = new HttpClient();
    }

    /*
     - GetBranchesAsync retrieves all branches from the server
     - It returns Task<ApiResponse<List<BranchInfo>>>
     - Task means this is asynchronous: it won't block the program while waiting for the server
     - ApiResponse<List<BranchInfo>> is the actual result that comes back once the Task finishes
    */
    public async Task<ApiResponse<List<BranchInfo>>> GetBranchesAsync()
    {
        try
        {
            // GetFromJsonAsync:
            // - Sends GET request to the URL
            // - Reads the raw response body
            // - Deserializes JSON into ApiResponse<List<BranchInfo>> object
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<BranchInfo>>>($"{BaseUrl}/branches");

            // If response is null, return failure with error message
            return response ?? new ApiResponse<List<BranchInfo>> { Success = false, Message = "No data returned from server." };
        }
        catch (Exception ex)
        {
            // If anything unexpected goes wrong, log it and wrap exception message into response
            System.Diagnostics.Debug.WriteLine($"Error getting branches: {ex.Message}");
            return new ApiResponse<List<BranchInfo>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }

    /*
     - AddBranchAsync creates a new branch on the server
     - It takes a BranchInfo object and converts it to the format the API expects
     - Returns Task<ApiResponse<object>> since we don't need specific data back, just success/failure
    */
    public async Task<ApiResponse<object>> AddBranchAsync(BranchInfo newBranch)
    {
        try
        {
            // Create an anonymous object with the exact field names the API expects
            var requestData = new
            {
                manager = newBranch.Manager,
                city = newBranch.City,
                email = newBranch.Email,
                phone = newBranch.Phone,
                address = newBranch.Address
            };

            // Send POST request with JSON body
            // PostAsJsonAsync converts requestData to JSON before sending
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/branches/add", requestData);

            // Use helper method to standardize response processing
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            // If anything unexpected goes wrong, log it and wrap exception message into response
            System.Diagnostics.Debug.WriteLine($"Error adding branch: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    /*
     - UpdateBranchAsync modifies an existing branch's information
     - It includes the branch_code so the API knows which branch to update
    */
    public async Task<ApiResponse<object>> UpdateBranchAsync(BranchInfo branch)
    {
        try
        {
            // Create request data with branch_code to identify which branch to update
            var requestData = new
            {
                branch_code = branch.Id,
                manager = branch.Manager,
                city = branch.City,
                email = branch.Email,
                phone = branch.Phone,
                address = branch.Address
            };

            // Send POST request with JSON body to update endpoint
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/branches/update", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating branch: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    /*
     - DeleteBranchAsync removes a branch from the server
     - It uses HTTP DELETE method and includes the branchId in the URL path
    */
    public async Task<ApiResponse<object>> DeleteBranchAsync(string? branchId)
    {
        // Check if branchId is valid before making the request
        if (string.IsNullOrEmpty(branchId))
        {
            return new ApiResponse<object> { Success = false, Message = "Invalid branch ID" };
        }

        try
        {
            // HTTP DELETE request with branchId in URL path
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/branches/{branchId}");
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting branch: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    /*
     - DownloadCsvAsync gets a CSV file containing all branch data
     - Returns byte array that can be saved as a file, or null if the download fails
    */
    public async Task<byte[]?> DownloadCsvAsync()
    {
        try
        {
            // Send GET request to download endpoint
            var response = await _httpClient.GetAsync($"{BaseUrl}/branches/download");

            // If response status code is in success range (200–299)
            if (response.IsSuccessStatusCode)
            {
                // ReadAsByteArrayAsync gets the raw file content as bytes
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error downloading CSV: {ex.Message}");
            return null;
        }
    }

    /*
     - UploadCsvAsync sends a CSV file to the server for bulk branch operations
     - It uses MultipartFormDataContent to handle file uploads properly
     - StorageFile is UWP's way of representing files
    */
    public async Task<ApiResponse<object>> UploadCsvAsync(StorageFile file)
    {
        try
        {
            // MultipartFormDataContent is used for file uploads
            using var content = new MultipartFormDataContent();

            // Read the file into a byte array
            var fileBytes = (await FileIO.ReadBufferAsync(file)).ToArray();

            // Wrap the bytes in ByteArrayContent and set the content type
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");

            // Add the file content to the multipart form with field name "file"
            content.Add(fileContent, "file", file.Name);

            // Send POST request with file content
            var response = await _httpClient.PostAsync($"{BaseUrl}/branches/upload", content);
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
    */
    private async Task<ApiResponse<T>> ProcessResponse<T>(HttpResponseMessage response)
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