using System;
using System.IO;
using System.Text;
using BepInEx.Configuration;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Reporting
{
    public sealed class DebugTrace : IDisposable
    {
        private const string TimeFormat = "HH:mm:ss.fff";
        private const string Separator = ", ";
        private const string Null = "null";

        private readonly ConfigEntry<bool> _enabled;
        private readonly ConfigEntry<bool> _allRpcs;
        private readonly StreamWriter _writer;

        public DebugTrace(string path, ConfigEntry<bool> enabled, ConfigEntry<bool> allRpcs)
        {
            _enabled = enabled;
            _allRpcs = allRpcs;
            _writer = new StreamWriter(path, false, Encoding.UTF8) { AutoFlush = true };
            Path = path;
            Write($"trace started, game time {Time.unscaledTime:F1}");
        }

        public string Path { get; }
        public bool TracesAllRpcs => _enabled.Value && _allRpcs.Value;

        public void Rpc(string method, PhotonPlayer sender, int viewId, PhotonPlayer viewOwner, object[] arguments)
        {
            Write($"RPC {method} from {Describe(sender)} on view {viewId} owned by {Describe(viewOwner)} args [{Join(arguments)}]");
        }

        public void Instantiate(PhotonPlayer creator, string prefab, Vector3 position)
        {
            Write($"INSTANTIATE {prefab} by {Describe(creator)} at {position}");
        }

        public void Flight(PhotonPlayer player, string numbers)
        {
            Write($"FLIGHT {Describe(player)}: {numbers}");
        }

        public void Finding(string level, PhotonPlayer culprit, string action)
        {
            Write($"{level} {Describe(culprit)}: {action}");
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        private void Write(string line)
        {
            if (_enabled.Value)
                _writer.WriteLine($"[{DateTime.Now.ToString(TimeFormat)}] {line}");
        }

        private string Describe(PhotonPlayer player)
        {
            if (player == null)
                return Null;

            string host = player.IsMasterClient ? " host" : string.Empty;
            return $"{SuspicionReport.NameOf(player)}#{player.ActorNumber}{host}";
        }

        private string Join(object[] arguments)
        {
            if (arguments == null)
                return string.Empty;

            StringBuilder text = new StringBuilder();

            for (int i = 0; i < arguments.Length; i++)
            {
                if (i > 0)
                    text.Append(Separator);

                text.Append(arguments[i] == null ? Null : arguments[i].ToString());
            }

            return text.ToString();
        }
    }
}
