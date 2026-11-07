/*
This is my PermissionsViewModel. Its main jobs are to hold the UI's state
(like the list of permissions) and to handle the logic when a user interacts
with the page. All the heavy lifting, especially API calls, is delegated
to the PermissionsApiService. This keeps my ViewModel clean and focused on UI logic.
*/
using App_v1.Models;
using App_v1.Services;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace App_v1.ViewModels;

public class PermissionsViewModel
{
    private readonly PermissionsApiService _permissionsApiService;

    public ObservableCollection<PermissionsInfo> Permissions
    {
        get; set;
    }

    // Placeholder for potential future permission checks
    public bool CanAddPermission => true; // UserSession.HasPermission("add_permissions");
    public bool CanGrantPermission => true; // UserSession.HasPermission("grant_permissions");
    public bool CanRevokePermission => true; // UserSession.HasPermission("revoke_permissions");

    /*
    These events allow the ViewModel to ask the View (the code-behind)
    to perform UI-specific actions, like showing a dialog window.
    */
    public event Func<Task>? AddRequested;
    public event Func<PermissionsInfo, Task>? GrantRequested;
    public event Func<PermissionsInfo, Task>? RevokeRequested;

    public PermissionsViewModel()
    {
        _permissionsApiService = new PermissionsApiService();
        Permissions = new ObservableCollection<PermissionsInfo>();
        _ = LoadPermissionsAsync();
    }

    public async Task LoadPermissionsAsync()
    {
        var response = await _permissionsApiService.GetPermissionsAsync();
        if (response.Success && response.Data != null)
        {
            Permissions.Clear();
            foreach (var perm in response.Data)
            {
                Permissions.Add(perm);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Error loading permissions: {response.Message}");
        }
    }

    public async Task<(bool success, string message)> AddPermissionsAsync(string section)
    {
        if (string.IsNullOrWhiteSpace(section))
        {
            return (false, "Section name cannot be empty.");
        }

        var response = await _permissionsApiService.AddPermissionsAsync(section);
        if (response.Success)
        {
            await LoadPermissionsAsync(); // Reload the list to show the new items.
        }
        return (response.Success, response.Message);
    }

    public async Task<(bool success, string message)> GrantPermissionAsync(string permissionId, string roleName)
    {
        var response = await _permissionsApiService.GrantPermissionAsync(permissionId, roleName);
        if (response.Success)
        {
            // After a successful grant, update the current user's session permissions
            await UserSession.UpdatePermissionsAsync();
            await LoadPermissionsAsync(); // Reload to show status changes.
        }
        return (response.Success, response.Message);
    }

    public async Task<(bool success, string message)> RevokePermissionAsync(string permissionId, string roleName)
    {
        var response = await _permissionsApiService.RevokePermissionAsync(permissionId, roleName);
        if (response.Success)
        {
            // After a successful revoke, update the current user's session permissions
            await UserSession.UpdatePermissionsAsync();
            await LoadPermissionsAsync(); // Reload to show status changes.
        }
        return (response.Success, response.Message);
    }

    // Methods to be called from View code-behind to trigger events
    public async Task RequestAdd() => await (AddRequested?.Invoke() ?? Task.CompletedTask);
    public async Task RequestGrant(PermissionsInfo perm) => await (GrantRequested?.Invoke(perm) ?? Task.CompletedTask);
    public async Task RequestRevoke(PermissionsInfo perm) => await (RevokeRequested?.Invoke(perm) ?? Task.CompletedTask);
}