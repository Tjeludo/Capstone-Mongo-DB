using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App_v1.Models;

public class UserInfo : INotifyPropertyChanged
{
    private string staff_id = string.Empty;
    private string staff_email = string.Empty;
    private string password = string.Empty;
    private string status = string.Empty;
    private string created_at = string.Empty;

    [JsonPropertyName("staff_id")]
    public string StaffID
    {
        get => staff_id;
        set { staff_id = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("staff_email")]
    public string Email
    {
        get => staff_email;
        set { staff_email = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("password")]
    public string Password
    {
        get => password;
        set { password = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("status")]
    public string Status
    {
        get => status;
        set { status = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("created_at")]
    public string CreatedAt
    {
        get => created_at;
        set { created_at = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
