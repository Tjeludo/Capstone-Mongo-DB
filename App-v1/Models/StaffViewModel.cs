using App_v1.Models;
using App_v1.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace App_v1.ViewModels;

/// <summary>
/// This is the "brain" for the StaffPage. It connects the UI to my data services.
/// It holds all the data and logic, keeping the UI code-behind clean and simple.
/// </summary>
public class StaffViewModel
{
    // My connections to the backend APIs. I make them readonly so they can only be set once in the constructor.
    private readonly StaffApiService _staffApiService;
    private readonly BranchApiService _branchApiService;
    private readonly RoleApiService _roleApiService;

    // These are the lists of data that the UI will display.
    // ObservableCollection is great because the UI updates automatically when I add or remove items from these lists.
    public ObservableCollection<StaffInfo> StaffMembers
    {
        get; set;
    }
    public ObservableCollection<BranchInfo> Branches
    {
        get; set;
    }
    public ObservableCollection<RolesInfo> Roles
    {
        get; set;
    }

    // These properties control which buttons are visible based on the logged-in user's permissions.
    public bool CanAddStaff => UserSession.HasPermission("add_staff");
    public bool CanEditStaff => UserSession.HasPermission("edit_staff");
    public bool CanDeleteStaff => UserSession.HasPermission("delete_staff");
    public bool CanTransferStaff => UserSession.HasPermission("transfer_staff");
    public bool CanPromoteStaff => UserSession.HasPermission("promote_staff");
    public bool CanDownloadCsv => UserSession.HasPermission("download_staff");
    public bool CanUploadCsv => UserSession.HasPermission("upload_staff");

    // These events are how I tell the UI (StaffPage.xaml.cs) to do something, like open a dialog.
    // This keeps the ViewModel from knowing anything about UI controls, which is good practice.
    public event Func<StaffInfo, Task>? TransferRequested;
    public event Func<StaffInfo, Task>? EditRequested;
    public event Func<StaffInfo, Task>? DeleteRequested;
    public event Func<StaffInfo, Task>? PromoteRequested;

    public StaffViewModel()
    {
        _staffApiService = new StaffApiService();
        _branchApiService = new BranchApiService();
        _roleApiService = new RoleApiService();

        StaffMembers = new ObservableCollection<StaffInfo>();
        Branches = new ObservableCollection<BranchInfo>();
        Roles = new ObservableCollection<RolesInfo>();

        // Kick off the data loading in the background so the UI doesn't freeze on startup.
        _ = LoadInitialDataAsync();
    }

    // This method orchestrates loading all the data I need for the page to work.
    private async Task LoadInitialDataAsync()
    {
        // I load them in this order to make sure dependencies (like roles and branches for dropdowns) are ready.
        await LoadRolesAsync();
        await LoadBranchesAsync();
        await LoadStaffAsync();
    }

    // Fetches the list of roles for the 'Add' and 'Promote' dialogs.
    public async Task LoadRolesAsync()
    {
        var response = await _roleApiService.GetRolesAsync();
        if (response is { Success: true, Data: not null })
        {
            Roles.Clear();
            foreach (var role in response.Data)
            {
                Roles.Add(role);
            }
        }
    }

    // Fetches the main list of staff members for the data grid.
    public async Task LoadStaffAsync()
    {
        var response = await _staffApiService.GetStaffAsync();
        if (response is { Success: true, Data: not null })
        {
            StaffMembers.Clear(); // Clear the old list before adding the new one.
            foreach (var staff in response.Data)
            {
                StaffMembers.Add(staff);
            }
        }
    }

    // Fetches the list of branches for the 'Add' and 'Transfer' dialogs.
    public async Task LoadBranchesAsync()
    {
        var response = await _branchApiService.GetBranchesAsync();
        if (response is { Success: true, Data: not null })
        {
            Branches.Clear();
            foreach (var branch in response.Data)
            {
                Branches.Add(branch);
            }
        }
    }

    /// <summary>
    /// Takes a new staff member, sends it to the API, and refreshes the list on success.
    /// </summary>
    public async Task<(bool success, string message)> AddStaffAsync(StaffInfo newStaff)
    {
        var response = await _staffApiService.AddStaffAsync(newStaff);
        if (response.Success)
        {
            await LoadStaffAsync(); // Refresh the grid to show the new person.
        }
        return (response.Success, response.Message);
    }

    /// <summary>
    /// Sends an updated staff object to the API.
    /// </summary>
    public async Task<(bool success, string message)> UpdateStaffAsync(StaffInfo staff)
    {
        var response = await _staffApiService.UpdateStaffAsync(staff);
        if (response.Success)
        {
            await LoadStaffAsync(); // Refresh the grid to show the changes.
        }
        return (response.Success, response.Message);
    }

    /// <summary>
    /// Deletes a staff member. Instead of a full reload, I just remove them from the local list for a faster UI update.
    /// </summary>
    public async Task<(bool success, string message)> DeleteStaffAsync(string staffId)
    {
        var response = await _staffApiService.DeleteStaffAsync(staffId);
        if (response.Success)
        {
            var staffToRemove = StaffMembers.FirstOrDefault(s => s.Id == staffId);
            if (staffToRemove != null)
            {
                StaffMembers.Remove(staffToRemove);
            }
        }
        return (response.Success, response.Message);
    }

    /// <summary>
    /// Handles the logic for transferring a staff member.
    /// </summary>
    public async Task<(bool success, string message)> TransferStaffAsync(StaffInfo staff, string newBranchId)
    {
        var response = await _staffApiService.TransferStaffAsync(staff.Id, newBranchId);
        if (response.Success)
        {
            await LoadStaffAsync(); // Refresh to show the updated branch.
        }
        return (response.Success, response.Message);
    }

    /// <summary>
    /// Handles the logic for promoting a staff member.
    /// </summary>
    public async Task<(bool success, string message)> PromoteStaffAsync(StaffInfo staff, string newRole)
    {
        var response = await _staffApiService.PromoteStaffAsync(staff.Id, newRole);
        if (response.Success)
        {
            await LoadStaffAsync(); // Refresh to show the updated role.
        }
        return (response.Success, response.Message);
    }

    /// <summary>
    /// Gets the raw CSV data from the API service.
    /// </summary>
    public async Task<byte[]?> DownloadCsvAsync()
    {
        return await _staffApiService.DownloadCsvAsync();
    }

    /// <summary>
    /// Sends a file to the API for bulk upload and refreshes the data on success.
    /// </summary>
    public async Task<(bool success, string message)> UploadCsvAsync(StorageFile file)
    {
        var response = await _staffApiService.UploadCsvAsync(file);
        if (response.Success)
        {
            await LoadStaffAsync(); // Refresh to show the newly imported staff.
        }
        return (response.Success, response.Message);
    }
}
