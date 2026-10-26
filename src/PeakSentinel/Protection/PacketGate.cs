using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using PeakSentinel.Detection.Flight;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Protection
{
    public sealed class PacketGate
    {
        private const float FloodWindow = 1f;
        private const float FloodReportSeconds = 10f;
        private const string FloodTag = "floods the network";
        private const string TakeoverTag = "takes over other characters";
        private const string DestroyTag = "destroys other players' objects";
        private const float JoinGraceSeconds = 15f;
        private const float CheckpointGraceSeconds = 20f;
        private const int TopKinds = 3;
        private const string SpawnKind = "spawn";
        private const string ObjectKind = "object change";
        private const string UnknownRpc = "unknown RPC";

        private static readonly HashSet<string> FrameSyncMethods = new HashSet<string> // Static: fixed RPCs the game sends every frame while a status or effect changes, not a flood when they come from the owner
        {
            RpcNames.SyncAfflictions, RpcNames.SyncStatuses, RpcNames.SyncThorns,
        };

        private readonly Dictionary<int, (float Start, int Rpcs, int Spawns, int Objects, int Streak, float Reported)> _rates = new Dictionary<int, (float Start, int Rpcs, int Spawns, int Objects, int Streak, float Reported)>();
        private readonly Dictionary<int, Dictionary<string, int>> _kinds = new Dictionary<int, Dictionary<string, int>>();
        private readonly SuspicionReport _report;
        private readonly DetectionSettings _detection;
        private readonly FloodSettings _flood;
        private readonly MuteList _mutes;
        private readonly LatencyMeter _latency;
        private readonly EnvironmentMarks _traps;
        private string _room;
        private Segment _segment;
        private float _calmAt;

        public PacketGate(SuspicionReport report, DetectionSettings detection, FloodSettings flood, MuteList mutes, LatencyMeter latency, EnvironmentMarks traps)
        {
            _traps = traps;
            _latency = latency;
            _report = report;
            _detection = detection;
            _flood = flood;
            _mutes = mutes;
        }

        public bool Allow(EventData data)
        {
            int actor = data.Sender;

            if (actor <= 0 || data.Code >= PunCodes.FirstServerEvent || PhotonNetwork.CurrentRoom == null)
                return true;

            PhotonPlayer sender = PhotonNetwork.CurrentRoom.GetPlayer(actor);

            if (sender == null || sender.IsLocal)
                return true;

            if (_mutes.IsMuted(sender))
            {
                _mutes.Drop(sender, data);
                return false;
            }

            if ((data.Code == PunCodes.SerializeEvent || data.Code == PunCodes.SerializeReliableEvent) && data.CustomData is object[] serialized && serialized.Length > 0 && serialized[0] is int sentAt)
                _latency.Note(actor, sentAt);

            if (_report.Spares(sender))
                return true;

            if (!sender.IsMasterClient && !InGracePeriod() && !IsCached(data) && !IsFrameSync(data, sender))
                CountRate(sender, data);

            if (data.Code == PunCodes.OwnershipRequestEvent || data.Code == PunCodes.OwnershipTransferEvent)
                return CheckTakeover(sender, data.CustomData as int[]);

            if (data.Code == PunCodes.RemoveEvent)
                return CheckDestroy(sender, data.CustomData as Hashtable);

            return true;
        }

        private bool InGracePeriod()
        {
            string room = PhotonNetwork.CurrentRoom.Name;

            if (room != _room)
            {
                _room = room;
                _calmAt = Time.unscaledTime + JoinGraceSeconds;
            }

            MapHandler map = Singleton<MapHandler>.Instance;

            if (map != null && map.GetCurrentSegment() != _segment)
            {
                _segment = map.GetCurrentSegment();
                _calmAt = Mathf.Max(_calmAt, Time.unscaledTime + CheckpointGraceSeconds);
            }

            return Time.unscaledTime < _calmAt;
        }

        private bool IsFrameSync(EventData data, PhotonPlayer sender)
        {
            if (data.Code != PunCodes.RpcEvent || !(data.CustomData is Hashtable rpc))
                return false;

            string method = RpcRouter.MethodOf(rpc) ?? string.Empty;
            return ((FrameSyncMethods.Contains(method) || method == RpcNames.ForceAtPosition) && IsOwnView(rpc, sender)) || (method == RpcNames.ForceToBodyPart && IsTrapped(rpc));
        }

        private bool IsTrapped(Hashtable rpc)
        {
            PhotonView view = rpc[PunCodes.ViewIdKey] is int viewId ? PhotonView.Find(viewId) : null;
            return view != null && view.TryGetComponent(out Character character) && _traps.CauseOf(character) != null;
        }

        private bool IsOwnView(Hashtable rpc, PhotonPlayer sender)
        {
            PhotonView view = rpc[PunCodes.ViewIdKey] is int viewId ? PhotonView.Find(viewId) : null;
            return view != null && view.OwnerActorNr == sender.ActorNumber;
        }

        private bool IsCached(EventData data)
        {
            if (!(data.CustomData is Hashtable content))
                return false;

            object sentAt = data.Code == PunCodes.SpawnEvent ? content[PunCodes.SpawnTimestampKey] : content[PunCodes.RpcTimestampKey];
            return sentAt is int timestamp && unchecked(PhotonNetwork.ServerTimestamp - timestamp) > PunCodes.CachedAgeMilliseconds;
        }

        private void CountRate(PhotonPlayer sender, EventData data)
        {
            byte code = data.Code;
            bool objectEvent = code == PunCodes.RemoveEvent || code == PunCodes.OwnershipRequestEvent || code == PunCodes.OwnershipTransferEvent || code == PunCodes.OwnershipUpdateEvent;

            if (!_detection.Flood.Value || (code != PunCodes.RpcEvent && code != PunCodes.SpawnEvent && !objectEvent))
                return;

            float now = Time.unscaledTime;
            (float start, int rpcs, int spawns, int objects, int streak, float reported) = _rates.TryGetValue(sender.ActorNumber, out var rate) ? rate : (now, 0, 0, 0, 0, -FloodReportSeconds);

            if (now - start > FloodWindow)
            {
                bool over = rpcs > _flood.MaxRpcPerSecond.Value || spawns > _flood.MaxSpawnsPerSecond.Value || objects > _flood.MaxObjectEventsPerSecond.Value;
                streak = over && now - start < FloodWindow * 2f ? streak + 1 : 0;

                if (streak >= _flood.FloodSeconds.Value && now - reported >= FloodReportSeconds)
                {
                    reported = now;
                    ReportFlood(sender, rpcs, spawns, objects, streak, MostSent(sender.ActorNumber));
                }

                (start, rpcs, spawns, objects) = (now, 0, 0, 0);
                KindsOf(sender.ActorNumber).Clear();
            }

            if (code == PunCodes.SpawnEvent)
                spawns++;
            else if (objectEvent)
                objects++;
            else
                rpcs++;

            _rates[sender.ActorNumber] = (start, rpcs, spawns, objects, streak, reported);
            Dictionary<string, int> kinds = KindsOf(sender.ActorNumber);
            string kind = code == PunCodes.SpawnEvent ? SpawnKind : objectEvent ? ObjectKind : (data.CustomData is Hashtable rpc ? RpcRouter.MethodOf(rpc) : null) ?? UnknownRpc;
            kinds[kind] = kinds.TryGetValue(kind, out int sent) ? sent + 1 : 1;
        }

        private Dictionary<string, int> KindsOf(int actor)
        {
            if (!_kinds.TryGetValue(actor, out Dictionary<string, int> kinds))
            {
                kinds = new Dictionary<string, int>();
                _kinds[actor] = kinds;
            }

            return kinds;
        }

        private string MostSent(int actor)
        {
            return string.Join(", ", KindsOf(actor).OrderByDescending(pair => pair.Value).Take(TopKinds).Select(pair => $"{pair.Key} x{pair.Value}"));
        }

        private void ReportFlood(PhotonPlayer sender, int rpcs, int spawns, int objects, int seconds, string mostSent)
        {
            bool mute = _detection.MuteFlooders.Value;
            _report.Tag(sender, FloodTag);
            _report.Suspect(sender, FindingCategory.Cheats, $"floods the network for {seconds} s in a row: {rpcs} commands, {spawns} spawns and {objects} object changes in the last second (most: {mostSent})", mute);

            if (mute)
                _mutes.Mute(sender, "flood");
        }
        private bool CheckDestroy(PhotonPlayer sender, Hashtable content)
        {
            if (!_detection.ObjectDestroy.Value || sender.IsMasterClient || content == null || !(content[PunCodes.ViewIdKey] is int viewId))
                return true;

            PhotonView view = PhotonView.Find(viewId);

            if (view == null || view.Owner == null || view.Owner.ActorNumber == sender.ActorNumber)
                return true;

            string what = view.TryGetComponent(out Character _) ? "destroyed the character" : view.TryGetComponent(out TiedBalloon _) ? "popped a balloon" : $"destroyed an object ({view.name})";
            return _report.Refuse(sender, FindingCategory.Cheats, DestroyTag, $"{what} of {PlayerNames.Name(view.Owner)}", _detection.BlockObjectDestroy.Value);
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