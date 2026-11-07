// ViewModels/LoginViewModel.cs

using App_v1.Models;
using App_v1.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace App_v1.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthApiService _authApiService;

    [ObservableProperty]
    private string _staffId = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    // Events for the View to subscribe to
    public event Func<Func<Task<bool>>, Task<string?>>? TwoFactorAuthRequested; // Updated event signature
    public event Action? LoginSuccess;
    public event Func<string, string, Task>? ShowDialogRequested;

    public LoginViewModel()
    {
        _authApiService = new AuthApiService();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrEmpty(StaffId) || string.IsNullOrEmpty(Password))
        {
            await OnShowDialogRequested("Error", "Please enter Staff ID and password.");
            return;
        }

        IsBusy = true;
        var response = await _authApiService.LoginAsync(StaffId, Password);
        IsBusy = false;

        if (response.Success && response.Data != null)
        {
            if (response.Data.Success && response.Data.Message == "2FA required")
            {
                // Pass the ResendOtpAsync method to the dialog
                var otpCode = await OnTwoFactorAuthRequested(ResendOtpAsync);
                if (!string.IsNullOrEmpty(otpCode))
                {
                    await Verify2faAsync(StaffId, otpCode);
                }
            }
            else
            {
                await OnShowDialogRequested("Login Failed", response.Data.Message ?? "Unknown error");
            }
        }
        else
        {
            await OnShowDialogRequested("Login Failed", response.Message);
        }
    }

    private async Task<bool> ResendOtpAsync()
    {
        // This method will be called by the dialog to resend the OTP
        var response = await _authApiService.LoginAsync(StaffId, Password); // Re-calling login to trigger OTP
        return response.Success && response.Data?.Message == "2FA required";
    }

    private async Task Verify2faAsync(string staffId, string otpCode)
    {
        IsBusy = true;
        var response = await _authApiService.Verify2faAsync(staffId, otpCode);

        if (response.Success && response.Data?.Data != null)
        {
            var userData = response.Data.Data;
            UserSession.StartSession(
                userData.StaffId,
                userData.StaffName,
                userData.StaffSurname,
                userData.Role,
                userData.StaffEmail,
                userData.PhoneNumber,
                userData.Permissions);

            await Task.Delay(500);
            IsBusy = false;

            LoginSuccess?.Invoke();
        }
        else
        {
            IsBusy = false;
            await OnShowDialogRequested("Verification Failed", response.Message ?? "Invalid code.");
        }
    }

    // Helper methods to safely invoke events
    private async Task OnShowDialogRequested(string title, string content)
    {
        if (ShowDialogRequested != null)
        {
            await ShowDialogRequested.Invoke(title, content);
        }
    }

    private async Task<string?> OnTwoFactorAuthRequested(Func<Task<bool>> resendAction)
    {
        if (TwoFactorAuthRequested != null)
        {
            return await TwoFactorAuthRequested.Invoke(resendAction);
        }
        return null;
    }
}