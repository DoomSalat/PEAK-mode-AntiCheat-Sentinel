using System.Collections.Generic;
using ExitGames.Client.Photon;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using Photon.Pun;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Protection
{
    public sealed class MuteList
    {
        private const float Silent = 0f;
        private const string ActorKeyPrefix = "actor:";

        private readonly Dictionary<string, Muted> _muted = new Dictionary<string, Muted>();
        private readonly StateLedger _ledger;
        private readonly SuspicionReport _report;
        private string _room;

        public MuteList(SuspicionReport report, StateLedger ledger)
        {
            _ledger = ledger;
            _report = report;
        }

        public int Version { get; private set; }

        public bool IsMuted(PhotonPlayer player)
        {
            ForgetOldRoom();
            return player != null && _muted.ContainsKey(KeyOf(player));
        }

        public int DroppedFrom(PhotonPlayer player)
        {
            return player != null && _muted.TryGetValue(KeyOf(player), out Muted muted) ? muted.Dropped : 0;
        }

        public void Mute(PhotonPlayer player, string reason)
        {
            ForgetOldRoom();

            if (player == null || player.IsLocal || _muted.ContainsKey(KeyOf(player)))
                return;

            string userId = player.UserId;
            float level = string.IsNullOrEmpty(userId) ? Silent : AudioLevels.GetPlayerLevel(userId);
            _muted[KeyOf(player)] = new Muted(userId, level);

            if (!string.IsNullOrEmpty(userId))
                AudioLevels.SetPlayerLevel(userId, Silent);

            _report.Notice(player, FindingCategory.Other, $"muted: everything they send, voice included, is ignored on your side ({reason})");
            Version++;
        }

        public void Unmute(PhotonPlayer player)
        {
            if (player == null || !_muted.TryGetValue(KeyOf(player), out Muted muted))
                return;

            _muted.Remove(KeyOf(player));
            RestoreVoice(muted);
            int restored = _ledger.CatchUp(player);
            _report.Notice(player, FindingCategory.Other, $"unmuted: what they did while muted was caught up ({restored} objects and state updates: their ghost, objects they made or removed, statuses, death or revive)");
            Version++;
        }

        public void Drop(PhotonPlayer player, EventData data)
        {
            _muted[KeyOf(player)].Dropped++;

            if (data.Code == PunCodes.RpcEvent)
                _ledger.NoteMissed(player, data.CustomData as Hashtable);
            else if (data.Code == PunCodes.SpawnEvent)
                _ledger.NoteSpawn(player, data.CustomData as Hashtable);
            else if (data.Code == PunCodes.RemoveEvent)
                _ledger.NoteRemoval(player, data.CustomData as Hashtable);
        }

        private string KeyOf(PhotonPlayer player)
        {
            return string.IsNullOrEmpty(player.UserId) ? ActorKeyPrefix + player.ActorNumber : player.UserId;
        }

        private void ForgetOldRoom()
        {
            string room = PhotonNetwork.CurrentRoom == null ? null : PhotonNetwork.CurrentRoom.Name;

            if (room == _room)
                return;

            _room = room;

            foreach (Muted muted in _muted.Values)
                RestoreVoice(muted);

            _muted.Clear();
            Version++;
        }

        private void RestoreVoice(Muted muted)
        {
            if (!string.IsNullOrEmpty(muted.UserId))
                AudioLevels.SetPlayerLevel(muted.UserId, muted.Level);
        }

        private sealed class Muted
        {
            public Muted(string userId, float level)
            {
                UserId = userId;
                Level = level;
            }

            public string UserId { get; }
            public float Level { get; }
            public int Dropped { get; set; } // Set: counted by MuteList.CountDrop
        }
    }
}
