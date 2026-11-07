// Services/UserApiService.cs

using App_v1.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace App_v1.Services;

/*
 - This service talks to the Users API
 - It has methods for managing user information and status updates
 - HTTP calls and JSON handling are wrapped here so the rest of the app doesn't deal with them
 - Focused on user management operations like fetching users, updating their status, and changing passwords.
*/
public class UserApiService
{
    // private = only this class can use it
    // readonly = it's set once in the constructor and can't be replaced later
    // HttpClient = the object used to send HTTP requests and receive responses
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:8000";
    //private const string BaseUrl = "https://capstone-project.azurewebsites.net";

    public UserApiService()
    {
        // Initialize the HttpClient once so it can be reused for all requests
        _httpClient = new HttpClient();
    }

    /*
     - GetUsersAsync retrieves all users from the server
     - It returns Task<ApiResponse<List<UserInfo>>>
     - Task means this is asynchronous: it won't block the program while waiting for the server
     - ApiResponse<List<UserInfo>> is the actual result that comes back once the Task finishes
    */
    public async Task<ApiResponse<List<UserInfo>>> GetUsersAsync()
    {
        try
        {
            // GetFromJsonAsync:
            // - Sends GET request to the URL
            // - Reads the raw response body
            // - Deserializes JSON into ApiResponse<List<UserInfo>> object
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<UserInfo>>>($"{BaseUrl}/users");

            // If response is null, return failure with error message
            return response ?? new ApiResponse<List<UserInfo>> { Success = false, Message = "No data returned from server." };
        }
        catch (Exception ex)
        {
            // If anything unexpected goes wrong, log it and wrap exception message into response
            System.Diagnostics.Debug.WriteLine($"Error getting users: {ex.Message}");
            return new ApiResponse<List<UserInfo>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }

    /*
     - UpdateUserStatusAsync changes the status of a specific user
     - It takes staffId to identify the user and newStatus for the new status value
     - Returns Task<ApiResponse<object>> since we don't need specific data back, just success/failure
    */
    public async Task<ApiResponse<object>> UpdateUserStatusAsync(string staffId, string newStatus)
    {
        try
        {
            // Create an anonymous object with the exact field names the API expects
            var requestData = new { staff_id = staffId, status = newStatus };

            // Send POST request with JSON body
            // PostAsJsonAsync converts requestData to JSON before sending
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/users/status", requestData);

            // Use helper method to standardize response processing
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            // If anything unexpected goes wrong, log it and wrap exception message into response
            System.Diagnostics.Debug.WriteLine($"Error updating status: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    // --- ADDED METHOD TO SUPPORT THE CHANGE PASSWORD FEATURE ---
    /*
     - ChangePasswordAsync sends a request to the server to update a user's password.
     - It requires the staff ID, the old password for verification, and the new password.
    */
    public async Task<ApiResponse<object>> ChangePasswordAsync(string staffId, string oldPassword, string newPassword)
    {
        try
        {
            // Create an anonymous object that matches the structure the Python API expects.
            var requestData = new
            {
                staff_id = staffId,
                old_password = oldPassword,
                new_password = newPassword
            };

            // Send an HTTP POST request to the "/users/change-password" endpoint.
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/users/change-password", requestData);

            // Use our helper method to process the HTTP response.
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error changing password: {ex.Message}");
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