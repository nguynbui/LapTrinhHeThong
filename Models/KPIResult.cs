using System;
using System.Collections.Generic;

namespace Models
{
    public class KPIResult
    {
        public int TotalSKUs { get; set; }
        public decimal StockValue { get; set; }
        public int OutOfStockItems { get; set; }
        public decimal AverageDailySales { get; set; }
        public double AverageInventoryAgeDays { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
