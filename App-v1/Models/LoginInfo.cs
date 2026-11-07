// Models/LoginModel.cs

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace App_v1.Models;

public class LoginRequest
{
    [JsonPropertyName("staff_id")]
    public string StaffId { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    [JsonPropertyName("success")]
    public bool Success
    {
        get; set;
    }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    // This property will hold the user data after a successful 2FA verification.
    [JsonPropertyName("data")]
    public UserData? Data
    {
        get; set;
    }
}

public class UserData
{
    [JsonPropertyName("staff_id")]
    public string StaffId { get; set; } = string.Empty;

    [JsonPropertyName("staff_name")]
    public string StaffName { get; set; } = string.Empty;

    [JsonPropertyName("staff_surname")]
    public string StaffSurname { get; set; } = string.Empty;


    [JsonPropertyName("staff_email")]
    public string StaffEmail { get; set; } = string.Empty;

    [JsonPropertyName("staff_phone")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = new();
}