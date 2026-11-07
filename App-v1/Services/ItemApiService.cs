// Services/ItemApiService.cs

using App_v1.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace App_v1.Services;

/*
 - This service talks to the Items API
 - It has methods for fetching general item information
 - HTTP calls and JSON handling are wrapped here so the rest of the app doesn't deal with them
 - Provides a clean, reusable way to get item data from the server
*/
public class ItemApiService
{
    // private = only this class can use it
    // readonly = it's set once in the constructor and can't be replaced later
    // HttpClient = the object used to send HTTP requests and receive responses
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:8000";
    //private const string BaseUrl = "https://capstone-project.azurewebsites.net";

    public ItemApiService()
    {
        // Initialize the HttpClient once so it can be reused for all requests
        _httpClient = new HttpClient();
    }

    /*
     - GetItemsAsync retrieves all general items from the server
     - It returns Task<ApiResponse<List<ItemInfo>>>
     - Task means this is asynchronous: it won't block the program while waiting for the server
     - ApiResponse<List<ItemInfo>> is the actual result that comes back once the Task finishes
    */
    public async Task<ApiResponse<List<ItemInfo>>> GetItemsAsync()
    {
        try
        {
            // GetFromJsonAsync:
            // - Sends GET request to the URL
            // - Reads the raw response body
            // - Deserializes JSON directly into ApiResponse<List<ItemInfo>> object
            // This eliminates the need for a separate 'ItemResponse' class
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ItemInfo>>>($"{BaseUrl}/supplier/products");

            // If response is null, return failure with error message
            // We can now directly return the response from the API call or a default error message
            return response ?? new ApiResponse<List<ItemInfo>> { Success = false, Message = "No data returned from server." };
        }
        catch (Exception ex)
        {
            // If anything unexpected goes wrong, log it and wrap exception message into response
            System.Diagnostics.Debug.WriteLine($"Error getting items: {ex.Message}");
            return new ApiResponse<List<ItemInfo>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }
}