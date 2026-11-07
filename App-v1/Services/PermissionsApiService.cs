// Services/PermissionsApiService.cs
using App_v1.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace App_v1.Services;

/*
 - This service talks to the Permissions API.
 - It has methods for reading permissions and performing actions like adding, granting, and revoking.
 - HTTP calls and JSON handling are wrapped here so the rest of the app doesn't deal with them.
*/
public class PermissionsApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:8000";

    public PermissionsApiService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<ApiResponse<List<PermissionsInfo>>> GetPermissionsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<PermissionsInfo>>>($"{BaseUrl}/permissions");
            return response ?? new ApiResponse<List<PermissionsInfo>> { Success = false, Message = "No data returned from server." };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting permissions: {ex.Message}");
            return new ApiResponse<List<PermissionsInfo>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<object>> AddPermissionsAsync(string section)
    {
        try
        {
            var requestData = new { section_name = section };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/permissions/add", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding permissions: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<object>> GrantPermissionAsync(string permissionId, string roleName)
    {
        try
        {
            var requestData = new { perm_id = permissionId, role_name = roleName };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/permissions/grant", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error granting permission: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<object>> RevokePermissionAsync(string permissionId, string roleName)
    {
        try
        {
            var requestData = new { perm_id = permissionId, role_name = roleName };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/permissions/revoke", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error revoking permission: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    private async Task<ApiResponse<T>> ProcessResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content);
                return apiResponse ?? new ApiResponse<T> { Success = true, Message = "Operation successful." };
            }

            var errorResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content);
            return errorResponse ?? new ApiResponse<T> { Success = false, Message = $"API returned an error: {response.ReasonPhrase}" };
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deserializing response: {ex.Message}");
            return new ApiResponse<T> { Success = false, Message = $"Failed to parse server response. Status: {response.StatusCode}" };
        }
    }
}