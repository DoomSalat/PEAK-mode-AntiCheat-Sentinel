using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Network
{
    public sealed class StateLedger
    {

        private static readonly HashSet<string> SnapshotMethods = new HashSet<string> // Static: fixed RPCs that carry a full snapshot of a player's state, safe to apply again
        {
            RpcNames.SyncStatuses, RpcNames.SyncAfflictions, RpcNames.SyncPetrify, RpcNames.SyncInventory, RpcNames.EquipSlot, RpcNames.Crouch,
            RpcNames.SyncThorns, RpcNames.SetBlockedState,
        };

        private static readonly HashSet<string> EventMethods = new HashSet<string> // Static: fixed RPCs that change a player's state once, replayed only when missed under mute
        {
            RpcNames.Die, RpcNames.SetDead, RpcNames.ReviveAtPosition, RpcNames.PassOut, RpcNames.UnPassOut, RpcNames.UnFall,
            RpcNames.Zombify, RpcNames.ShownDead, RpcNames.ShownPassedOut, RpcNames.InitGhost, RpcNames.GhostTarget,
        };

        private readonly Dictionary<int, List<Entry>> _entries = new Dictionary<int, List<Entry>>();
        private readonly Dictionary<int, List<Entry>> _missed = new Dictionary<int, List<Entry>>();
        private readonly Dictionary<int, List<(int Id, Hashtable Spawn)>> _spawns = new Dictionary<int, List<(int Id, Hashtable Spawn)>>();
        private readonly Dictionary<int, HashSet<int>> _removals = new Dictionary<int, HashSet<int>>();
        private readonly ForeignObjects _objects;
        private string _room;
        private bool _replaying;

        public StateLedger(ForeignObjects objects)
        {
            _objects = objects;
        }

        public void Note(PhotonPlayer sender, Hashtable rpc)
        {
            Record(sender, rpc, false);
        }

        public void NoteMissed(PhotonPlayer sender, Hashtable rpc)
        {
            Record(sender, rpc, true);
        }

        public void NoteSpawn(PhotonPlayer creator, Hashtable spawn)
        {
            if (creator != null && spawn != null && spawn[PunCodes.InstantiationIdKey] is int id)
                ListOf(_spawns, creator.ActorNumber).Add((id, Copy(spawn)));
        }

        public void NoteRemoval(PhotonPlayer sender, Hashtable removal)
        {
            if (sender == null || removal == null || !(removal[PunCodes.ViewIdKey] is int viewId))
                return;

            List<(int Id, Hashtable Spawn)> spawns = ListOf(_spawns, sender.ActorNumber);

            if (spawns.RemoveAll(spawn => spawn.Id == viewId) > 0)
                return;

            if (!_removals.TryGetValue(sender.ActorNumber, out HashSet<int> removals))
            {
                removals = new HashSet<int>();
                _removals[sender.ActorNumber] = removals;
            }

            removals.Add(viewId);
        }

        public int CatchUp(PhotonPlayer player)
        {
            ForgetOldRoom();

            if (player == null || PhotonNetwork.CurrentRoom == null)
                return 0;

            int replayed = 0;
            _replaying = true;

            try
            {
                replayed += ReplayRemovals(player);
                replayed += ReplaySpawns(player);

                if (_missed.TryGetValue(player.ActorNumber, out List<Entry> missed))
                {
                    _missed.Remove(player.ActorNumber);
                    replayed += Apply(missed);
                }

                replayed += Reapply(player);
            }
            finally
            {
                _replaying = false;
            }

            return replayed;
        }

        public int Reapply(PhotonPlayer player)
        {
            ForgetOldRoom();

            if (player == null || PhotonNetwork.CurrentRoom == null || !_entries.TryGetValue(player.ActorNumber, out List<Entry> entries))
                return 0;

            bool replaying = _replaying;
            _replaying = true;

            try
            {
                return Apply(entries);
            }
            finally
            {
                _replaying = replaying;
            }
        }

        private void Record(PhotonPlayer sender, Hashtable rpc, bool missed)
        {
            if (_replaying || sender == null || rpc == null || !(rpc[PunCodes.ViewIdKey] is int viewId))
                return;

            string method = RpcRouter.MethodOf(rpc);
            bool snapshot = method != null && SnapshotMethods.Contains(method);

            if (!snapshot && !(missed && method != null && EventMethods.Contains(method)))
                return;

            int owner = OwnerOf(viewId);

            if (owner <= 0 || (sender.ActorNumber != owner && !sender.IsMasterClient))
                return;

            List<Entry> entries = ListOf(snapshot ? _entries : _missed, owner);
            entries.RemoveAll(entry => entry.ViewId == viewId && entry.Method == method);
            entries.Add(new Entry(viewId, method, sender.ActorNumber, Copy(rpc)));
        }

        private int ReplayRemovals(PhotonPlayer player)
        {
            if (!_removals.TryGetValue(player.ActorNumber, out HashSet<int> removals))
                return 0;

            _removals.Remove(player.ActorNumber);
            int removed = 0;

            foreach (int viewId in removals)
            {
                PhotonView view = PhotonView.Find(viewId);

                if (view == null || view.Owner == null || view.Owner.ActorNumber != player.ActorNumber)
                    continue;

                _objects.RemoveLocal(view.gameObject);
                removed++;
            }

            return removed;
        }

        private int ReplaySpawns(PhotonPlayer player)
        {
            if (!_spawns.TryGetValue(player.ActorNumber, out List<(int Id, Hashtable Spawn)> spawns))
                return 0;

            _spawns.Remove(player.ActorNumber);
            int spawned = 0;

            foreach ((int id, Hashtable spawn) in spawns)
            {
                if (PhotonView.Find(id) != null)
                    continue;

                PhotonNetwork.NetworkInstantiate(spawn, player);
                spawned++;
            }

            return spawned;
        }

        private int Apply(List<Entry> entries)
        {
            int replayed = 0;

            foreach (Entry entry in entries.ToArray())
            {
                PhotonPlayer sender = PhotonNetwork.CurrentRoom.GetPlayer(entry.SenderActor);

                if (sender == null || PhotonView.Find(entry.ViewId) == null)
                    continue;

                PhotonNetwork.ExecuteRpc(Copy(entry.Rpc), sender);
                replayed++;
            }

            return replayed;
        }

        private int OwnerOf(int viewId)
        {
            PhotonView view = PhotonView.Find(viewId);

            if (view != null)
                return view.Owner == null ? 0 : view.Owner.ActorNumber;

            return viewId / PhotonNetwork.MAX_VIEW_IDS;
        }

        private List<T> ListOf<T>(Dictionary<int, List<T>> lists, int actor)
        {
            ForgetOldRoom();

            if (!lists.TryGetValue(actor, out List<T> list))
            {
                list = new List<T>();
                lists[actor] = list;
            }

            return list;
        }

        private void ForgetOldRoom()
        {
            string room = PhotonNetwork.CurrentRoom == null ? null : PhotonNetwork.CurrentRoom.Name;

            if (room == _room)
                return;

            _room = room;
            _entries.Clear();
            _missed.Clear();
            _spawns.Clear();
            _removals.Clear();
        }

        private Hashtable Copy(Hashtable source)
        {
            Hashtable copy = new Hashtable();

            foreach (object key in source.Keys)
                copy[key] = source[key];

            return copy;
        }

        private sealed class Entry
        {
            public Entry(int viewId, string method, int senderActor, Hashtable rpc)
            {
                ViewId = viewId;
                Method = method;
                SenderActor = senderActor;
                Rpc = rpc;
            }

            public int ViewId { get; }
            public string Method { get; }
            public int SenderActor { get; }
            public Hashtable Rpc { get; }
        }
    }
}
