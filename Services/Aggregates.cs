using System;
using System.Collections.Generic;
using System.Linq;
using Models;

namespace Services
{
    public class Aggregates
    {
        private readonly object _lock = new();

        private class Lot { public int Quantity; public decimal UnitCost; public DateTime PurchaseDate; }
        private readonly Dictionary<string, List<Lot>> _lots = new();
        private readonly HashSet<string> _allSkus = new();
        private readonly Dictionary<DateTime, long> _dailySales = new(); // ngày -> số lượng

        public void ApplyPurchase(PurchaseOrder po)
        {
            lock (_lock)
            {
                _allSkus.Add(po.ProductId);
                if (!_lots.TryGetValue(po.ProductId, out var list))
                {
                    list = new List<Lot>();
                    _lots[po.ProductId] = list;
                }
                list.Add(new Lot { Quantity = po.QuantityPurchased, UnitCost = po.UnitCost, PurchaseDate = po.PurchaseDate.Date });
            }
        }

        public void ApplyInvoice(Models.Invoice inv)
        {
            lock (_lock)
            {
                _allSkus.Add(inv.ProductId);
                if (!_lots.TryGetValue(inv.ProductId, out var list) || list.Sum(x => x.Quantity) <= 0)
                {
                    // Không còn tồn; khởi tạo danh sách rỗng để biểu diễn tồn (âm nếu cần)
                    _lots.TryAdd(inv.ProductId, new List<Lot>());
                }

                var remaining = inv.QuantitySold;
                if (_lots.TryGetValue(inv.ProductId, out list))
                {
                    // FIFO: tiêu thụ lô cũ nhất trước
                    list.Sort((a, b) => a.PurchaseDate.CompareTo(b.PurchaseDate));
                    for (int i = 0; i < list.Count && remaining > 0; i++)
                    {
                        var lot = list[i];
                        if (lot.Quantity <= remaining)
                        {
                            remaining -= lot.Quantity;
                            lot.Quantity = 0;
                        }
                        else
                        {
                            lot.Quantity -= remaining;
                            remaining = 0;
                        }
                    }
                    // Loại bỏ lô đã hết
                    list.RemoveAll(x => x.Quantity == 0);
                }

                if (remaining > 0)
                {
                    // Bán vượt tồn -> lưu lô âm để biểu diễn tồn âm
                    var negList = _lots.GetValueOrDefault(inv.ProductId) ?? new List<Lot>();
                    negList.Insert(0, new Lot { Quantity = -remaining, UnitCost = 0m, PurchaseDate = DateTime.UtcNow.Date });
                    _lots[inv.ProductId] = negList;
                }

                var day = inv.InvoiceDate.Date;
                if (!_dailySales.ContainsKey(day)) _dailySales[day] = 0;
                _dailySales[day] += inv.QuantitySold;
            }
        }

        public KPIResult GetKPI()
        {
            lock (_lock)
            {
                var totalSkus = _allSkus.Count;
                decimal stockValue = 0m;
                int outOfStock = 0;
                var now = DateTime.UtcNow.Date;
                double totalAgeDaysTimesQty = 0.0;
                long totalQtyForAge = 0;

                foreach (var sku in _allSkus)
                {
                    var lots = _lots.GetValueOrDefault(sku) ?? new List<Lot>();
                    var qty = lots.Sum(x => x.Quantity);
                    if (qty <= 0) outOfStock++;
                    foreach (var lot in lots)
                    {
                        stockValue += lot.UnitCost * lot.Quantity;
                        var age = (now - lot.PurchaseDate).TotalDays;
                        if (lot.Quantity > 0)
                        {
                            totalAgeDaysTimesQty += age * lot.Quantity;
                            totalQtyForAge += lot.Quantity;
                        }
                    }
                }

                double avgInventoryAge = totalQtyForAge > 0 ? totalAgeDaysTimesQty / totalQtyForAge : 0.0;

                // Average daily sales across days present in data
                double avgDailySales = 0.0;
                if (_dailySales.Count > 0)
                {
                    avgDailySales = _dailySales.Values.Average();
                }

                return new KPIResult
                {
                    TotalSKUs = totalSkus,
                    StockValue = stockValue,
                    OutOfStockItems = outOfStock,
                    AverageDailySales = (decimal)avgDailySales,
                    AverageInventoryAgeDays = avgInventoryAge
                };
            }
        }

        public class ProductSnapshot
        {
            public string ProductId { get; set; } = string.Empty;
            public int QuantityOnHand { get; set; }
            public decimal EstimatedValue { get; set; }
        }

        public ProductSnapshot[] GetInventorySnapshot()
        {
            lock (_lock)
            {
                var list = new System.Collections.Generic.List<ProductSnapshot>();
                foreach (var sku in _allSkus)
                {
                    var lots = _lots.GetValueOrDefault(sku) ?? new List<Lot>();
                    var qty = lots.Sum(x => x.Quantity);
                    var est = lots.Sum(x => x.Quantity * x.UnitCost);
                    list.Add(new ProductSnapshot { ProductId = sku, QuantityOnHand = qty, EstimatedValue = est });
                }
                return list.ToArray();
            }
        }
    }
}