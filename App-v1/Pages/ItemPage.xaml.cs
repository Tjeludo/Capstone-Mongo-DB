// FileName: ItemPage.xaml.cs
/*
This is the "code-behind" for my ItemPage view. Its primary role is to handle
UI-specific tasks that are difficult to manage from a ViewModel, such as creating
and showing dynamic dialog boxes and updating UI elements in response to changes
in a global state (like the shopping cart). All business logic is initiated here
but processed in dedicated methods or delegated to the ViewModel where appropriate.
*/
using App_v1.Models;
using App_v1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace App_v1.Pages;

public sealed partial class ItemPage : Page
{
    private ViewModels.ItemViewModel ViewModel => (ViewModels.ItemViewModel)DataContext;

    public ItemPage()
    {
        this.InitializeComponent();

        if (ViewModel != null)
        {
            /*
            I'm subscribing to the AddToCartRequested event from the ViewModel. This
            allows the ViewModel to trigger the "add to cart" logic (which needs
            to show a UI dialog) when the user interacts with a ViewModel-bound
            control, like the right-click context menu.
            */
            ViewModel.AddToCartRequested += OnAddToCartRequested;
        }

        // The shopping cart is a global collection. I need to listen for changes to it.
        App.Cart.CollectionChanged += Cart_CollectionChanged;
        // Set the initial count on the cart badge when the page first loads.
        UpdateCartBadge();
    }

    private async void AddToCart_Click(object sender, RoutedEventArgs e)
    {
        // This handles the click from the inline "Add to Cart" button in the data grid.
        if (sender is Button { Tag: ItemInfo item })
        {
            // The actual logic is in a separate method to be reusable.
            await OnAddToCartRequested(item);
        }
    }

    private async Task OnAddToCartRequested(ItemInfo item)
    {
        // BUSINESS RULE: Prevent adding free items to the cart.
        if (item.Price <= 0)
        {
            ShowNotification("Cannot Add Item", "Items with a price of zero cannot be added to the cart.", InfoBarSeverity.Error);
            return;
        }

        /*
        I'm creating the UI for the dialog dynamically here in the code-behind.
        This is a View-specific concern that doesn't belong in the ViewModel.
        */
        var quantityNumberBox = new NumberBox
        {
            Header = "Quantity *",
            Value = 1,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
            Minimum = 1 // BUSINESS RULE: The quantity must be at least 1.
        };

        var dialog = new ContentDialog
        {
            Title = "Add Item to Cart",
            PrimaryButtonText = "Add to Cart",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = new StackPanel
            {
                Spacing = 12,
                MinWidth = 350,
                Children =
                {
                    new TextBox { Header = "Supplier ID", Text = item.SupplierId, IsReadOnly = true },
                    new TextBox { Header = "Product Code", Text = item.ProductCode, IsReadOnly = true },
                    new TextBox { Header = "Description", Text = item.Description, IsReadOnly = true },
                    new TextBox { Header = "Price (ZAR)", Text = item.Price.ToString("F2"), IsReadOnly = true },
                    quantityNumberBox
                }
            }
        };

        dialog.XamlRoot = this.XamlRoot;
        var result = await dialog.ShowAsync();

        // Only proceed if the user confirms by clicking the primary button.
        if (result == ContentDialogResult.Primary)
        {
            int quantity = (int)quantityNumberBox.Value;
            await HandleAddToCart(item, quantity);
        }
    }

    private Task HandleAddToCart(ItemInfo item, int quantity)
    {
        // BUSINESS RULE: The cart can only contain items from a single supplier at a time.
        if (App.Cart.Any() && App.Cart.First().SupplierId != item.SupplierId)
        {
            ShowNotification(
                "Cannot Add Item",
                $"You can only add items from one supplier at a time. Please clear your cart or complete the order for supplier {App.Cart.First().SupplierId} first.",
                InfoBarSeverity.Warning);
            return Task.CompletedTask;
        }

        // Check if the item is already in the cart.
        var existingCartItem = App.Cart.FirstOrDefault(ci => ci.ProductCode == item.ProductCode);

        if (existingCartItem != null)
        {
            // If it exists, just update the quantity.
            existingCartItem.Quantity += quantity;
        }
        else
        {
            // If it's a new item, create a new CartInfo object and add it to the cart.
            App.Cart.Add(new CartInfo
            {
                SupplierId = item.SupplierId,
                ProductCode = item.ProductCode,
                Description = item.Description,
                Price = (decimal)item.Price,
                Quantity = quantity
            });
        }

        ShowNotification("Success", $"'{item.Description}' has been added to your cart.", InfoBarSeverity.Success);
        return Task.CompletedTask;
    }

    private void CartButton_Click(object sender, RoutedEventArgs e)
    {
        // Simple navigation to the cart page.
        Frame.Navigate(typeof(CartPage));
    }

    private void Cart_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // This event fires whenever an item is added to or removed from the global cart.
        UpdateCartBadge();
    }



    private void UpdateCartBadge()
    {
        // Calculate the total number of items by summing up the quantities of all items in the cart.
        int totalItems = App.Cart.Sum(i => i.Quantity);
        if (CartBadgeCount != null)
        {
            CartBadgeCount.Content = totalItems.ToString();
        }
    }

    // A helper to show a notification bar for a few seconds.
    private void ShowNotification(string title, string message, InfoBarSeverity severity)
    {
        NotificationInfoBar.Title = title;
        NotificationInfoBar.Message = message;
        NotificationInfoBar.Severity = severity;
        NotificationInfoBar.IsOpen = true;

        TimeSpan duration;
        switch (severity)
        {
            case InfoBarSeverity.Success:
                duration = TimeSpan.FromSeconds(1.5);
                break;
            case InfoBarSeverity.Warning:
                duration = TimeSpan.FromSeconds(6.5);
                break;
            case InfoBarSeverity.Error:
                duration = TimeSpan.FromSeconds(5);
                break;
            default:
                duration = TimeSpan.FromSeconds(5);
                break;
        }

        var timer = new DispatcherTimer { Interval = duration };
        timer.Tick += (s, e) =>
        {
            NotificationInfoBar.IsOpen = false;
            timer.Stop();
        };
        timer.Start();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        /*
        This is a crucial cleanup step. I must unsubscribe from events to prevent
        memory leaks. If I don't, the ViewModel and the global App.Cart collection
        would keep a reference to this page object, preventing it from being
        garbage collected even after the user navigates away.
        */
        if (ViewModel != null)
        {
            ViewModel.AddToCartRequested -= OnAddToCartRequested;
        }
        App.Cart.CollectionChanged -= Cart_CollectionChanged;
    }
}