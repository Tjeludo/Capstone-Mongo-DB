/*
I created this generic class to handle all API responses consistently.
Using a generic type 'T' means I can wrap any data type from the API,
like a list of branches or just a simple success message, without needing
to create separate response classes for everything.
*/
using System.Text.Json.Serialization;

namespace App_v1.Models;

public class ApiResponse<T>
{
    /*
    A simple boolean to tell me if the API call worked.
    */
    [JsonPropertyName("success")]
    public bool Success
    {
        get; set;
    }

    /*
    This holds any message from the API. It's useful for showing
    feedback to the user, like "Branch added successfully".
    */
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /*
    This is the actual data payload from the API. I made it nullable ('T?')
    because sometimes an API call might succeed but not return any data.
    */
    [JsonPropertyName("data")]
    public T? Data
    {
        get; set;
    }
}