using App_v1.Models;
using App_v1.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace App_v1.Pages;

public sealed partial class CategoryProductsPage : Page
{
    private CategoryProductsViewModel ViewModel => (CategoryProductsViewModel)DataContext;

    public CategoryProductsPage()
    {
        this.InitializeComponent();
        this.DataContext = new CategoryProductsViewModel();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Load products for the selected category
        if (!string.IsNullOrEmpty(AppState.SelectedCategoryId))
        {
            await ViewModel.LoadCategoryProductsAsync(AppState.SelectedCategoryId);

            // Update the page title
            PageTitle.Text = $"Products in {ViewModel.CategoryName}";
        }
    }

    private async void DownloadCSV_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowDownloadBusyState(true);
            var csvData = await ViewModel.DownloadCsvAsync();

            if (csvData is { Length: > 0 })
            {
                var savePicker = new FileSavePicker();
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker,
                    WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance));

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV Files", [".csv"]);
                savePicker.SuggestedFileName = $"category_{ViewModel.CategoryCode}_products_{DateTime.Now:dd_MM_yyyy_HHmmss}.csv";

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteBytesAsync(file, csvData);
                    ShowSuccessInfoBar("Download successful & complete");
                }
            }
            else
            {
                ShowErrorInfoBar("No data available to download.");
            }
        }
        catch (Exception ex)
        {
            ShowErrorInfoBar($"Error downloading CSV: {ex.Message}");
        }
        finally
        {
            ShowDownloadBusyState(false);
        }
    }

    private void ShowDownloadBusyState(bool isBusy)
    {
        DownloadButton.IsEnabled = !isBusy;
        DownloadContent.Visibility = isBusy ? Visibility.Collapsed : Visibility.Visible;
        DownloadBusyIndicator.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        DownloadBusyIndicator.IsActive = isBusy;
    }

    private void ShowSuccessInfoBar(string message)
    {
        SuccessInfoBar.Title = "Success";
        SuccessInfoBar.Message = message;
        SuccessInfoBar.IsOpen = true;
    }

    private void ShowErrorInfoBar(string message)
    {
        ErrorInfoBar.Title = "Error";
        ErrorInfoBar.Message = message;
        ErrorInfoBar.IsOpen = true;
    }
}