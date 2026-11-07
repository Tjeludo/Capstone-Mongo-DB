// Helpers/AppState.cs

namespace App_v1.Helpers;

public static class AppState
{
    public static string? SelectedCategoryId
    {
        get; set;
    }
    public static string? SelectedSupplierId
    {
        get; set;
    }

    public static string? SelectedOrderId
    {
        get; set;
    }

    public static int SelectedRoleCode
    {
        get; set;
    }
    public static string? SelectedRoleName
    {
        get; set;
    }
}