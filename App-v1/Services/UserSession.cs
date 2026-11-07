// Services/UserSession.cs

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App_v1.Services;

/*
 - This class manages the user's session information globally across the app
 - It holds the logged-in staff member's name, role, and permissions
 - Static class means all properties and methods belong to the class itself, not to any instance
 - This allows session data to be accessed from anywhere in the app without creating objects
*/
public static class UserSession
{
    public static string? StaffId
    {
        get; private set;
    }
    public static string? StaffName
    {
        get; private set;
    }
    public static string? StaffSurname
    {
        get; private set;
    }
    public static string? Role
    {
        get; private set;
    }
    public static string? StaffEmail
    {
        get; private set;
    }
    public static string? PhoneNumber
    {
        get; private set;
    }
    private static HashSet<string> Permissions { get; set; } = new();

    /*
     - StartSession sets up the session when a user successfully logs in
     - It takes the user's information and stores it in the static properties
     - This method is called from the login process after authentication succeeds
    */
    public static void StartSession(string staffId, string staffName, string staffSurname, string role, string staffEmail, string phoneNumber, List<string> permissions)
    {
        StaffId = staffId;
        StaffName = staffName;
        StaffSurname = staffSurname;
        Role = role;
        StaffEmail = staffEmail;
        PhoneNumber = phoneNumber;
        Permissions = new HashSet<string>(permissions);
    }

    /*
     - EndSession clears all session data when a user logs out
     - This ensures no leftover data from previous sessions
     - Should be called when user explicitly logs out or session expires
    */
    public static void EndSession()
    {
        StaffId = null;
        StaffName = null;
        StaffSurname = null;
        Role = null;
        StaffEmail = null;
        PhoneNumber = null;
        Permissions.Clear();
    }

    /*
     - UpdatePermissionsAsync fetches the latest permissions from the API
     - and updates the current session.
    */
    public static async Task UpdatePermissionsAsync()
    {
        if (string.IsNullOrEmpty(StaffId))
        {
            // Cannot update permissions if no user is logged in.
            return;
        }

        var authService = new AuthApiService();
        var response = await authService.GetUpdatedPermissionsAsync(StaffId);

        if (response.Success && response.Data != null)
        {
            // Replace the old permissions with the freshly fetched ones.
            Permissions = new HashSet<string>(response.Data);
            System.Diagnostics.Debug.WriteLine("User session permissions updated successfully.");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update user session permissions: {response.Message}");
        }
    }

    /*
     - HasPermission checks if the current user has a specific permission
     - It returns true if the user has the permission, false otherwise
     - Uses HashSet.Contains() which is very fast (O(1) average time)
    */
    public static bool HasPermission(string permission)
    {
        return Permissions.Contains(permission);
    }
}