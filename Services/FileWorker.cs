using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Models;

namespace Services
{
    public class FileWorker : IDisposable
    {
        private readonly ProcessedFileRegistry _registry;
        private readonly Aggregates _aggregates;
        private readonly KPIWriter _writer;
        private readonly BlockingCollection<string> _queue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _workerTask;

        public FileWorker(ProcessedFileRegistry registry, Aggregates aggregates, KPIWriter writer)
        {
            _registry = registry;
            _aggregates = aggregates;
            _writer = writer;
            _workerTask = Task.Run(ProcessLoop);
        }

        public void EnqueueFile(string path)
        {
            _queue.Add(path);
        }

        private async Task ProcessLoop()
        {
            foreach (var path in _queue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    await ProcessFileWithRetry(path);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error processing {path}: {ex.Message}");
                }
            }
        }

        private async Task ProcessFileWithRetry(string path)
        {
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await ProcessFile(path);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {path} attempt {attempt}: {ex.Message}");
                    await Task.Delay(500 * attempt);
                }
            }
            Console.WriteLine($"Failed to process {path} after retries.");
        }

        private async Task ProcessFile(string path)
        {
            if (!File.Exists(path)) return;
            var checksum = Utils.ComputeChecksum(path);
            var fileName = Path.GetFileName(path);
            if (_registry.IsProcessed(fileName, checksum))
            {
                Console.WriteLine($"Skipping already processed file {fileName}");
                return;
            }

            var text = await File.ReadAllTextAsync(path);

            // Heuristics: tên thư mục chỉ ra purchase hoặc invoice
            var lower = path.ToLowerInvariant();
            if (lower.Contains("purchase"))
            {
                var items = JsonSerializer.Deserialize<List<Models.PurchaseOrder>>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (items != null)
                {
                    foreach (var po in items) _aggregates.ApplyPurchase(po);
                }
            }
            else if (lower.Contains("invoice"))
            {
                var items = JsonSerializer.Deserialize<List<Models.Invoice>>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (items != null)
                {
                    foreach (var inv in items) _aggregates.ApplyInvoice(inv);
                }
            }
            else
            {
                // Nếu không rõ loại theo tên thư mục, cố gắng nhận diện từ nội dung JSON
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                {
                    var first = doc.RootElement[0];
                    if (first.TryGetProperty("orderId", out _))
                    {
                        var items = JsonSerializer.Deserialize<List<Models.PurchaseOrder>>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (items != null) foreach (var po in items) _aggregates.ApplyPurchase(po);
                    }
                    else if (first.TryGetProperty("invoiceId", out _))
                    {
                        var items = JsonSerializer.Deserialize<List<Models.Invoice>>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (items != null) foreach (var inv in items) _aggregates.ApplyInvoice(inv);
                    }
                }
            }

            _registry.MarkProcessed(fileName, checksum);
            var kpi = _aggregates.GetKPI();
            _writer.Write(kpi);
            Console.WriteLine($"Processed {fileName} and updated KPI.");
        }

        public async Task StopAsync()
        {
            _queue.CompleteAdding();
            _cts.CancelAfter(TimeSpan.FromSeconds(5));
            try { await _workerTask; } catch { }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _queue.Dispose();
            _cts.Dispose();
        }
    }
}
