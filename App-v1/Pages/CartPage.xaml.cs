using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using App_v1.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using App_v1.ViewModels;
using System.Threading.Tasks;
using App_v1.Services;
using System.ComponentModel;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using System.IO;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Collections.Generic;
using Syncfusion.Drawing;

namespace App_v1.Pages;

public sealed partial class CartPage : Page
{
    public ObservableCollection<CartInfo> Cart => App.Cart;

    public CartPage()
    {
        InitializeComponent();
        Cart.CollectionChanged += Cart_CollectionChanged;

        foreach (var item in Cart)
        {
            item.PropertyChanged += OnCartItemPropertyChanged;
        }

        UpdateTotals();
    }

    private void Cart_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.Cast<CartInfo>())
            {
                item.PropertyChanged += OnCartItemPropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.Cast<CartInfo>())
            {
                item.PropertyChanged -= OnCartItemPropertyChanged;
            }
        }
        UpdateTotals();
    }

    private void OnCartItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CartInfo.Subtotal))
        {
            UpdateTotals();
        }
    }

    private void UpdateTotals()
    {
        var total = Cart.Sum(i => i.Subtotal);
        TotalTextBlock.Text = $"Total: R {total:N2}";
        ConfirmOrderButton.IsEnabled = Cart.Any() && UserSession.HasPermission("confirm_orders");
        ClearCartButton.IsEnabled = Cart.Any();
    }

    private void Increase_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string productCode)
        {
            var item = Cart.FirstOrDefault(i => i.ProductCode == productCode);
            if (item != null)
            {
                item.Quantity++;
            }
        }
    }

    private void Decrease_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string productCode)
        {
            var item = Cart.FirstOrDefault(i => i.ProductCode == productCode);
            if (item != null)
            {
                item.Quantity--;
                if (item.Quantity <= 0)
                {
                    Cart.Remove(item);
                }
            }
        }
    }

    private async void ClearCart_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Confirm Clear Cart",
            Content = "Are you sure you want to remove all items from the cart?",
            PrimaryButtonText = "Clear",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            Cart.Clear();
        }
    }

    private async void ConfirmOrder_Click(object sender, RoutedEventArgs e)
    {
        var orderViewModel = new OrdersViewModel();
        var (success, message) = await orderViewModel.ConfirmOrderAsync(UserSession.StaffId ?? "N/A");

        if (success)
        {
            var dialog = new ContentDialog
            {
                Title = "Order Confirmed",
                Content = "What would you like to do next?",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var stackPanel = new StackPanel { Spacing = 10 };
            var generateInvoiceRadio = new RadioButton { Content = "Generate Invoice", IsChecked = true };
            var emailSupplierRadio = new RadioButton { Content = "Email Supplier" };
            var smsSupplierRadio = new RadioButton { Content = "SMS Supplier" };
            var callSupplierRadio = new RadioButton { Content = "Call Supplier" };

            stackPanel.Children.Add(generateInvoiceRadio);
            stackPanel.Children.Add(emailSupplierRadio);
            stackPanel.Children.Add(smsSupplierRadio);
            stackPanel.Children.Add(callSupplierRadio);
            dialog.Content = stackPanel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var supplierId = Cart.FirstOrDefault()?.SupplierId;
                if (supplierId != null)
                {
                    if (generateInvoiceRadio.IsChecked == true) await GenerateInvoice(supplierId);
                    else if (emailSupplierRadio.IsChecked == true) await EmailSupplier(supplierId);
                    else if (smsSupplierRadio.IsChecked == true) await SmsSupplier(supplierId);
                    else if (callSupplierRadio.IsChecked == true) await CallSupplier(supplierId);
                }
            }

            Cart.Clear();
            this.Frame.Navigate(typeof(ItemPage));

            var pageTypesToRemove = new[] { typeof(ItemPage), typeof(CartPage) };

            var entriesToRemove = Frame.BackStack
                .Where(entry => pageTypesToRemove.Contains(entry.SourcePageType))
                .ToList();

            foreach (var entry in entriesToRemove)
            {
                Frame.BackStack.Remove(entry);
            }
        }
        else
        {
            await ShowNotification("Error", $"Failed to confirm order: {message}", InfoBarSeverity.Error);
        }
    }

    private async Task GenerateInvoice(string supplierId)
    {
        //Create a new PDF document.
        PdfDocument document = new PdfDocument();
        //Set page orientation and margin. 
        document.PageSettings.Orientation = PdfPageOrientation.Landscape;
        document.PageSettings.Margins.All = 50;
        //Add a page to the document.
        PdfPage page = document.Pages.Add();
        //Create PDF graphics for the page.
        PdfGraphics graphics = page.Graphics;

        //Create a text element with the text and font.
        PdfTextElement element = new PdfTextElement("Baby Bargains\n123 Innovation Drive,\nClaremont, Cape Town,\nSouth Africa\n7700.");
        element.Font = new PdfStandardFont(PdfFontFamily.TimesRoman, 12);
        element.Brush = new PdfSolidBrush(new PdfColor(89, 89, 93));
        PdfLayoutResult result = element.Draw(page, new RectangleF(0, 0, page.Graphics.ClientSize.Width / 2, 200));

        PdfFont subHeadingFont = new PdfStandardFont(PdfFontFamily.TimesRoman, 14);
        graphics.DrawRectangle(new PdfSolidBrush(new PdfColor(126, 151, 173)), new RectangleF(0, result.Bounds.Bottom + 40, graphics.ClientSize.Width, 30));

        //Create a text element with the text and font.
        element = new PdfTextElement("INVOICE", subHeadingFont);
        element.Brush = PdfBrushes.White;
        result = element.Draw(page, new PointF(10, result.Bounds.Bottom + 48));
        string currentDate = "DATE " + DateTime.Now.ToString("MM/dd/yyyy");
        SizeF textSize = subHeadingFont.MeasureString(currentDate);
        graphics.DrawString(currentDate, subHeadingFont, element.Brush, new PointF(graphics.ClientSize.Width - textSize.Width - 10, result.Bounds.Y));

        //Create a text element and draw it to a PDF page.
        element = new PdfTextElement($"Bill To: Supplier {supplierId}", new PdfStandardFont(PdfFontFamily.TimesRoman, 11));
        element.Brush = new PdfSolidBrush(new PdfColor(126, 155, 203));
        result = element.Draw(page, new PointF(10, result.Bounds.Bottom + 25));
        graphics.DrawLine(new PdfPen(new PdfColor(126, 151, 173), 0.70f), new PointF(0, result.Bounds.Bottom + 3), new PointF(graphics.ClientSize.Width, result.Bounds.Bottom + 3));

        // Create a list from cart data for the grid.
        var invoiceItems = Cart.Select(item => new
        {
            ProductCode = item.ProductCode,
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = item.Price,
            Total = item.Subtotal
        }).ToList();

        //Create a PDF grid with the product details.
        PdfGrid grid = new PdfGrid();
        grid.DataSource = invoiceItems;

        //Initialize PdfGridCellStyle and set the border color.
        PdfGridCellStyle cellStyle = new PdfGridCellStyle();
        cellStyle.Borders.All = PdfPens.White;
        cellStyle.Borders.Bottom = new PdfPen(new PdfColor(217, 217, 217), 0.70f);
        cellStyle.Font = new PdfStandardFont(PdfFontFamily.TimesRoman, 12f);
        cellStyle.TextBrush = new PdfSolidBrush(new PdfColor(131, 130, 136));

        //Initialize PdfGridCellStyle and set the header style.
        PdfGridCellStyle headerStyle = new PdfGridCellStyle();
        headerStyle.Borders.All = new PdfPen(new PdfColor(126, 151, 173));
        headerStyle.BackgroundBrush = new PdfSolidBrush(new PdfColor(126, 151, 173));
        headerStyle.TextBrush = PdfBrushes.White;
        headerStyle.Font = new PdfStandardFont(PdfFontFamily.TimesRoman, 14f, PdfFontStyle.Regular);

        PdfGridRow header = grid.Headers[0];
        for (int i = 0; i < header.Cells.Count; i++)
        {
            if (i == 0 || i == 1)
                header.Cells[i].StringFormat = new PdfStringFormat(PdfTextAlignment.Left, PdfVerticalAlignment.Middle);
            else
                header.Cells[i].StringFormat = new PdfStringFormat(PdfTextAlignment.Right, PdfVerticalAlignment.Middle);
        }
        header.ApplyStyle(headerStyle);

        foreach (PdfGridRow row in grid.Rows)
        {
            row.ApplyStyle(cellStyle);
            for (int i = 0; i < row.Cells.Count; i++)
            {
                PdfGridCell cell = row.Cells[i];
                // Align ProductCode (0) and Description (1) to the left
                if (i == 0 || i == 1)
                    cell.StringFormat = new PdfStringFormat(PdfTextAlignment.Left, PdfVerticalAlignment.Middle);
                else // Align other columns to the right
                    cell.StringFormat = new PdfStringFormat(PdfTextAlignment.Right, PdfVerticalAlignment.Middle);

                // Format currency columns
                if (i > 2)
                {
                    decimal val = 0m;
                    decimal.TryParse(cell.Value.ToString(), out val);
                    cell.Value = 'R' + val.ToString("N2");
                }
            }
        }

        grid.Columns[1].Width = 200;

        //Set properties to paginate the grid.
        PdfGridLayoutFormat layoutFormat = new PdfGridLayoutFormat();
        layoutFormat.Layout = PdfLayoutType.Paginate;

        //Draw a grid on the page of the PDF document.
        PdfGridLayoutResult gridResult = grid.Draw(page, new RectangleF(new PointF(0, result.Bounds.Bottom + 40), new SizeF(graphics.ClientSize.Width, graphics.ClientSize.Height - 100)), layoutFormat);

        // Calculate total and draw it
        decimal total = Cart.Sum(i => i.Subtotal);
        float pos = 0.0f;
        for (int i = 0; i < grid.Columns.Count - 1; i++)
            pos += grid.Columns[i].Width;

        PdfFont font = new PdfStandardFont(PdfFontFamily.TimesRoman, 14f);
        gridResult.Page.Graphics.DrawString("Total Due", font, new PdfSolidBrush(new PdfColor(126, 151, 173)), new RectangleF(new PointF(pos, gridResult.Bounds.Bottom + 20), new SizeF(grid.Columns[3].Width - pos, 20)), new PdfStringFormat(PdfTextAlignment.Right));
        gridResult.Page.Graphics.DrawString("Pleasure doing business with you!", new PdfStandardFont(PdfFontFamily.TimesRoman, 12), new PdfSolidBrush(new PdfColor(89, 89, 93)), new PointF(pos - 55, gridResult.Bounds.Bottom + 60));
        pos += grid.Columns[3].Width;
        gridResult.Page.Graphics.DrawString('R' + string.Format("{0:N2}", total), font, new PdfSolidBrush(new PdfColor(131, 130, 136)), new RectangleF(new PointF(pos, gridResult.Bounds.Bottom + 20), new SizeF(grid.Columns[4].Width - pos, 20)), new PdfStringFormat(PdfTextAlignment.Right));

        //Save the PDF document to stream and prompt user for location.
        using (MemoryStream stream = new MemoryStream())
        {
            document.Save(stream);
            document.Close(true);
            stream.Position = 0;

            var picker = new FileSavePicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance!);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("PDF", new List<string> { ".pdf" });
            picker.SuggestedFileName = $"Invoice_Supplier_{supplierId}_{DateTime.Now:yyyyMMdd}.pdf";

            StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteBytesAsync(file, stream.ToArray());
                await ShowNotification("Success", $"Invoice saved successfully as {file.Name}", InfoBarSeverity.Success);
            }
        }
    }


    private async Task EmailSupplier(string supplierId)
    {
        //await GenerateInvoice(supplierId);
        await ShowNotification("Success", $"Email sent to supplier {supplierId}", InfoBarSeverity.Success);
    }

    private async Task SmsSupplier(string supplierId)
    {
        //await GenerateInvoice(supplierId);
        await ShowNotification("Success", $"SMS sent to supplier {supplierId}", InfoBarSeverity.Success);
    }

    private async Task CallSupplier(string supplierId)
    {
        //await GenerateInvoice(supplierId);
        await ShowNotification("Success", $"Call initiated to supplier {supplierId}", InfoBarSeverity.Success);
    }

    private Task ShowNotification(string title, string message, InfoBarSeverity severity)
    {
        var tcs = new TaskCompletionSource();

        NotificationInfoBar.Title = title;
        NotificationInfoBar.Message = message;
        NotificationInfoBar.Severity = severity;
        NotificationInfoBar.IsOpen = true;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (s, e) =>
        {
            NotificationInfoBar.IsOpen = false;
            timer.Stop();
            tcs.SetResult();
        };
        timer.Start();

        return tcs.Task;
    }
}