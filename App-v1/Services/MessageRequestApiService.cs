// Services/MessageRequestApiService.cs

using App_v1.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace App_v1.Services;

public class MessageRequestApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:8000";

    public MessageRequestApiService()
    {
        _httpClient = new HttpClient();
        // In a real app, you would add the auth token here
        // _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "YOUR_TOKEN");
    }

    // For Managers: Create a new request
    public async Task<ApiResponse<object>> CreateRequestAsync(string title, string body, string requesterId)
    {
        try
        {
            var requestData = new { title, body, requester_id = requesterId };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/requests", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Success = false, Message = ex.Message };
        }
    }

    // For Managers: Get their own requests
    public async Task<ApiResponse<List<MessageRequestInfo>>> GetMyRequestsAsync(string staffId)
    {
        try
        {
            var requestData = new { staff_id = staffId };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/requests/my-requests", requestData);
            return await ProcessResponse<List<MessageRequestInfo>>(response);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<MessageRequestInfo>> { Success = false, Message = ex.Message };
        }
    }

    // For Admins: Get all requests
    public async Task<ApiResponse<List<MessageRequestInfo>>> GetAllRequestsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ApiResponse<List<MessageRequestInfo>>>($"{BaseUrl}/requests");
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<MessageRequestInfo>> { Success = false, Message = ex.Message };
        }
    }

    // For Admins: Approve a request
    public async Task<ApiResponse<object>> ApproveRequestAsync(string requestId, string adminId)
    {
        try
        {
            var requestData = new { staff_id = adminId };
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/requests/{requestId}/approve", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Success = false, Message = ex.Message };
        }
    }

    // For Admins: Decline a request
    public async Task<ApiResponse<object>> DeclineRequestAsync(string requestId, string adminId)
    {
        try
        {
            var requestData = new { staff_id = adminId };
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/requests/{requestId}/decline", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Success = false, Message = ex.Message };
        }
    }

    private async Task<ApiResponse<T>> ProcessResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ApiResponse<T>>(content) ?? new ApiResponse<T> { Success = true };
            }
            return JsonSerializer.Deserialize<ApiResponse<T>>(content) ?? new ApiResponse<T> { Success = false, Message = "An unknown error occurred." };
        }
        catch (JsonException)
        {
            return new ApiResponse<T> { Success = false, Message = "Failed to parse server response." };
        }
    }
}