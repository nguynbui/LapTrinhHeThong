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

            try
            {
                var text = await File.ReadAllTextAsync(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // Ép toàn bộ JSON vào cái khuôn XeroRoot
                var rootData = JsonSerializer.Deserialize<XeroRoot>(text, options);

                if (rootData != null && rootData.Invoices != null)
                {
                    foreach (var inv in rootData.Invoices)
                    {
                        // Lấy ngày tháng
                        DateTime date = DateTime.UtcNow;
                        if (DateTime.TryParse(inv.DateString, out var parsedDate)) date = parsedDate;

                        if (inv.LineItems == null) continue;

                        foreach (var line in inv.LineItems)
                        {
                            if (line.Quantity <= 0) continue;

                            string productId = line.ItemCode;
                            if (string.IsNullOrWhiteSpace(productId))
                            {
                                continue; // Đá văng ngay lập tức các dòng rác
                            }

                            if (inv.Type == "ACCPAY") // ACCPAY = Hóa đơn đầu vào (Mua hàng)
                            {
                                var po = new Models.PurchaseOrder
                                {
                                    ProductId = productId,
                                    QuantityPurchased = (int)Math.Round(line.Quantity),
                                    UnitCost = line.UnitAmount,
                                    PurchaseDate = date
                                };
                                _aggregates.ApplyPurchase(po);
                            }
                            else if (inv.Type == "ACCREC") // ACCREC = Hóa đơn đầu ra (Bán hàng)
                            {
                                var invoice = new Models.Invoice
                                {
                                    ProductId = productId,
                                    QuantitySold = (int)Math.Round(line.Quantity),
                                    UnitSellingPrice = line.UnitAmount,
                                    InvoiceDate = date
                                };
                                _aggregates.ApplyInvoice(invoice);
                            }
                        }
                    }
                }
                _registry.MarkProcessed(fileName, checksum);
                var kpi = _aggregates.GetKPI();
                _writer.Write(kpi);
                Console.WriteLine($"Processed {fileName} and updated KPI.");
            }
            catch (Exception ex)
            {
                Logger.LogError("FileWorker Loop", "Unexpected error", ex);
            }
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