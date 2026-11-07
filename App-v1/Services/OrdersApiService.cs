// This service is the middleman between the app and the Orders API.
// All HTTP requests related to orders (getting, updating, etc.) live here.

using App_v1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace App_v1.Services;

public class OrderApiService
{
    // A single HttpClient instance is reused for all API calls for performance.
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:8000";
    //private const string BaseUrl = "https://capstone-project.azurewebsites.net";

    public OrderApiService()
    {
        _httpClient = new HttpClient();
    }

    // Fetches the specific items (products) for a single order.
    public async Task<ApiResponse<List<OrderItemInfo>>> GetOrderItemsAsync(string orderId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<OrderItemInfo>>>($"{BaseUrl}/orders/view/{orderId}");
            return response ?? new ApiResponse<List<OrderItemInfo>> { Success = false, Message = "No data returned from the server." };
        }
        catch (Exception ex)
        {
            // This usually means the server is down or there's a network issue.
            System.Diagnostics.Debug.WriteLine($"Error getting order items: {ex.Message}");
            return new ApiResponse<List<OrderItemInfo>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }

    // Fetches the complete list of all orders from the server.
    public async Task<ApiResponse<List<OrdersInfo>>> GetOrdersAsync()
    {
        try
        {
            // GetFromJsonAsync handles the GET request and JSON deserialization in one go.
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<OrdersInfo>>>($"{BaseUrl}/orders");
            return response ?? new ApiResponse<List<OrdersInfo>> { Success = false, Message = "No data returned from the server." };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting orders: {ex.Message}");
            return new ApiResponse<List<OrdersInfo>> { Success = false, Message = $"Could not connect to the server: {ex.Message}" };
        }
    }

    // Sends a request to change the status of an existing order.
    public async Task<ApiResponse<object>> UpdateOrderStatusAsync(string orderId, string status)
    {
        try
        {
            // The API expects a JSON object with 'id' and 'status' fields.
            var requestData = new { id = orderId, status };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/orders/status", requestData);

            // Let the helper method handle the response, keeping this clean.
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating order status: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    // Sends a request to cancel a specific order.
    public async Task<ApiResponse<object>> CancelOrderAsync(string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
        {
            return new ApiResponse<object> { Success = false, Message = "Invalid order ID" };
        }

        try
        {
            var requestData = new { id = orderId };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/orders/cancel", requestData);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cancelling order: {ex.Message}");
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    // Downloads a CSV file of all orders. Returns the raw file content as bytes.
    public async Task<byte[]?> DownloadCsvAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/orders/download");
            // If the request was successful, return the file bytes. Otherwise, return null.
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error downloading CSV: {ex.Message}");
            return null;
        }
    }

    // Submits the current cart to create a new order.
    public async Task<ApiResponse<object>> ConfirmOrderAsync(string orderedBy)
    {
        if (!App.Cart.Any())
        {
            return new ApiResponse<object> { Success = false, Message = "Cart is empty." };
        }

        // Build the JSON payload in the exact format the API expects.
        var orderPayload = new
        {
            // Assumption: all items in the cart are from the same supplier.
            supplier_id = App.Cart.First().SupplierId,
            products = App.Cart.Select(item => new
            {
                supplier_id = item.SupplierId,
                product_code = item.ProductCode,
                description = item.Description,
                price = item.Price,
                quantity = item.Quantity,
                subtotal = item.Subtotal
            }).ToList(),
            total_cost = App.Cart.Sum(item => item.Subtotal),
            ordered_by = orderedBy
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/orders/confirm", orderPayload);
            return await ProcessResponse<object>(response);
        }
        catch (Exception ex)
        {
            return new ApiResponse<object> { Success = false, Message = $"An error occurred: {ex.Message}" };
        }
    }

    // A private helper to avoid repeating response-handling logic.
    // It processes both successful responses and API error messages.
    private async Task<ApiResponse<T>> ProcessResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        try
        {
            if (response.IsSuccessStatusCode)
            {
                // If the request was successful, deserialize the API's response.
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content);
                return apiResponse ?? new ApiResponse<T> { Success = true, Message = "Operation successful." };
            }

            // If the API returned an error (like 400 or 500), try to parse its error message.
            var errorResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content);
            return errorResponse ?? new ApiResponse<T> { Success = false, Message = $"API returned an error: {response.ReasonPhrase}" };
        }
        catch (JsonException ex)
        {
            // This happens if the server returns something that isn't valid JSON.
            System.Diagnostics.Debug.WriteLine($"Error deserializing response: {ex.Message}");
            return new ApiResponse<T> { Success = false, Message = $"Failed to parse server response. Status: {response.StatusCode}" };
        }
    }
}