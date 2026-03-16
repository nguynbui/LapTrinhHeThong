using System;
using System.IO;

namespace Services
{
    public class FileWatcherService : IDisposable
    {
        private readonly FileSystemWatcher _watcherInvoices;
        private readonly FileSystemWatcher _watcherPurchases;
        private readonly FileWorker _worker;

        public FileWatcherService(FileWorker worker, string invoicesDir, string purchasesDir)
        {
            _worker = worker;

            _watcherInvoices = new FileSystemWatcher(invoicesDir)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            _watcherInvoices.Created += OnCreated;
            _watcherInvoices.Renamed += OnRenamed;

            _watcherPurchases = new FileSystemWatcher(purchasesDir)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            _watcherPurchases.Created += OnCreated;
            _watcherPurchases.Renamed += OnRenamed;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            // Tạm dừng ngắn để chờ hoàn thành ghi file
            TaskDelayEnqueue(e.FullPath);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            TaskDelayEnqueue(e.FullPath);
        }

        private async void TaskDelayEnqueue(string path)
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(300);
                _worker.EnqueueFile(path);
            }
            catch { }
        }

        public void Dispose()
        {
            _watcherInvoices?.Dispose();
            _watcherPurchases?.Dispose();
        }
    }
}
