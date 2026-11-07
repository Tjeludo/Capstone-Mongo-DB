// ViewModels/MessageRequestViewModel.cs

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using App_v1.Models;
using App_v1.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace App_v1.ViewModels;

public partial class MessageRequestViewModel : ObservableObject
{
    private readonly MessageRequestApiService _apiService;

    public ObservableCollection<MessageRequestInfo> Requests
    {
        get; set;
    }

    // UI visibility controls based on user role
    public bool IsManager => UserSession.Role == "Branch Manager";
    public bool IsAdmin => UserSession.Role == "Administrator";

    public MessageRequestViewModel()
    {
        _apiService = new MessageRequestApiService();
        Requests = new ObservableCollection<MessageRequestInfo>();
        _ = LoadRequestsAsync();
    }

    public async Task LoadRequestsAsync()
    {
        ApiResponse<List<MessageRequestInfo>> response;
        if (IsAdmin)
        {
            // Admins see all requests
            response = await _apiService.GetAllRequestsAsync();
        }
        else if (IsManager)
        {
            // Managers see all of their own requests
            var staffId = UserSession.StaffId ?? "";
            response = await _apiService.GetMyRequestsAsync(staffId);
        }
        else
        {
            // No role, so no requests to load
            return;
        }

        if (response.Success && response.Data != null)
        {
            Requests.Clear();
            foreach (var req in response.Data)
            {
                Requests.Add(req);
            }
        }
    }

    public async Task<(bool success, string message)> CreateRequestAsync(string title, string body)
    {
        var requesterId = UserSession.StaffId ?? "";
        var response = await _apiService.CreateRequestAsync(title, body, requesterId);
        if (response.Success)
        {
            await LoadRequestsAsync(); // Refresh the list
        }
        return (response.Success, response.Message);
    }

    public async Task<(bool success, string message)> ApproveRequestAsync(string requestId)
    {
        var adminId = UserSession.StaffId ?? "";
        var response = await _apiService.ApproveRequestAsync(requestId, adminId);
        if (response.Success)
        {
            await LoadRequestsAsync(); // Refresh the list
        }
        return (response.Success, response.Message);
    }

    public async Task<(bool success, string message)> DeclineRequestAsync(string requestId)
    {
        var adminId = UserSession.StaffId ?? "";
        var response = await _apiService.DeclineRequestAsync(requestId, adminId);
        if (response.Success)
        {
            await LoadRequestsAsync(); // Refresh the list
        }
        return (response.Success, response.Message);
    }
}