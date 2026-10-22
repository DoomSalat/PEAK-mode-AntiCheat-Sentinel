using System.Collections.Generic;
using PeakSentinel.Compat;
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
        private const string UsesTag = "uses items without end";
        private const string CloneTag = "spawns copies of their character";
        private const string CharacterPrefab = "Character";
        private const string AdminMenuBot = "AdminBot";
        private const string AdminMenuTag = "Admin_menukz user";
        private const float GiveSeconds = 15f;
        private const float BreakReach = 3f;

        private static readonly HashSet<string> Dangerous = new HashSet<string> // Static: fixed host-only prefabs (PEAKER list plus the mobs PEAK-MX spawns)
        {
            "BeeSwarm", "Tornado", "Bingbong_Blow", "Bingbong_Grab", "Bingbong_Push", "Bingbong_Suck", "Bingbong_Push_Gentle",
            "MushroomZombie", "MushroomZombie_Player", "Character_Scoutmaster",
        };

        private readonly SuspicionReport _report;
        private readonly DebugTrace _trace;
        private readonly DetectionSettings _detection;
        private readonly ProtectionSettings _protection;
        private readonly ItemRpcAudit _items;
        private readonly ForeignObjects _objects;
        private readonly ItemLocator _locator;
        private readonly TrueAscentState _ascent;
        private readonly SpawnBlame _blame;
        private readonly ItemUseLedger _uses;
        private bool _removeNext;

        public InstantiateAudit(SuspicionReport report, DebugTrace trace, DetectionSettings detection, ProtectionSettings protection, ItemRpcAudit items, ForeignObjects objects, ItemLocator locator, TrueAscentState ascent, SpawnBlame blame, ItemUseLedger uses)
        {
            _uses = uses;
            _blame = blame;
            _ascent = ascent;
            _locator = locator;
            _objects = objects;
            _protection = protection;
            _items = items;
            _report = report;
            _trace = trace;
            _detection = detection;
        }

        public bool AllowCreate(PhotonPlayer creator, string prefabPath, int sentAt, int viewId, object[] data)
        {
            string prefab = PrefabOf(prefabPath);

            if (creator != null && !creator.IsLocal && data != null && data.Length > 0 && data[0] as string == AdminMenuBot)
            {
                _report.Tag(creator, AdminMenuTag);
                _report.Flag(creator, FindingCategory.Cheats, $"spawned a bot ('{prefab}') with the Admin_menukz cheat menu (the menu marks its bots '{AdminMenuBot}')");
            }

            if (_detection.DangerousSpawns.Value && prefab == CharacterPrefab && creator != null && !creator.IsLocal && PlayerHandler.GetPlayerCharacter(creator) != null)
                return RefuseClone(creator, viewId);
            Item shooter = creator == null || creator.IsLocal || unchecked(PhotonNetwork.ServerTimestamp - sentAt) > PunCodes.CachedAgeMilliseconds ? null : ShooterOf(creator, prefab);

            if (_detection.HarmToOthers.Value && _uses.Overspent(shooter))
            {
                bool blockShot = _detection.BlockHarmToOthers.Value;
                bool removeShot = blockShot && PhotonNetwork.IsMasterClient;
                _report.Refuse(creator, FindingCategory.Cheats, UsesTag, $"fired '{shooter.GetItemName()}' more than its {shooter.totalUses} uses (endless uses cheat){(removeShot ? ", removed for everyone" : string.Empty)}", blockShot);
                _removeNext = removeShot;
                return !blockShot || removeShot;
            }

            if (!_detection.DangerousSpawns.Value || creator == null || creator.IsMasterClient || !Dangerous.Contains(prefab))
                return true;

            bool block = _detection.BlockDangerousSpawns.Value;
            bool removeForAll = block && PhotonNetwork.IsMasterClient;
            string action = removeForAll ? $"spawned '{prefab}' (only the host may spawn it), removed for everyone" : $"spawned '{prefab}' (only the host may spawn it)";
            _report.Refuse(creator, FindingCategory.Cheats, DangerousTag, action, block);
            _removeNext = removeForAll;
            return !block || removeForAll;
        }

        public void Inspect(PhotonPlayer creator, GameObject created, bool fromNetwork, int sentAt)
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

            if (!fromNetwork || creator == null || creator.IsMasterClient || unchecked(PhotonNetwork.ServerTimestamp - sentAt) > PunCodes.CachedAgeMilliseconds)
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

            if (!_blame.Blames(creator) && _locator.LooseItemNear(creator, created.transform.position, BreakReach, created))
            {
                _report.Notice(creator, FindingCategory.Items, $"got '{item.GetItemName()}' out of an item they broke");
                return;
            }

            string ascent = created.TryGetComponent(out PhotonView view) ? _ascent.SpawnCause(view) : null;

            if (ascent != null)
            {
                _report.Notice(creator, FindingCategory.Items, $"spawned '{item.GetItemName()}' ({ascent})");
                return;
            }

            bool block = _protection.BlockCheatSpawns.Value && PhotonNetwork.IsMasterClient;
            _blame.Note(creator);
            _report.Flag(creator, FindingCategory.Items, $"spawned '{item.GetItemName()}' out of nowhere (not in their inventory, nothing broken there)", block);

            if (block)
                _objects.RemoveForEveryone(created, creator);
        }

        private bool RefuseClone(PhotonPlayer creator, int viewId)
        {
            bool removeForAll = PhotonNetwork.IsMasterClient && viewId != 0;

            if (removeForAll)
                _objects.RemoveForEveryone(viewId, creator);

            _report.Refuse(creator, FindingCategory.Cheats, CloneTag, $"spawned a second copy of their character (cheat bots), always blocked{(removeForAll ? ", removed for everyone" : string.Empty)}", true);
            return false;
        }

        private void RemoveGiven(PhotonPlayer creator, GameObject created, Item item)
        {
            bool block = _protection.BlockCheatSpawns.Value && PhotonNetwork.IsMasterClient;
            _blame.Note(creator);
            _report.Flag(creator, FindingCategory.Items, $"took out '{item.GetItemName()}' they gave themselves (the host refused it)", block);

            if (block)
                _objects.RemoveForEveryone(created, creator);
        }

        private Item ShooterOf(PhotonPlayer creator, string prefab)
        {
            Character character = PlayerHandler.GetPlayerCharacter(creator);
            Item held = character == null ? null : character.data.currentItem;

            if (held == null)
                return null;

            foreach (Peak.Action_RaycastSpawnSomething shot in held.GetComponents<Peak.Action_RaycastSpawnSomething>())
            {
                if (shot.prefabToSpawn != null && shot.prefabToSpawn.name == prefab)
                    return held;
            }

            return null;
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
