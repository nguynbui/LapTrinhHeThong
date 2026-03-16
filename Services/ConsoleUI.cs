using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;

namespace Services
{
    public class ConsoleUI
    {
        private readonly Aggregates _aggregates;
        private readonly ProcessedFileRegistry _registry;
        private readonly FileWorker _worker;

        public ConsoleUI(Aggregates aggregates, ProcessedFileRegistry registry, FileWorker worker)
        {
            _aggregates = aggregates;
            _registry = registry;
            _worker = worker;
        }

        public async Task RunAsync()
        {
            while (true)
            {
                Console.Clear();
                RenderHeader();
                RenderKPIDashboard();
                RenderMenu();
                Console.Write("Select option: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;
                input = input.Trim().ToLowerInvariant();
                if (input == "q" || input == "quit" || input == "6")
                {
                    break;
                }

                switch (input)
                {
                    case "1":
                        ShowCurrentKPIs();
                        break;
                    case "2":
                        await ShowProductKPIsAsync();
                        break;
                    case "3":
                        ShowSystemStatus();
                        break;
                    case "4":
                        await GenerateReportAsync();
                        break;
                    case "5":
                        ShowConfiguration();
                        break;
                    default:
                        Console.WriteLine("Unknown option.");
                        break;
                }

                Console.WriteLine("\nPress Enter to continue...");
                Console.ReadLine();
            }
        }

        private void RenderHeader()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("          INVENTORY KPI DASHBOARD       ");
            Console.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("========================================\n");
        }

        private void RenderKPIDashboard()
        {
            var kpi = _aggregates.GetKPI();
            var sb = new StringBuilder();
            sb.AppendLine("KPI\t\t\tValue");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($"Total SKUs:\t\t{kpi.TotalSKUs}");
            sb.AppendLine($"Cost of Inventory:\t${kpi.StockValue:0.00}");
            sb.AppendLine($"Out-of-Stock Items:\t{kpi.OutOfStockItems}");
            sb.AppendLine($"Average Daily Sales:\t{kpi.AverageDailySales:0.##}");
            sb.AppendLine($"Average Inventory Age (days):\t{kpi.AverageInventoryAgeDays:0.##}");
            Console.WriteLine(sb.ToString());
        }

        private void RenderMenu()
        {
            Console.WriteLine("MAIN MENU");
            Console.WriteLine("1. View Current KPIs");
            Console.WriteLine("2. View Product KPIs");
            Console.WriteLine("3. View System Status");
            Console.WriteLine("4. Generate Report");
            Console.WriteLine("5. Configuration");
            Console.WriteLine("6. Quit");
        }

        private void ShowCurrentKPIs()
        {
            Console.WriteLine();
            RenderKPIDashboard();
        }

        private async Task ShowProductKPIsAsync()
        {
            Console.WriteLine();
            Console.WriteLine("Product | Qty on hand | Estimated Value");
            Console.WriteLine("----------------------------------------");
            var snaps = _aggregates.GetInventorySnapshot();
            if (snaps == null || snaps.Length == 0)
            {
                Console.WriteLine("No product data available.");
                return;
            }
            Console.WriteLine(string.Format("{0,-12} | {1,10} | {2,15}", "Product", "Qty", "Est Value"));
            Console.WriteLine("------------------------------------------------");
            foreach (var s in snaps.OrderBy(x => x.ProductId))
            {
                Console.WriteLine(string.Format("{0,-12} | {1,10} | {2,15:C}", s.ProductId, s.QuantityOnHand, s.EstimatedValue));
            }
            await Task.CompletedTask;
        }

        private void ShowSystemStatus()
        {
            Console.WriteLine();
            Console.WriteLine($"Processed files: {_registry.ProcessedCount}");
            Console.WriteLine("Worker queue: (not exposed in demo)");
        }

        private async Task GenerateReportAsync()
        {
            Console.WriteLine();
            var kpi = _aggregates.GetKPI();
            var report = new System.Text.StringBuilder();
            report.AppendLine("INVENTORY KPI REPORT");
            report.AppendLine($"Generated: {DateTime.UtcNow:O}");
            report.AppendLine();
            report.AppendLine($"Total SKUs: {kpi.TotalSKUs}");
            report.AppendLine($"Cost of Inventory: ${kpi.StockValue:0.00}");
            report.AppendLine($"Out-of-Stock Items: {kpi.OutOfStockItems}");
            report.AppendLine($"Average Daily Sales: {kpi.AverageDailySales:0.##}");
            report.AppendLine($"Average Inventory Age (days): {kpi.AverageInventoryAgeDays:0.##}");

            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "kpi_report.txt");
            await System.IO.File.WriteAllTextAsync(path, report.ToString());
            Console.WriteLine($"Report generated: {path}");
        }

        private void ShowConfiguration()
        {
            Console.WriteLine();
            Console.WriteLine($"Base dir: {AppContext.BaseDirectory}");
            Console.WriteLine("Data folders: data/invoices/, data/purchase-orders/");
            Console.WriteLine("Processed registry: processed_files.json");
        }
    }
}
