// Models/MessageRequestInfo.cs

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App_v1.Models;

public class MessageRequestInfo : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _title = string.Empty;
    private string _body = string.Empty;
    private string _requesterId = string.Empty;
    private string _status = string.Empty;
    private string? _reviewerId;
    private string _createdAt = string.Empty;
    private string _updatedAt = string.Empty;

    [JsonPropertyName("request_id")]
    public string Id
    {
        get => _id; set
        {
            _id = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("title")]
    public string Title
    {
        get => _title; set
        {
            _title = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("body")]
    public string Body
    {
        get => _body; set
        {
            _body = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("requester_id")]
    public string RequesterId
    {
        get => _requesterId; set
        {
            _requesterId = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("status")]
    public string Status
    {
        get => _status; set
        {
            _status = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("reviewer_id")]
    public string? ReviewerId
    {
        get => _reviewerId; set
        {
            _reviewerId = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("created_at")]
    public string CreatedAt
    {
        get => _createdAt; set
        {
            _createdAt = value; OnPropertyChanged();
        }
    }

    [JsonPropertyName("updated_at")]
    public string UpdatedAt
    {
        get => _updatedAt; set
        {
            _updatedAt = value; OnPropertyChanged();
        }
    }

    // Helper property for UI binding
    [JsonIgnore]
    public bool IsPending => Status == "Pending";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}