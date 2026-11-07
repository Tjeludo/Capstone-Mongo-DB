// Services/RoleApiService.cs

using App_v1.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace App_v1.Services;

public class RoleApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:8000";

    public RoleApiService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<ApiResponse<List<RolesInfo>>> GetRolesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<RolesInfo>>>($"{BaseUrl}/roles");
            return response ?? new ApiResponse<List<RolesInfo>> { Success = false, Message = "No data returned from server." };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting roles: {ex.Message}");
            return new ApiResponse<List<RolesInfo>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<object>> AddRoleAsync(RolesInfo newRole)
    {
        try
        {
            // UPDATED: Removed description from request data
            var requestData = new
            {
                name = newRole.RoleName,
                abbrev = newRole.RoleAbbreviation,
                status = newRole.Status
            };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/roles/add", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding role: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    public async Task<ApiResponse<object>> UpdateRoleAsync(RolesInfo role)
    {
        try
        {
            // UPDATED: Removed description from request data
            var requestData = new
            {
                role_code = role.RoleCode,
                name = role.RoleName,
                abbrev = role.RoleAbbreviation,
                status = role.Status
            };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/roles/update", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating role: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    // --- (DeleteRoleAsync, DownloadCsvAsync, UploadCsvAsync, ProcessResponse are unchanged) ---
    public async Task<ApiResponse<object>> DeleteRoleAsync(int roleCode)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/roles/{roleCode}");
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting role: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    // --- (NEW method to get permissions for a role) ---
    public async Task<ApiResponse<List<string>>> GetRolePermissionsAsync(int roleCode)
    {
        try
        {
            var requestData = new { role_code = roleCode };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/roles/permissions", requestData);
            return await ProcessResponse<List<string>>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting role permissions: {ex.Message}");
            return new ApiResponse<List<string>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }

    public async Task<byte[]?> DownloadCsvAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/roles/download");
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error downloading CSV: {ex.Message}");
            return null;
        }
    }
    public async Task<ApiResponse<object>> UploadCsvAsync(StorageFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileBytes = System.IO.File.ReadAllBytes(file.Path);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
            content.Add(fileContent, "file", file.Name);
            var response = await _httpClient.PostAsync($"{BaseUrl}/roles/upload", content);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error uploading CSV: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }
    private static async Task<ApiResponse<T>> ProcessResponse<T>(HttpResponseMessage response)
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