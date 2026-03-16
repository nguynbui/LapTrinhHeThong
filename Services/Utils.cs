using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Services
{
    public static class Utils
    {
        public static string ComputeChecksum(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            var hash = sha.ComputeHash(stream);
            var sb = new StringBuilder();
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
