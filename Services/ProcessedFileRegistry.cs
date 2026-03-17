using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Services
{
    public class ProcessedFileEntry
    {
        public string FileName { get; set; } = string.Empty;
        public string Checksum { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }

    public class ProcessedFileRegistry
    {
        private readonly string _path;
        private readonly List<ProcessedFileEntry> _entries = new();
        private readonly object _lock = new();

        public ProcessedFileRegistry(string path)
        {
            _path = path;
            if (File.Exists(_path))
            {
                try
                {
                    var text = File.ReadAllText(_path);
                    var arr = JsonSerializer.Deserialize<List<ProcessedFileEntry>>(text);
                    if (arr != null) _entries.AddRange(arr);
                }
                catch { }
            }
        }

        public int ProcessedCount
        {
            get { lock (_lock) { return _entries.Count; } }
        }
        public bool IsProcessed(string fileName, string checksum)
        {
            lock (_lock)
            {
                foreach (var e in _entries)
                    if (e.FileName == fileName && e.Checksum == checksum) return true;
                return false;
            }
        }

        public void MarkProcessed(string fileName, string checksum)
        {
            lock (_lock)
            {
                _entries.Add(new ProcessedFileEntry { FileName = fileName, Checksum = checksum, ProcessedAt = DateTime.UtcNow });
                try
                {
                    var text = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_path, text);
                }
                catch { }
            }
        }
    }
}
