using App_v1.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace App_v1.Services;

/// <summary>
/// This service is responsible for communicating with the scheduler-related endpoints of the API.
/// </summary>
public class ScheduleApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:8000"; 

    public ScheduleApiService()
    {
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Fetches a list of all orders with an expected delivery date in the future.
    /// </summary>
    /// <returns>An ApiResponse containing a list of orders for the scheduler.</returns>
    public async Task<ApiResponse<List<OrdersInfo>>> GetUpcomingDeliveriesAsync()
    {
        try
        {
            // Makes a GET request to the new endpoint you created in order_api.py
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<OrdersInfo>>>($"{BaseUrl}/orders/deliveries/upcoming");
            return response ?? new ApiResponse<List<OrdersInfo>> { Success = false, Message = "No data returned from the server." };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting upcoming deliveries: {ex.Message}");
            return new ApiResponse<List<OrdersInfo>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }
}
