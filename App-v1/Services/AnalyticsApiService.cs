// Services/AnalyticsApiService.cs

using App_v1.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace App_v1.Services;

public class AnalyticsApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:8000";

    public AnalyticsApiService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<ApiResponse<AnalyticsDashboardData>> GetAnalyticsAsync(int days)
    {
        try
        {
            // The 'days' parameter is now added as a query string to the URL
            var url = $"{BaseUrl}/analytics/dashboard?days={days}";
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<AnalyticsDashboardData>>(url);
            return response ?? new ApiResponse<AnalyticsDashboardData> { Success = false, Message = "No data returned from server." };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting analytics: {ex.Message}");
            return new ApiResponse<AnalyticsDashboardData> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }
}