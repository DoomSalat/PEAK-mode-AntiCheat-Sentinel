using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PeakSentinel.Moderation
{
    public sealed class BanList
    {
        private const char Separator = '\t';

        private readonly Dictionary<string, string> _names = new Dictionary<string, string>();
        private readonly string _file;

        public BanList(string file)
        {
            _file = file;
            Load();
        }

        public IEnumerable<KeyValuePair<string, string>> Entries => _names.OrderBy(entry => entry.Value);
        public int Count => _names.Count;

        public bool Contains(string userId)
        {
            return !string.IsNullOrEmpty(userId) && _names.ContainsKey(userId);
        }

        public void Ban(string userId, string name)
        {
            _names[userId] = name;
            Save();
        }

        public void Unban(string userId)
        {
            if (_names.Remove(userId))
                Save();
        }

        private void Load()
        {
            if (!File.Exists(_file))
                return;

            foreach (string line in File.ReadAllLines(_file))
            {
                string[] parts = line.Split(new[] { Separator }, 2);

                if (parts[0].Length > 0)
                    _names[parts[0]] = parts.Length > 1 ? parts[1] : parts[0];
            }
        }

        private void Save()
        {
            File.WriteAllLines(_file, _names.Select(entry => entry.Key + Separator + entry.Value));
        }
    }
}
