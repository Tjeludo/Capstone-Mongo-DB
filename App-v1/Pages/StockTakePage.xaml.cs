using App_v1.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.UI.Xaml.Grids;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Net.Http;
using System.Net.Http.Json;


namespace App_v1.Pages
{
    
    public sealed partial class StockTakePage : Page
    {
        private readonly string apiBaseUrl = "http://127.0.0.1:8000";
        private static readonly HttpClient client = new HttpClient();

        public StockTakePage()
        {
            InitializeComponent();
        }

        public void Add_StockTake_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(StockTakeForm));
        }

        public void Print_Template_Click(object sender, RoutedEventArgs e)
        {
            _ = PrintTemplateAsync();
        }

        private async Task PrintTemplateAsync()
        {
            string? branchID = await PromptForBranchSelectionAsync();
            if (string.IsNullOrEmpty(branchID))
                return;

            // Fetch inventory for the selected branch
            List<InventoryItem> items = await GetInventoryForBranchAsync(branchID);
            if (items.Count == 0)
            {
                await ShowDialogAsync("No Items", $"No inventory items found for branch {branchID}");
                return;
            }

            // Generate PDF
            await GeneratePdfAsync(items, branchID);
        }

        private async Task<string?> PromptForBranchSelectionAsync()
        {
            var comboBox = new ComboBox
            {
                ItemsSource = (this.DataContext as StockTakeViewModel)?.Branches,
                DisplayMemberPath = "BranchCity",
                SelectedValuePath = "BranchCode",
                PlaceholderText = "Select a branch",
                Margin = new Thickness(0, 10, 0, 0)
            };

            var dialog = new ContentDialog
            {
                Title = "Select Branch",
                Content = comboBox,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && comboBox.SelectedValue != null)
            {
                return comboBox.SelectedValue.ToString();
            }

            return null;
        }

        private async Task<List<InventoryItem>> GetInventoryForBranchAsync(string branchID)
        {
            if (string.IsNullOrWhiteSpace(branchID))
                return new List<InventoryItem>();

            try
            {
                string requestUrl = $"{apiBaseUrl}/get_inventory_by_branch?branchId={Uri.EscapeDataString(branchID)}";
                HttpResponseMessage response = await client.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<List<InventoryItem>>(responseBody, options) ?? new List<InventoryItem>();
                }
            }
            catch
            {
              
            }

            return new List<InventoryItem>();
        }

        private async Task GeneratePdfAsync(List<InventoryItem> items, string branchID)
        {
            // Create PDF document
            PdfDocument document = new PdfDocument();
            PdfPage page = document.Pages.Add();
            PdfGraphics graphics = page.Graphics;

            PdfFont headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 18, PdfFontStyle.Bold);
            PdfFont subHeaderFont = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Regular);
            PdfFont regularFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Regular);

            float yPosition = 0;

            // Draw Title
            graphics.DrawString($"Stock Take - Branch {branchID}", headerFont, PdfBrushes.Black, new Syncfusion.Drawing.PointF(0, yPosition));
            yPosition += headerFont.MeasureString("Stock Take").Height + 10;

            // Draw Date
            string dateText = $"Date: {DateTime.Now:yyyy-MM-dd}";
            graphics.DrawString(dateText, subHeaderFont, PdfBrushes.Black, new Syncfusion.Drawing.PointF(0, yPosition));
            yPosition += subHeaderFont.MeasureString(dateText).Height + 20;

            // Create PDF grid
            PdfGrid grid = new PdfGrid();
            grid.Columns.Add(4); // ItemID, ItemName, QuantityOnHand, CountedQuantity

            // Header row
            var headerRow = grid.Headers.Add(1)[0];
            headerRow.Cells[0].Value = "Item ID";
            headerRow.Cells[1].Value = "Item Name";
            headerRow.Cells[2].Value = "Quantity On Hand";
            headerRow.Cells[3].Value = "Counted Quantity";

            headerRow.Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(68, 114, 196));
            headerRow.Style.TextBrush = PdfBrushes.White;
            headerRow.Style.Font = regularFont;
            headerRow.Cells[0].StringFormat = new PdfStringFormat { Alignment = PdfTextAlignment.Center };
            headerRow.Cells[1].StringFormat = new PdfStringFormat { Alignment = PdfTextAlignment.Center };
            headerRow.Cells[2].StringFormat = new PdfStringFormat { Alignment = PdfTextAlignment.Center };
            headerRow.Cells[3].StringFormat = new PdfStringFormat { Alignment = PdfTextAlignment.Center };

            // Add inventory items
            bool alternateRow = false;
            foreach (var item in items)
            {
                var row = grid.Rows.Add();
                row.Cells[0].Value = item.itemID;
                row.Cells[1].Value = item.itemName.ToString();
                row.Cells[2].Value = item.QuantityOnHand.ToString();
                row.Cells[3].Value = item.CountedQuantity;

                if (alternateRow)
                    row.Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(230, 230, 230));
                alternateRow = !alternateRow;

                row.Cells[0].StringFormat = new PdfStringFormat { Alignment = PdfTextAlignment.Center };
                row.Cells[1].StringFormat = new PdfStringFormat { Alignment = PdfTextAlignment.Center };
                row.Cells[2].StringFormat = new PdfStringFormat { Alignment = PdfTextAlignment.Center };
                row.Cells[3].StringFormat = new PdfStringFormat { Alignment = PdfTextAlignment.Center };
            }

            grid.ApplyBuiltinStyle(PdfGridBuiltinStyle.ListTable4Accent5);
            grid.Draw(page, new Syncfusion.Drawing.PointF(0, yPosition));

            // Save PDF
            using (MemoryStream stream = new MemoryStream())
            {
                document.Save(stream);
                document.Close();
                stream.Position = 0;

                var picker = new FileSavePicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance!);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.SuggestedStartLocation = PickerLocationId.Desktop;
                picker.FileTypeChoices.Add("PDF", new List<string> { ".pdf" });
                picker.SuggestedFileName = $"StockTake_Branch_{branchID}_{DateTime.Now:yyyyMMdd}.pdf";

                StorageFile file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteBytesAsync(file, stream.ToArray());
                }
            }
        }

        private async Task ShowDialogAsync(string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is StockTake stockTake)
            {

                var itemsGrid = new Syncfusion.UI.Xaml.DataGrid.SfDataGrid
                {
                    AutoGenerateColumns = false,
                    ItemsSource = stockTake.Items,

                    AllowEditing = false,
                    AllowSorting = true,
                    AllowResizingColumns = true,
                    GridLinesVisibility = GridLinesVisibility.Both,

                    ColumnWidthMode = ColumnWidthMode.Auto,
                    RowHeight = 35,
                    HeaderRowHeight = 40,
                    AllowTriStateSorting = true,


                    Height = 400,
                    Width = 650
                };

                itemsGrid.Columns.Add(new Syncfusion.UI.Xaml.DataGrid.GridTextColumn
                {
                    MappingName = "ItemID",
                    HeaderText = "Product Code"
                });
                itemsGrid.Columns.Add(new Syncfusion.UI.Xaml.DataGrid.GridTextColumn
                {
                    MappingName = "ItemName",
                    HeaderText = "Product Name",
                    TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
                });
                itemsGrid.Columns.Add(new Syncfusion.UI.Xaml.DataGrid.GridTextColumn
                {
                    MappingName = "PreviousQuantity",
                    HeaderText = "Previous Quantity"
                });
                itemsGrid.Columns.Add(new Syncfusion.UI.Xaml.DataGrid.GridTextColumn
                {
                    MappingName = "quantityOnHand",
                    HeaderText = "Quantity on Hand"
                });

                // Wrap grid in a ContentDialog
                var dialog = new ContentDialog
                {
                    Title = $"Stock Take {stockTake.StockTakeNum} - Details",
                    Content = itemsGrid,
                    PrimaryButtonText = "Close",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
        }

    }
}