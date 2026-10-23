using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PeakSentinel.Protection
{
    public sealed class BanList
    {
        private const char Separator = '\t';
        private const string DateFormat = "yyyy-MM-dd HH:mm";

        private static readonly char[] LineBreaks = { '\r', '\n' }; // Static: fixed line separators for imported lists

        private readonly Dictionary<string, BanEntry> _entries = new Dictionary<string, BanEntry>();
        private readonly string _file;

        public BanList(string file)
        {
            _file = file;
            Load();
        }

        public IEnumerable<BanEntry> Entries => _entries.Values.OrderBy(entry => entry.Name);
        public int Count => _entries.Count;

        public bool Contains(string userId)
        {
            return !string.IsNullOrEmpty(userId) && _entries.ContainsKey(userId);
        }

        public void Ban(string userId, string name)
        {
            _entries[userId] = new BanEntry(userId, name, DateTime.Now);
            Save();
        }

        public string Export()
        {
            return string.Join("\n", Lines());
        }

        public int Import(string text)
        {
            int added = 0;

            foreach (string line in (text ?? string.Empty).Split(LineBreaks, StringSplitOptions.RemoveEmptyEntries))
            {
                BanEntry entry = Parse(line.Trim());

                if (entry.UserId.Length == 0 || _entries.ContainsKey(entry.UserId))
                    continue;

                _entries[entry.UserId] = entry;
                added++;
            }

            if (added > 0)
                Save();

            return added;
        }

        public void Unban(string userId)
        {
            if (_entries.Remove(userId))
                Save();
        }

        public string DateOf(BanEntry entry)
        {
            return entry.BannedAt.HasValue ? entry.BannedAt.Value.ToString(DateFormat, CultureInfo.InvariantCulture) : null;
        }

        private void Load()
        {
            if (!File.Exists(_file))
                return;

            foreach (string line in File.ReadAllLines(_file))
            {
                BanEntry entry = Parse(line);

                if (entry.UserId.Length > 0)
                    _entries[entry.UserId] = entry;
            }
        }

        private BanEntry Parse(string line)
        {
            string[] parts = line.Split(Separator);
            string userId = parts[0].Trim();
            int nameParts = parts.Length - 1;
            DateTime? bannedAt = null;

            if (parts.Length > 2 && DateTime.TryParseExact(parts[parts.Length - 1].Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                bannedAt = date;
                nameParts--;
            }

            string name = nameParts > 0 ? string.Join(Separator.ToString(), parts, 1, nameParts).Trim() : userId;
            return new BanEntry(userId, name, bannedAt);
        }

        private IEnumerable<string> Lines()
        {
            return _entries.Values.Select(entry => entry.BannedAt.HasValue ? entry.UserId + Separator + entry.Name + Separator + DateOf(entry) : entry.UserId + Separator + entry.Name);
        }

        private void Save()
        {
            File.WriteAllLines(_file, Lines());
        }
    }
}
