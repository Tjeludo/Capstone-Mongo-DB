// Services/AuthApiService.cs

using App_v1.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace App_v1.Services;

/*
 - This service talks to the Authentication API
 - It has methods for login and verifying two-factor authentication (2FA)
 - HTTP calls and JSON handling are wrapped here so the rest of the app doesn’t deal with them
*/
public class AuthApiService
{
    // private = only this class can use it
    // readonly = it’s set once in the constructor and can’t be replaced later
    // HttpClient = the object used to send HTTP requests and receive responses
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:8000";

    public AuthApiService()
    {
        // Initialize the HttpClient once so it can be reused for all requests
        _httpClient = new HttpClient();
    }

    /*
     - LoginAsync tries to log in a staff member
     - It returns Task<ApiResponse<LoginResponse>>
     - Task means this is asynchronous: it won’t block the program while waiting for the server
     - ApiResponse<LoginResponse> is the actual result that comes back once the Task finishes
    */
    public async Task<ApiResponse<LoginResponse>> LoginAsync(string staffId, string password)
    {
        try
        {
            // Create the login request object with staffId and password
            var loginRequest = new LoginRequest { StaffId = staffId, Password = password };

            // Send POST request with JSON body
            // PostAsJsonAsync converts loginRequest to JSON before sending
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/login", loginRequest);

            // If response status code is in success range (200–299)
            if (response.IsSuccessStatusCode)
            {
                // ReadFromJsonAsync<LoginResponse>:
                // - Reads the raw response body
                // - Deserializes JSON into a LoginResponse object
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                // Create and return a wrapper object marking the operation as successful
                // Success = true means the call worked, Data = loginResponse holds the actual response from the API
                return new ApiResponse<LoginResponse> { Success = true, Data = loginResponse };
            }

            // If not success, still try to read any error details in the body
            var errorResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

            // Return failure with error message if available, otherwise fallback text
            return new ApiResponse<LoginResponse> { Success = false, Message = errorResponse?.Message ?? "An unknown error occurred." };
        }
        catch (Exception ex)
        {
            // If anything unexpected goes wrong, wrap exception message into response
            return new ApiResponse<LoginResponse> { Success = false, Message = ex.Message };
        }
    }

    /*
     - Verify2faAsync checks a one-time password (OTP) code for 2FA
     - It also returns Task<ApiResponse<LoginResponse>> since the check is asynchronous
     - The payload is serialized into JSON manually before sending
    */
    public async Task<ApiResponse<LoginResponse>> Verify2faAsync(string staffId, string otpCode)
    {
        try
        {
            // Build an anonymous object with staffId and otpCode
            var payload = new { staff_id = staffId, otp_code = otpCode };

            // Convert the payload to JSON text
            var jsonPayload = JsonSerializer.Serialize(payload);

            // Wrap JSON into StringContent so it can be sent in the request body
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // POST to the verify-2fa endpoint with the content
            var response = await _httpClient.PostAsync($"{BaseUrl}/verify-2fa", content);

            // Read response body and convert JSON into LoginResponse object
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            // If API says success and result object is valid, return success response
            // Success = true means the verification worked, Data = result carries the API’s data
            return new ApiResponse<LoginResponse> { Success = true, Data = result };

            // Otherwise return failure with error message (or default text if none provided)
            return new ApiResponse<LoginResponse> { Success = false, Message = result?.Message ?? "Invalid 2FA code." };
        }
        catch (Exception ex)
        {
            // Catch unexpected errors and return them as failure response
            return new ApiResponse<LoginResponse> { Success = false, Message = ex.Message };
        }
    }

    /*
     - GetUpdatedPermissionsAsync fetches the latest permissions list for the current user
     - This is called after a grant/revoke action to ensure the session is up-to-date
    */
    public async Task<ApiResponse<List<string>>> GetUpdatedPermissionsAsync(string staffId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<string>>>($"{BaseUrl}/permissions/{staffId}");
            return response ?? new ApiResponse<List<string>> { Success = false, Message = "No data returned from server." };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting updated permissions: {ex.Message}");
            return new ApiResponse<List<string>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }
}