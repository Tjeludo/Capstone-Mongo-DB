// Models/AnalyticsInfo.cs

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace App_v1.Models;

public class ProductMetric
{
    [JsonPropertyName("product_code")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("total_quantity")]
    public int TotalQuantity
    {
        get; set;
    }
}

public class StatusMetric
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count
    {
        get; set;
    }
}

public class SupplierMetric
{
    [JsonPropertyName("supplier_id")]
    public string SupplierId { get; set; } = string.Empty;

    [JsonPropertyName("order_count")]
    public int OrderCount
    {
        get; set;
    }
}

public class AnalyticsDashboardData
{
    [JsonPropertyName("most_ordered_products")]
    public List<ProductMetric> MostOrderedProducts { get; set; } = new();

    [JsonPropertyName("order_status_distribution")]
    public List<StatusMetric> OrderStatusDistribution { get; set; } = new();

    [JsonPropertyName("order_frequency_by_supplier")]
    public List<SupplierMetric> OrderFrequencyBySupplier { get; set; } = new();
}