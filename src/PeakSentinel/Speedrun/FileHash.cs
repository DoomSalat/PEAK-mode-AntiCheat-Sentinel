using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace PeakSentinel.Speedrun
{
    public sealed class FileHash
    {
        private const string HashFormat = "x2";
        private const int ShortBytes = 6;

        public string Short(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return string.Empty;

            using (SHA256 sha = SHA256.Create())
            using (FileStream file = File.OpenRead(path))
                return string.Concat(sha.ComputeHash(file).Take(ShortBytes).Select(part => part.ToString(HashFormat)));
        }
    }
}
