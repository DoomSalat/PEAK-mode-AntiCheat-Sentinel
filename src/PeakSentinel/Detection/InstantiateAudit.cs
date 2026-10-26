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
        private const string CloneAction = "spawned a second copy of their character (cheat bots)";
        private const string RemovedNote = ", removed for everyone";
        private const string CharacterPrefab = "Character";
        private const string StatuePrefab = "PetrifiedScout";
        private const string AdminMenuBot = "AdminBot";
        private const string AdminMenuTag = "Admin_menukz user";
        private const float GiveSeconds = 15f;
        private const float BreakReach = 3f;

        private static readonly HashSet<string> Dangerous = new HashSet<string> // Static: fixed host-only prefabs (PEAKER list plus the mobs PEAK-MX spawns)
        {
            "BeeSwarm", "Tornado", "Bingbong_Blow", "Bingbong_Grab", "Bingbong_Push", "Bingbong_Suck", "Bingbong_Push_Gentle",
            "MushroomZombie", "MushroomZombie_Player", "Character_Scoutmaster",
        };

        private readonly HostOnlyPrefabs _hostOnly = new HostOnlyPrefabs();
        private readonly DaggerFeeds _daggerFeeds;
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

        public InstantiateAudit(SuspicionReport report, DebugTrace trace, DetectionSettings detection, ProtectionSettings protection, ItemRpcAudit items, ForeignObjects objects, ItemLocator locator, TrueAscentState ascent, SpawnBlame blame, ItemUseLedger uses, DaggerFeeds daggerFeeds)
        {
            _daggerFeeds = daggerFeeds;
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

            if (_report.Spares(creator))
                return true;

            if (_detection.DangerousSpawns.Value && prefab == CharacterPrefab && creator != null && !creator.IsLocal && PlayerHandler.GetPlayerCharacter(creator) != null)
                return RefuseClone(creator, viewId);
            Item shooter = creator == null || creator.IsLocal || unchecked(PhotonNetwork.ServerTimestamp - sentAt) > PunCodes.CachedAgeMilliseconds ? null : ShooterOf(creator, prefab);

            if (_detection.HarmToOthers.Value && _uses.Overspent(shooter))
            {
                bool blockShot = _detection.BlockHarmToOthers.Value;
                bool removeShot = blockShot && PhotonNetwork.IsMasterClient;
                string shot = $"fired '{shooter.GetItemName()}' more than its {shooter.totalUses} uses (endless uses cheat)";

                if (removeShot)
                    _report.Recover(creator, FindingCategory.Cheats, UsesTag, shot + RemovedNote);
                else
                    _report.Refuse(creator, FindingCategory.Cheats, UsesTag, shot, blockShot);

                _removeNext = removeShot;
                return !blockShot || removeShot;
            }

            if (!_detection.DangerousSpawns.Value || creator == null || creator.IsMasterClient || !(IsDangerous(prefab, prefabPath) || IsFakeStatue(prefab, creator)))
                return true;

            bool block = _detection.BlockDangerousSpawns.Value;
            bool removeForAll = block && PhotonNetwork.IsMasterClient;
            string action = $"spawned '{prefab}' (only the host may spawn it)";

            if (removeForAll)
                _report.Recover(creator, FindingCategory.Cheats, DangerousTag, action + RemovedNote);
            else
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

            if (created == null)
                return;

            _trace.Instantiate(creator, created.name, created.transform.position);

            if (!created.TryGetComponent(out Item item))
                return;

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
            ReportSpawn(creator, created, block, $"spawned '{item.GetItemName()}' out of nowhere (not in their inventory, nothing broken there)");
        }

        private bool RefuseClone(PhotonPlayer creator, int viewId)
        {
            bool removeForAll = PhotonNetwork.IsMasterClient && viewId != 0;

            if (!removeForAll)
                return _report.Refuse(creator, FindingCategory.Cheats, CloneTag, CloneAction + ", always blocked", true);

            _objects.RemoveForEveryone(viewId, creator);
            return _report.Recover(creator, FindingCategory.Cheats, CloneTag, CloneAction + RemovedNote);
        }

        private void RemoveGiven(PhotonPlayer creator, GameObject created, Item item)
        {
            bool block = _protection.BlockCheatSpawns.Value && PhotonNetwork.IsMasterClient;
            _blame.Note(creator);
            ReportSpawn(creator, created, block, $"took out '{item.GetItemName()}' they gave themselves (the host refused it)");
        }

        private void ReportSpawn(PhotonPlayer creator, GameObject created, bool remove, string action)
        {
            if (!remove || _report.Spares(creator))
            {
                _report.Flag(creator, FindingCategory.Items, action);
                return;
            }

            _objects.RemoveForEveryone(created, creator);
            _report.Recover(creator, FindingCategory.Items, action + RemovedNote);
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

        public void CheckOwnSpawn(PhotonPlayer creator, string prefabPath)
        {
            if (!_detection.CheckMyself.Value || creator == null || !creator.IsLocal)
                return;

            string prefab = PrefabOf(prefabPath);

            if (_detection.DangerousSpawns.Value && !creator.IsMasterClient && IsDangerous(prefab, prefabPath))
                _report.Refuse(creator, FindingCategory.Cheats, DangerousTag, $"spawned '{prefab}' (only the host may spawn it)", false);
        }

        private bool IsFakeStatue(string prefab, PhotonPlayer creator)
        {
            return prefab == StatuePrefab && !_daggerFeeds.StabbedRecently(PlayerHandler.GetPlayerCharacter(creator)); // Only the host spawns statues on death; a player spawns their own only when a dagger with a checkpoint kills them
        }

        private bool IsDangerous(string prefab, string prefabPath)
        {
            return Dangerous.Contains(prefab) || _hostOnly.IsHostOnly(prefabPath);
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
