using System.Collections.Generic;
using ExitGames.Client.Photon;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Protection
{
    public sealed class PacketGate
    {
        private const byte RpcEvent = 200;
        private const byte SpawnEvent = 202;
        private const byte DestroyEvent = 204;
        private const byte OwnershipUpdateEvent = 212;
        private const byte ViewIdKey = 0;
        private const byte OwnershipRequestEvent = 209;
        private const byte OwnershipTransferEvent = 210;
        private const byte FirstServerEvent = 220;
        private const float FloodWindow = 1f;
        private const float FloodReportSeconds = 10f;
        private const string FloodTag = "floods the network";
        private const string TakeoverTag = "takes over other characters";
        private const string DestroyTag = "destroys other players' objects";

        private readonly Dictionary<int, (float Start, int Rpcs, int Spawns, int Objects, float Reported)> _rates = new Dictionary<int, (float Start, int Rpcs, int Spawns, int Objects, float Reported)>();
        private readonly SuspicionReport _report;
        private readonly DetectionSettings _detection;
        private readonly FloodSettings _flood;
        private readonly MuteList _mutes;

        public PacketGate(SuspicionReport report, DetectionSettings detection, FloodSettings flood, MuteList mutes)
        {
            _report = report;
            _detection = detection;
            _flood = flood;
            _mutes = mutes;
        }

        public bool Allow(EventData data)
        {
            int actor = data.Sender;

            if (actor <= 0 || data.Code >= FirstServerEvent || PhotonNetwork.CurrentRoom == null)
                return true;

            PhotonPlayer sender = PhotonNetwork.CurrentRoom.GetPlayer(actor);

            if (sender == null || sender.IsLocal)
                return true;

            if (_mutes.IsMuted(sender))
            {
                _mutes.CountDrop(sender);
                return false;
            }

            CountRate(sender, data.Code);

            if (data.Code == OwnershipRequestEvent || data.Code == OwnershipTransferEvent)
                return CheckTakeover(sender, data.CustomData as int[]);

            if (data.Code == DestroyEvent)
                return CheckDestroy(sender, data.CustomData as Hashtable);

            return true;
        }

        private void CountRate(PhotonPlayer sender, byte code)
        {
            bool objectEvent = code == DestroyEvent || code == OwnershipRequestEvent || code == OwnershipTransferEvent || code == OwnershipUpdateEvent;

            if (!_detection.Flood.Value || (code != RpcEvent && code != SpawnEvent && !objectEvent))
                return;

            float now = Time.unscaledTime;
            (float start, int rpcs, int spawns, int objects, float reported) = _rates.TryGetValue(sender.ActorNumber, out var rate) ? rate : (now, 0, 0, 0, -FloodReportSeconds);

            if (now - start > FloodWindow)
                (start, rpcs, spawns, objects) = (now, 0, 0, 0);

            if (code == SpawnEvent)
                spawns++;
            else if (objectEvent)
                objects++;
            else
                rpcs++;

            bool flooding = rpcs > _flood.MaxRpcPerSecond.Value || spawns > _flood.MaxSpawnsPerSecond.Value || objects > _flood.MaxObjectEventsPerSecond.Value;

            if (flooding && now - reported >= FloodReportSeconds)
            {
                reported = now;
                bool mute = _detection.MuteFlooders.Value;
                _report.Refuse(sender, FindingCategory.Cheats, FloodTag, $"floods the network: {rpcs} commands, {spawns} spawns and {objects} object changes in a second", mute);

                if (mute)
                    _mutes.Mute(sender, "flood");
            }

            _rates[sender.ActorNumber] = (start, rpcs, spawns, objects, reported);
        }

        private bool CheckDestroy(PhotonPlayer sender, Hashtable content)
        {
            if (!_detection.ObjectDestroy.Value || sender.IsMasterClient || content == null || !(content[ViewIdKey] is int viewId))
                return true;

            PhotonView view = PhotonView.Find(viewId);

            if (view == null || view.Owner == null || view.Owner.ActorNumber == sender.ActorNumber)
                return true;

            string what = view.TryGetComponent(out Character _) ? "the character" : $"an object ({view.name})";
            return _report.Refuse(sender, FindingCategory.Cheats, DestroyTag, $"destroyed {what} of {PlayerNames.Name(view.Owner)}", _detection.BlockObjectDestroy.Value);
        }

        private bool CheckTakeover(PhotonPlayer sender, int[] content)
        {
            if (!_detection.CharacterTakeover.Value || content == null || content.Length == 0)
                return true;

            PhotonView view = PhotonView.Find(content[0]);

            if (view == null || view.Owner == null || view.Owner.ActorNumber == sender.ActorNumber || !view.TryGetComponent(out Character _))
                return true;

            return _report.Refuse(sender, FindingCategory.Cheats, TakeoverTag, $"tried to take control of the character of {PlayerNames.Name(view.Owner)}", _detection.BlockCharacterTakeover.Value);
        }
    }
}