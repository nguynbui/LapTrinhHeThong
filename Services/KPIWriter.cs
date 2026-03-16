using System;
using System.IO;
using System.Text.Json;
using Models;

namespace Services
{
    public class KPIWriter
    {
        private readonly string _path;
        public KPIWriter(string path) { _path = path; }
        public void Write(KPIResult result)
        {
            try
            {
                result.GeneratedAt = DateTime.UtcNow;
                var txt = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_path, txt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write KPI file: {ex.Message}");
            }
        }
    }
}
