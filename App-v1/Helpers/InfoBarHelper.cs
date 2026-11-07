using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace App_v1.Helpers;

public static class InfoBarHelper
{
    public static async void ShowInfoBar(InfoBar infoBar, string message, int durationMs = 3500)
    {
        if (infoBar == null) return;

        infoBar.Message = message;
        infoBar.IsOpen = true;

        await Task.Delay(durationMs);

        infoBar.IsOpen = false;
    }
}
