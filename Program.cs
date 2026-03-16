using System;
using System.IO;
using System.Threading.Tasks;
using Services;

class Program
{
	static async Task Main(string[] args)
	{
		Console.WriteLine("Starting KPI processor...");

		var baseDir = AppContext.BaseDirectory;
		var dataDir = Path.Combine(baseDir, "data");
		var invoicesDir = Path.Combine(dataDir, "invoices");
		var purchasesDir = Path.Combine(dataDir, "purchase-orders");
		Directory.CreateDirectory(invoicesDir);
		Directory.CreateDirectory(purchasesDir);

		var registryFile = Path.Combine(baseDir, "processed_files.json");
		var kpiOutFile = Path.Combine(baseDir, "kpi_output.json");

		var registry = new ProcessedFileRegistry(registryFile);
		var aggregates = new Aggregates();
		var writer = new KPIWriter(kpiOutFile);

		using var worker = new FileWorker(registry, aggregates, writer);
		using var watcher = new FileWatcherService(worker, invoicesDir, purchasesDir);

		Console.WriteLine("Watching folders:");
		Console.WriteLine(invoicesDir);
		Console.WriteLine(purchasesDir);

		// Xử lý các file có sẵn khi khởi động (từng bước)
		foreach (var f in Directory.GetFiles(invoicesDir)) worker.EnqueueFile(f);
		foreach (var f in Directory.GetFiles(purchasesDir)) worker.EnqueueFile(f);

		if (args != null && args.Length > 0 && args[0] == "--run-once")
		{
			// Chờ ngắn cho worker xử lý (ở đây 1 giây)
			await System.Threading.Tasks.Task.Delay(1000);
			await worker.StopAsync();
			var kpi = aggregates.GetKPI();
			var writerFile = kpiOutFile;
			Console.WriteLine("Run-once mode: KPI generated to " + writerFile);
			Console.WriteLine($"Total SKUs: {kpi.TotalSKUs}, StockValue: {kpi.StockValue:0.00}");
			return;
		}

		var ui = new ConsoleUI(aggregates, registry, worker);
		await ui.RunAsync();

		await worker.StopAsync();
		Console.WriteLine("Stopped.");
	}
}
