namespace App_v1.Models;

public class InventoryItem
{
    public string itemID { get; set; } = "";
    public string itemName { get; set; } = "";
    public int QuantityOnHand { get; set; } = 0;
    public int CountedQuantity { get; set; } = 0;

    public InventoryItem()
    {
    }

    public InventoryItem(string itemID, string itemName, int quantityOnHand)
    {
        this.itemID = itemID;
        this.itemName = itemName;
        this.QuantityOnHand = quantityOnHand;
        this.CountedQuantity = 0;
    }
}

public class StockTakeNumberResponse
{
    public string stockTakeNum { get; set; } = string.Empty;
}