// CategoryViewModel.cs
using App_v1.Models;
using App_v1.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;

namespace App_v1.ViewModels;

// The "brain" for CategoriesPage. It connects the view to the data services and business logic.
public class CategoryViewModel
{
    // The service that actually talks to the API.
    private readonly CategoryApiService _categoryApiService;

    // A special collection that automatically updates the UI when items are added or removed.
    public ObservableCollection<CategoryInfo> Categories
    {
        get; set;
    }

    // Properties for controlling button visibility in the UI based on user permissions.
    public bool CanAddCategory => UserSession.HasPermission("add_categories");
    public bool CanEditCategory => UserSession.HasPermission("edit_categories");
    public bool CanViewCategory => UserSession.HasPermission("view_categories");
    public bool CanDownloadCsv => UserSession.HasPermission("download_categories");
    public bool CanUploadCsv => UserSession.HasPermission("upload_categories");

    // Events that the page can subscribe to for navigation.
    public event Func<CategoryInfo, Task>? ViewRequested;
    public event Func<CategoryInfo, Task>? EditRequested;

    public CategoryViewModel()
    {
        _categoryApiService = new CategoryApiService();
        Categories = new ObservableCollection<CategoryInfo>();
        // Fire-and-forget: load categories in the background.
        _ = LoadCategoriesAsync();
    }

    // Fetches categories from the API and populates the collection.
    public async Task LoadCategoriesAsync()
    {
        var response = await _categoryApiService.GetCategoriesAsync();
        if (response.Success && response.Data != null)
        {
            Categories.Clear();
            foreach (var category in response.Data)
            {
                Categories.Add(category);
            }
        }
        else
        {
            Debug.WriteLine($"Error loading categories: {response.Message}");
        }
    }

    // Adds a new category via the API and reloads the local list on success.
    public async Task<(bool success, string message)> AddCategoryAsync(CategoryInfo newCategory)
    {
        var response = await _categoryApiService.AddCategoryAsync(newCategory);
        if (response.Success)
        {
            await LoadCategoriesAsync();
        }
        return (response.Success, response.Message);
    }

    // Updates a category and reloads the list.
    public async Task<(bool success, string message)> UpdateCategoryAsync(CategoryInfo category)
    {
        var response = await _categoryApiService.UpdateCategoryAsync(category);
        if (response.Success)
        {
            await LoadCategoriesAsync();
        }
        return (response.Success, response.Message);
    }

    // Gets the raw CSV data from the API.
    public async Task<byte[]?> DownloadCsvAsync()
    {
        return await _categoryApiService.DownloadCsvAsync();
    }

    // Uploads a CSV file and reloads the list to show new data.
    public async Task<(bool success, string message)> UploadCsvAsync(StorageFile file)
    {
        var response = await _categoryApiService.UploadCsvAsync(file);
        if (response.Success)
        {
            await LoadCategoriesAsync();
        }
        return (response.Success, response.Message);
    }
}