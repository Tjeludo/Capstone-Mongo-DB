using App_v1.Models;
using App_v1.Services;
using CommunityToolkit.Mvvm.Input;
using Syncfusion.UI.Xaml.DataGrid;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;

namespace App_v1.ViewModels; // <-- Moved to the correct ViewModels namespace

public class RolesViewModel
{
    private readonly RoleApiService _apiService;
    public ObservableCollection<RolesInfo> Roles
    {
        get; set;
    }

    public ICommand GridViewCommand
    {
        get;
    }
    public ICommand GridEditCommand
    {
        get;
    }
    public ICommand GridDeleteCommand
    {
        get;
    }

    // New properties for button visibility
    public bool CanAddRole => UserSession.HasPermission("add_roles");
    public bool CanViewRole => UserSession.HasPermission("view_roles");
    public bool CanEditRole => UserSession.HasPermission("edit_roles");
    public bool CanDeleteRole => UserSession.HasPermission("delete_roles");
    public bool CanDownloadCsv => UserSession.HasPermission("download_roles");
    public bool CanUploadCsv => UserSession.HasPermission("upload_roles");


    public event Func<RolesInfo, Task>? ViewRequested;
    public event Func<RolesInfo, Task>? EditRequested;
    public event Func<RolesInfo, Task>? DeleteRequested;

    public RolesViewModel()
    {
        _apiService = new RoleApiService();
        Roles = new ObservableCollection<RolesInfo>();
        _ = LoadRolesAsync();

        GridViewCommand = new RelayCommand<object?>(OnView);
        GridEditCommand = new RelayCommand<object?>(OnEdit);
        GridDeleteCommand = new RelayCommand<object?>(OnDelete);
    }

    public async Task LoadRolesAsync()
    {
        var response = await _apiService.GetRolesAsync();
        if (response.Success && response.Data != null)
        {
            Roles.Clear();
            foreach (var role in response.Data)
            {
                Roles.Add(role);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Error loading roles: {response.Message}");
        }
    }

    public async Task<(bool success, string message)> AddRoleAsync(RolesInfo newRole)
    {
        var response = await _apiService.AddRoleAsync(newRole);
        if (response.Success)
        {
            await LoadRolesAsync(); // Reload the list to show the new item.
        }
        return (response.Success, response.Message);
    }

    public async Task<(bool success, string message)> UpdateRoleAsync(RolesInfo role)
    {
        var response = await _apiService.UpdateRoleAsync(role);
        if (response.Success)
        {
            await LoadRolesAsync(); // Reload to show the changes.
        }
        return (response.Success, response.Message);
    }

    public async Task<(bool success, string message)> DeleteRoleAsync(int roleCode)
    {
        var response = await _apiService.DeleteRoleAsync(roleCode);
        if (response.Success)
        {
            // For a faster UI update, remove the item directly from the local collection.
            var roleToRemove = Roles.FirstOrDefault(r => r.RoleCode == roleCode);
            if (roleToRemove != null)
            {
                Roles.Remove(roleToRemove);
            }
        }
        return (response.Success, response.Message);
    }

    public async Task<byte[]?> DownloadCsvAsync()
    {
        return await _apiService.DownloadCsvAsync();
    }

    public async Task<(bool success, string message)> UploadCsvAsync(StorageFile file)
    {
        var response = await _apiService.UploadCsvAsync(file);
        if (response.Success)
        {
            await LoadRolesAsync();
        }
        return (response.Success, response.Message);
    }

    private RolesInfo? GetRoleFromParameter(object? parameter)
    {
        return parameter switch
        {
            GridRecordContextFlyoutInfo flyoutInfo => flyoutInfo.Record as RolesInfo,
            RolesInfo directRole => directRole,
            _ => null
        };
    }

    private async void OnView(object? parameter)
    {
        if (GetRoleFromParameter(parameter) is { } role)
        {
            await ViewRequested?.Invoke(role);
        }
    }

    private async void OnEdit(object? parameter)
    {
        if (GetRoleFromParameter(parameter) is { } role)
        {
            await EditRequested?.Invoke(role);
        }
    }

    private async void OnDelete(object? parameter)
    {
        if (GetRoleFromParameter(parameter) is { } role)
        {
            await DeleteRequested?.Invoke(role);
        }
    }
}