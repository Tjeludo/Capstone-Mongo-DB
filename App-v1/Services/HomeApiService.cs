// Services/HomeApiService.cs

using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace App_v1.Services;

// Matches the "data" object in your API response
public class DashboardData
{
    [JsonPropertyName("total_expenses")]
    public double TotalExpenses
    {
        get; set;
    }

    [JsonPropertyName("deliveries_count")]
    public int DeliveriesCount
    {
        get; set;
    }

    [JsonPropertyName("pending_orders")]
    public int PendingOrdersCount
    {
        get; set;
    }

    [JsonPropertyName("confirmed_orders")]
    public int ConfirmedOrdersCount
    {
        get; set;
    }

    [JsonPropertyName("transfers_count")]
    public int TransfersCount
    {
        get; set;
    }

    [JsonPropertyName("pending_requests_count")]
    public int NewRequestsCount
    {
        get; set;
    }
}

// Matches the top-level structure of your API response
public class ApiDashboardResponse
{
    [JsonPropertyName("success")]
    public bool Success
    {
        get; set;
    }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public DashboardData? Data
    {
        get; set;
    }
}


/// <summary>
/// Service to handle HTTP requests to the backend API for the Home Page.
/// </summary>
public class HomeApiService
{
    private readonly HttpClient _httpClient;

    // The base URL for your local FastAPI backend.
    private const string BaseApiUrl = "http://127.0.0.1:8000";

    public HomeApiService()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseApiUrl) };
    }

    /// <summary>
    /// Fetches all dashboard data concurrently from the /home/dashboard endpoint.
    /// </summary>
    public async Task<DashboardData?> GetDashboardDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/home/dashboard");
            response.EnsureSuccessStatusCode(); // Throws on non-2xx status codes.
            var json = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiDashboardResponse>(json);

            if (apiResponse is { Success: true, Data: not null })
            {
                return apiResponse.Data;
            }
            else
            {
                // Log the error message from the API if it failed but returned a 2xx code
                Console.WriteLine($"API returned an error: {apiResponse?.Message}");
                return null;
            }
        }
        catch (HttpRequestException e)
        {
            // Log or handle network errors
            Console.WriteLine($"API request error calling /home/dashboard: {e.Message}");
            return null;
        }
        catch (JsonException e)
        {
            // Log or handle JSON parsing errors
            Console.WriteLine($"JSON parsing error from /home/dashboard: {e.Message}");
            return null;
        }
    }
}