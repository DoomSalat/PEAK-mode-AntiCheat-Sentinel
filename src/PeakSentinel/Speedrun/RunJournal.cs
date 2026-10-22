using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Configuration;

namespace PeakSentinel.Speedrun
{
    public sealed class RunJournal
    {
        private const string FileFormat = "PeakSentinel-speedrun-{0:yyyyMMdd}.log";
        private const string LineFormat = "{0:HH:mm:ss}\t{1}\t{2}";
        private const string AttemptFormat = "attempt {0}: {1}";
        private const string StartKind = "START";
        private const string ChestKind = "CHEST";
        private const string AlertKind = "ALERT";
        private const string Genesis = "GENESIS";
        private const string HashFormat = "x2";
        private const int HashBytes = 8;
        private const int RunIdLength = 8;
        private const char Separator = '\t';

        private readonly string _folder;
        private readonly ConfigEntry<bool> _enabled;
        private DateTime _day;
        private string _path;
        private string _last = Genesis;

        public RunJournal(string folder, ConfigEntry<bool> enabled)
        {
            _folder = folder;
            _enabled = enabled;
        }

        public string Path => _path ?? string.Empty;
        public int Attempt { get; private set; }
        public string RunId { get; private set; } = string.Empty;
        public DateTime AttemptStart { get; private set; } = DateTime.Now;
        public int Version { get; private set; }

        public void StartAttempt(string details)
        {
            AttemptStart = DateTime.Now;

            if (!_enabled.Value)
                return;

            OpenDay();
            Attempt++;
            RunId = Write(StartKind, string.Format(AttemptFormat, Attempt, details)).Substring(0, RunIdLength).ToUpperInvariant();
            Version++;
        }

        public void Chest(string text)
        {
            Write(ChestKind, text);
        }

        public void Alert(string text)
        {
            Write(AlertKind, text);
        }

        private string Write(string kind, string text)
        {
            if (!_enabled.Value)
                return string.Empty;

            OpenDay();
            string body = string.Format(LineFormat, DateTime.Now, kind, Clean(text));
            string hash = Chain(_last, body);
            File.AppendAllText(_path, body + Separator + hash + Environment.NewLine);
            _last = hash;
            return hash;
        }

        private void OpenDay()
        {
            if (_path != null && DateTime.Now.Date == _day)
                return;

            _day = DateTime.Now.Date;
            _path = System.IO.Path.Combine(_folder, string.Format(FileFormat, _day));
            Attempt = 0;
            _last = Genesis;

            if (!File.Exists(_path))
                return;

            string[] lines = File.ReadAllLines(_path).Where(line => line.Length > 0).ToArray();
            Attempt = lines.Count(line => line.Split(Separator).ElementAtOrDefault(1) == StartKind);

            if (lines.Length > 0)
                _last = lines[lines.Length - 1].Split(Separator).Last();
        }

        private string Chain(string previous, string body)
        {
            using (SHA256 sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(previous + body)).Take(HashBytes).Select(part => part.ToString(HashFormat)));
        }

        private string Clean(string text)
        {
            return text.Replace(Separator, ' ').Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
