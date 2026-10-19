# PeakSentinel — Feature Index

> Прототип античита для PEAK (BepInEx 5): пишет в лог и в ленту подключений, кто сделал подозрительное действие с предметами. Определение «кто» — по отправителю сетевого сообщения, его ставит сервер Photon.

## Layer Map
SentinelPlugin (BepInPlugin) — создаёт отчёт и аудиты, Harmony PatchAll
├── Patches/NetworkPatches — PhotonNetwork.ExecuteRpc (префикс: каждое входящее и локальное RPC с отправителем), PhotonNetwork.NetworkInstantiate(InstantiateParameters, bool, bool) (постфикс: любое сетевое создание объекта с создателем; аудит только для пришедших по сети — instantiateEvent), PlayerConnectionLog.Awake (лента)
├── Patches/SentinelHooks — статические ссылки для патчей
├── Detection/RpcAudit — правила по RPC предметов; Inspect возвращает «выполнять ли»: префикс ExecuteRpc отменяет RPC, если нарушение и BlockCheatSpawns включён и ты хост (свои запросы хоста не блокируются при AllowMySpawns); отклонение пишется как BLOCKED
├── Detection/ProtectionSettings — конфиг [Protection]: BlockCheatSpawns (вкл.), AllowMySpawns (вкл.)
├── Detection/InstantiateAudit — создание предметов не-хостом
├── Reporting/DebugTrace — текстовый лог отладки BepInEx/PeakSentinel.log (перезаписывается при запуске, AutoFlush): каждое RPC предметов (метод, отправитель#actor и host, view и его владелец, аргументы), каждое созданное по сети и локально предметное тело (префаб, создатель, позиция), каждая находка SUSPICIOUS/notice; конфиг [Debug] TraceFile (вкл.), TraceAllRpcs (выкл. — писать вообще все RPC)
├── Reporting/VersionBadge — IMGUI-надпись «Peak Sentinel 0.1.0» вверху по центру, пока ты в комнате Photon (лобби и забег); отдельный GameObject DontDestroyOnLoad
├── Reporting/SentinelWindow — окно менеджера (IMGUI GUI.Window, перетаскивается), открыть/закрыть [Manager] ToggleKey = F6 (свободна: F2 BadScout/античит, F3–F4 Spectator, F5 спавнеры, F7 Rope Carabiner, F9 PEAKER/OBS, F12 Steam); переключатели [Protection] BlockCheatSpawns и AllowMySpawns, «показывать notice», «очистить», пометка «блок работает только у хоста»; история последних 500 находок копится всё время, даже при закрытом окне (красный SUSPICIOUS, оранжевый BLOCKED, серый notice), вертикальная прокрутка всегда видна, «Follow newest» держит внизу (выключить — листать вручную); перетаскивание только за заголовок, чтобы не мешать полосе прокрутки; SUSPICIOUS и BLOCKED параллельно идут в ванильную ленту подключений; пока открыто — курсор свободен (патч GUIManager.UpdateWindowStatus)
└── Reporting/SuspicionReport — лог BepInEx (SUSPICIOUS / notice) + лента подключений (повтор того же текста не чаще раза в 2 с) + консоль

## Правила RpcAudit
| RPC | Кто вправе | Что пишет |
|---|---|---|
| `RPC_SpawnItemInHandMaster` (CharacterItems, идёт хосту) | никто в обычной игре; владелец — только паспорт в аэропорту (игра выдаёт его этой же командой) и стартовый предмет режима Grapple | «заспавнил X в руки Y» / «себе в руки» |
| `RequestPickup` (Item, идёт хосту) | владелец персонажа из аргумента или хост (после спавна в руки хост сам шлёт «подобрать» за игрока — виноват отправитель спавна, он уже пойман) | «заставил Y подобрать X» |
| `OnPickupAccepted` | хост | «принудительный подбор» |
| `SyncInventoryRPC`, `RPC_SetInventory`, `RPCRemoveItemFromSlot` (Player) | владелец или хост | «переписал инвентарь Y» |
| `RPC_InitHoldingItem`, `EquipSlotRpc` | владелец или хост | «вложил предмет в руки Y» |
| `RPC_Clone` | — | любой клон предмета |

## InstantiateAudit
Создатель не хост, объект — предмет: ниже y = −400 (визуал рюкзака) — пропуск; такой предмет есть в слотах создателя — notice (достал из инвентаря); иначе SUSPICIOUS (спавн или законный разбиваемый объект — проверить).

## Ограничения прототипа
- Видит только сообщения, дошедшие до этого клиента: RPC хосту видит только хост.
- Хост-читер не ловится (хост создаёт предметы сам).
- Сборка кладёт DLL в профиль Thunderstore Default: `BepInEx/plugins/PeakSentinel`.
- Каким модом сделан спавн, не пишется: ItemSpawnerPremium и ItemSpawnerEnhanced оба идут через ту же ванильную команду и ничего о себе по сети не сообщают — по имени их не отличить.
- Проверено в игре (хост + 1 игрок): выдача себе и другому через спавнер блокируется, предмет не появляется ни у кого.
