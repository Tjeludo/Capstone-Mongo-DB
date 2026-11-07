/*
This is the main data model for a single permission.
I implemented INotifyPropertyChanged so that the UI automatically
updates whenever a property like 'Name' or 'Status' changes. This is
essential for data binding to work smoothly.
*/
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace App_v1.Models;

public class PermissionsInfo : INotifyPropertyChanged
{
    private string _permId = string.Empty;
    private string _permName = string.Empty;
    // REMOVED: private string _permDescription = string.Empty;
    private string _permStatus = string.Empty;
    private string _creationDate = string.Empty;

    /*
    Using JsonPropertyName to map the API's snake_case
    fields to my PascalCase properties.
    */
    [JsonPropertyName("perm_id")]
    public string Id
    {
        get => _permId;
        set
        {
            /*
            I check if the value has actually changed before raising
            the PropertyChanged event to avoid unnecessary UI updates.
            The '?? string.Empty' ensures I never assign a null value.
            */
            if (_permId != value)
            {
                _permId = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("perm_name")]
    public string Name
    {
        get => _permName;
        set
        {
            if (_permName != value)
            {
                _permName = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    // REMOVED: The Description property has been fully removed.
    // [JsonPropertyName("perm_description")]
    // public string Description ...

    [JsonPropertyName("perm_status")]
    public string Status
    {
        get => _permStatus;
        set
        {
            if (_permStatus != value)
            {
                _permStatus = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    [JsonPropertyName("creation_date")]
    public string CreationDate
    {
        get => _creationDate;
        set
        {
            if (_creationDate != value)
            {
                _creationDate = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /*
    I'm using [CallerMemberName] here. It's a handy attribute that
    automatically passes the name of the calling property (e.g., "Name"),
    so I don't have to type it manually and risk making a typo.
    */
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}