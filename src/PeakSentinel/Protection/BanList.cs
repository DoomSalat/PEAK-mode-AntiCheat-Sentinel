using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PeakSentinel.Protection
{
    public sealed class BanList
    {
        private const char Separator = '\t';

        private static readonly char[] LineBreaks = { '\r', '\n' }; // Static: fixed line separators for imported lists

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

        public string Export()
        {
            return string.Join("\n", Lines());
        }

        public int Import(string text)
        {
            int added = 0;

            foreach (string line in (text ?? string.Empty).Split(LineBreaks, System.StringSplitOptions.RemoveEmptyEntries))
            {
                (string userId, string name) = Parse(line.Trim());

                if (userId.Length == 0 || _names.ContainsKey(userId))
                    continue;

                _names[userId] = name;
                added++;
            }

            if (added > 0)
                Save();

            return added;
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
                (string userId, string name) = Parse(line);

                if (userId.Length > 0)
                    _names[userId] = name;
            }
        }

        private (string UserId, string Name) Parse(string line)
        {
            string[] parts = line.Split(new[] { Separator }, 2);
            return (parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : parts[0].Trim());
        }

        private IEnumerable<string> Lines()
        {
            return _names.Select(entry => entry.Key + Separator + entry.Value);
        }

        private void Save()
        {
            File.WriteAllLines(_file, Lines());
        }
    }
}
