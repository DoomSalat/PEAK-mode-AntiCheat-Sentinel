using System.Collections.Generic;
using PeakSentinel.Network;
using PeakSentinel.Reporting;
using PeakSentinel.Settings;
using Photon.Pun;
using UnityEngine;
using PhotonPlayer = Photon.Realtime.Player;

namespace PeakSentinel.Detection
{
    public sealed class InstantiateAudit
    {
        private const float BackpackVisualHeight = -400f;
        private const char PathSeparator = '/';
        private const string DangerousTag = "spawns dangerous objects";
        private const float GiveSeconds = 15f;
        private const float BreakReach = 3f;

        private static readonly HashSet<string> Dangerous = new HashSet<string> // Static: fixed host-only prefabs (PEAKER list plus the mobs PEAK-MX spawns)
        {
            "BeeSwarm", "Tornado", "Bingbong_Blow", "Bingbong_Grab", "Bingbong_Push", "Bingbong_Suck", "Bingbong_Push_Gentle",
            "MushroomZombie", "Character_Scoutmaster",
        };

        private readonly SuspicionReport _report;
        private readonly DebugTrace _trace;
        private readonly DetectionSettings _detection;
        private readonly ProtectionSettings _protection;
        private readonly ItemRpcAudit _items;
        private readonly ForeignObjects _objects;
        private readonly ItemLocator _locator;
        private bool _removeNext;

        public InstantiateAudit(SuspicionReport report, DebugTrace trace, DetectionSettings detection, ProtectionSettings protection, ItemRpcAudit items, ForeignObjects objects, ItemLocator locator)
        {
            _locator = locator;
            _objects = objects;
            _protection = protection;
            _items = items;
            _report = report;
            _trace = trace;
            _detection = detection;
        }

        public bool AllowCreate(PhotonPlayer creator, string prefabPath)
        {
            string prefab = PrefabOf(prefabPath);

            if (!_detection.DangerousSpawns.Value || creator == null || creator.IsMasterClient || !Dangerous.Contains(prefab))
                return true;

            bool block = _detection.BlockDangerousSpawns.Value;
            bool removeForAll = block && PhotonNetwork.IsMasterClient;
            string action = removeForAll ? $"spawned '{prefab}' (only the host may spawn it), removed for everyone" : $"spawned '{prefab}' (only the host may spawn it)";
            _report.Refuse(creator, FindingCategory.Cheats, DangerousTag, action, block);
            _removeNext = removeForAll;
            return !block || removeForAll;
        }

        public void Inspect(PhotonPlayer creator, GameObject created, bool fromNetwork)
        {
            if (_removeNext)
            {
                _removeNext = false;

                if (created != null)
                    _objects.RemoveForEveryone(created, creator);

                return;
            }

            if (created == null || !created.TryGetComponent(out Item item))
                return;

            _trace.Instantiate(creator, created.name, created.transform.position);

            if (!fromNetwork || creator == null || creator.IsMasterClient)
                return;

            if (created.transform.position.y < BackpackVisualHeight)
                return;

            if (_locator.Carries(creator, item.itemID))
            {
                _report.Notice(creator, FindingCategory.Items, $"took '{item.GetItemName()}' out of the inventory");
                return;
            }

            if (_items.RefusedGiveWithin(creator, GiveSeconds))
            {
                RemoveGiven(creator, created, item);
                return;
            }

            if (_locator.LooseItemNear(creator, created.transform.position, BreakReach, created))
            {
                _report.Notice(creator, FindingCategory.Items, $"got '{item.GetItemName()}' out of an item they broke");
                return;
            }

            bool block = _protection.BlockCheatSpawns.Value && PhotonNetwork.IsMasterClient;
            _report.Flag(creator, FindingCategory.Items, $"spawned '{item.GetItemName()}' out of nowhere (not in their inventory, nothing broken there)", block);

            if (block)
                _objects.RemoveForEveryone(created, creator);
        }

        private void RemoveGiven(PhotonPlayer creator, GameObject created, Item item)
        {
            bool block = _protection.BlockCheatSpawns.Value && PhotonNetwork.IsMasterClient;
            _report.Flag(creator, FindingCategory.Items, $"took out '{item.GetItemName()}' they gave themselves (the host refused it)", block);

            if (block)
                _objects.RemoveForEveryone(created, creator);
        }

        private string PrefabOf(string path)
        {
            if (path == null)
                return string.Empty;

            int slash = path.LastIndexOf(PathSeparator);
            return slash < 0 ? path : path.Substring(slash + 1);
        }
    }
}
