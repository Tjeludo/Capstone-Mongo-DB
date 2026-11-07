namespace App_v1.Models;

public class OrderItem
{
    public string? SupplierId
    {
        get; set;
    }
    public string? ProductCode
    {
        get; set;
    }
    public string? Description
    {
        get; set;
    }
    public decimal Price
    {
        get; set;
    }
}