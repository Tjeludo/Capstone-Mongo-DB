using App_v1.Models;
using App_v1.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace App_v1.ViewModels; // <-- Moved to the correct ViewModels namespace

public class UserViewModel
{
    private readonly UserApiService _apiService;
    public ObservableCollection<UserInfo> Users
    {
        get; set;
    }

    // New property for button visibility
    public bool CanChangeStatus => UserSession.HasPermission("status_users");

    public UserViewModel()
    {
        _apiService = new UserApiService();
        Users = new ObservableCollection<UserInfo>();
        _ = LoadUsersAsync();
    }

    public async Task LoadUsersAsync()
    {
        var response = await _apiService.GetUsersAsync();
        if (response.Success && response.Data != null)
        {
            Users.Clear();
            foreach (var user in response.Data)
            {
                Users.Add(user);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Error loading users: {response.Message}");
        }
    }

    public async Task<(bool success, string message)> UpdateUserStatusAsync(UserInfo user)
    {
        var newStatus = user.Status.Equals("active", StringComparison.OrdinalIgnoreCase) ? "inactive" : "active";
        var response = await _apiService.UpdateUserStatusAsync(user.StaffID, newStatus);

        if (response.Success)
        {
            // Update the local object for immediate UI feedback without a full reload.
            user.Status = newStatus;
        }

        return (response.Success, response.Message);
    }
}