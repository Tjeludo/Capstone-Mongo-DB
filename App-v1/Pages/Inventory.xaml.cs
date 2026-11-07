using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syncfusion.UI.Xaml.DataGrid;

namespace App_v1.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Inventory : Page
{
    public Inventory()
    {
        InitializeComponent();
    }

    private void InventoryDataGrid_AutoGeneratingColumn(object sender, AutoGeneratingColumnArgs e)
    {
       // The MappingName corresponds to the property name in your Inventory model[cite: 3].
        if (e.Column.MappingName == "Quantity")
        {
            // --- Style for the "Quantity" column ---
            var leftCellStyle = new Style(typeof(GridCell));

            // Create setters for left horizontal and center vertical alignment
            var horizontalSetter = new Setter(GridCell.HorizontalContentAlignmentProperty, HorizontalAlignment.Left);
            var verticalSetter = new Setter(GridCell.VerticalContentAlignmentProperty, VerticalAlignment.Center);

            // Add the setters to the style
            leftCellStyle.Setters.Add(horizontalSetter);
            leftCellStyle.Setters.Add(verticalSetter);

            // Apply this new style ONLY to the Quantity column
            e.Column.CellStyle = leftCellStyle;
        }
        else
        {
            // --- Style for all other columns ---
            var centerCellStyle = new Style(typeof(GridCell));

            // Create setters to center the content
            var horizontalSetter = new Setter(GridCell.HorizontalContentAlignmentProperty, HorizontalAlignment.Center);
            var verticalSetter = new Setter(GridCell.VerticalContentAlignmentProperty, VerticalAlignment.Center);

            // Add the setters to the style
            centerCellStyle.Setters.Add(horizontalSetter);
            centerCellStyle.Setters.Add(verticalSetter);

            // Apply the center-aligned style to the current column [cite: 6]
            e.Column.CellStyle = centerCellStyle;
        }
    }
}