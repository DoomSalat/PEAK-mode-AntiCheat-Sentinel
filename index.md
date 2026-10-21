# PeakSentinel — Feature Index

> Анти-чит для PEAK (BepInEx 5): по сетевым сообщениям Photon определяет, КТО сделал запрещённое действие (отправителя RPC ставит сервер, подделать его нельзя), пишет это в лог, окно F6 и ленту подключений и по настройкам блокирует или защищает.

## Layer Map
```
SentinelPlugin                          — composition root по шагам: ядро (SentinelSettings, DebugTrace, FindingLog, SuspicionReport) → GuardPackets → AuditRpcs → Watch → OpenWindow, затем Harmony
├── Settings/SentinelSettings            — все группы настроек одним объектом
├── Game/GameInternals                   — единственный доступ к закрытым полям игры и PUN (кэшированные FieldRef), чинить здесь после патча PEAK
├── Patches/NetworkPatches              — OnEvent → PacketGate; ExecuteRpc → RpcRouter; NetworkInstantiate → InstantiateAudit; PhotonView.RPC → HandGuard; курсор окна
├── Patches/CampfirePatches              — «все у костра» (Campfire.EveryoneInRange, обе перегрузки) не учитывает замьюченных: их застывший персонаж не мешает зажечь костёр, отдыху и остановке тумана; в подсказке «нельзя зажечь» их тоже нет
├── Patches/PauseMenuPatches            — PlayerUI.Initialize → AvatarBadge на аватарке в меню Esc
├── Patches/EnvironmentPatches          — AntiSphere / WindChillZone / PlayerMoveZone помечают персонажей для FlightExemptions
├── Network/RpcRouter                   — разбирает RPC в RpcCall, пишет трассу, опрашивает все IRpcAudit
│   ├── Detection/ItemRpcAudit          — выдача и подмена предметов
│   ├── Protection/HandGuard            — подсунутый в руки предмет (клиент)
│   ├── Protection/KickShield           — пинки
│   ├── Detection/HarmAudit             — вред другим игрокам
│   ├── Detection/PushAudit             — толчки без пинка, «пульс», «взрыв» (честные причины — LegitForces + ItemLocator)
│   ├── Detection/WorldAudit            — события, которые вправе слать только хост
│   ├── Detection/ControlAudit          — принуждение чужого персонажа (прыжок, присед, отпустить, выронить, эмоция, статусы)
│   ├── Detection/BuildAudit            — постройка чужим предметом и спам постройками (флаги)
│   ├── Detection/CarryAudit            — переноска не по правилам игры
│   ├── Detection/ReviveAudit           — воскрешение без источника (само-рес)
│   ├── Detection/AreaEffects           — эффекты по области (амулеты, предметы) как источник
│   ├── Detection/StatusAudit           — лок статусов (съел вредное, статус застыл)
│   └── Detection/EffectAudit           — защита / бесконечная стамина без источника
├── Detection/InstantiateAudit          — сетевое создание предметов и опасных префабов
├── Detection/CheatModScan              — метки Cherry / Atlas / PEAK-MX
├── Detection/IdentityAudit             — подмена имени и Steam ID, смена имени посреди игры
├── Detection/Stamina/StaminaAudit      — стамина не тратится (+ StaminaTrack)
├── Detection/Flight/FlightAudit        — полёт и сверхскорость
│   ├── FlightTrack, HorizontalSpeed    — история высот и пути
│   └── FlightExemptions                — законные причины быть в воздухе (RopeLinks, EnvironmentMarks)
├── Protection/BanGuard + BanList       — постоянный бан
├── Protection/DesyncFixer              — лечение «взбесившегося» тела другого игрока у тебя (рассинхрон после захода)
├── Protection/HostGuard                — захват хоста: запись, возврат, кик
├── Protection/SentinelPresence         — метка Sentinel игрока, проверка хоста при входе
├── Network/StateLedger + ForeignObjects — снимки/пропущенное для Unmute и Resync; удаление чужих объектов
├── Protection/PacketGate + MuteList    — каждое входящее событие Photon: mute игрока, флуд, захват чужого персонажа
├── Reporting/SuspicionReport           — запись находки: BepInEx-лог, DebugTrace, FindingLog, лента подключений
└── Ui/SentinelWindow                   — окно F6 (Back), получает SentinelSettings + WindowServices
    ├── HotkeyButton, SettingsBinder, FeedOptions, FindingFeed
    ├── PlayerListView → PlayerProfileView, общие действия PlayerActions
    └── Pages/ Log, Items, Cheats, Other, Players, Bans
```

## Classes

### Back
| Class | File | Role |
|---|---|---|
| SentinelPlugin | SentinelPlugin.cs | Composition root. `Awake`: SentinelSettings, DebugTrace, FindingLog, SuspicionReport, SentinelHooks, хост-объект, StateLedger/ForeignObjects/MuteList/LatencyMeter/SpawnClock; затем шаги `GuardPackets` (PacketGate), `AuditRpcs` (все IRpcAudit в RpcRouter, InstantiateAudit, HandGuard), `Watch` (MonoBehaviour-наблюдатели, BanList; возвращает WindowServices), `OpenWindow`; Harmony.PatchAll |
| SentinelSettings | Settings/SentinelSettings.cs | Debug, Manager, Protection, Detection, Other, Flood, Flight, Stamina — создаются из ConfigFile в одном месте |
| GameInternals | Game/GameInternals.cs | static: всё обращение к закрытым членам игры и PUN — Campfire.ALL_CAMPFIRES, RescueHook isPulling/targetPlayer, FrogTongue._targetCharacter, Skelleton.spawnedFromCharacter, RunManager.timerActive, PhotonNetwork.RemoveInstantiatedGO. Поля — кэшированные `AccessTools.FieldRef` (без поиска поля на каждый вызов) |
| WindowServices | Ui/WindowServices.cs | Сервисы окна одним объектом: FindingLog, SuspicionReport, BanList, BanGuard, MuteList, ReportExport, LatencyMeter, DesyncFixer, SentinelPresence |
| SentinelWindow | Ui/SentinelWindow.cs | Окно F6: клавиша открытия (или захват новой клавиши через HotkeyButton), Canvas строится при первом открытии и заново при смене шрифта, свой EventSystem если в сцене нет, вкладки, обновление открытой вкладки 4 раза в секунду; `IsOpen` читает патч курсора |
| FlightAudit | Detection/Flight/FlightAudit.cs | 10 раз в секунду по каждому чужому персонажу: высота (своя сфера радиусом GroundProbeRadius вниз по всем физическим слоям, кроме персонажей — камни, брёвна и грибы лежат на Default — тонкий луч промахивался мимо уступа, когда игрок стоял краем ступни), вертикальная скорость по позициям, горизонтальная скорость; находки и откат «Snap back» |
| StaminaAudit | Detection/Stamina/StaminaAudit.cs | 4 раза в секунду по каждому чужому персонажу: стамина (обычная + бонусная) из CharacterSyncer, пока игрок лезет (стена, верёвка, лиана) или бежит (isSprinting, на земле, быстрее MinSprintSpeed); если за усилие стамина не опустилась ниже минимума на MinDrop дольше FlagSeconds — подозрение «stamina does not go down», метка «infinite stamina», раз за усилие; не проверяется: мёртв/без сознания, аэропорт, эффекты InfiniteStamina/FasterBoi, стамина пуста; блокировать нельзя — стамина считается на клиенте игрока; строки STAMINA в PeakSentinel.log |
| CheatModScan | Detection/CheatModScan.cs | Раз в 5 с: свойства игроков CherryUser/CherryOwner/AtlUser/AtlOwner, pmx_ver (PEAK-MX), хост кикает при KickCheatModUsers |
| BanGuard | Protection/BanGuard.cs | Раз в секунду у хоста кикает игроков из BanList, повторяет кик каждые 5 с, пока игрок в комнате; `Ban(player)` |

### Network
| Class | File | Role | Key API |
|---|---|---|---|
| RpcRouter | Network/RpcRouter.cs | Hashtable RPC → RpcCall (ключи 0 view, 3 имя, 4 аргументы, 5 индекс в RpcList); трасса, если RPC кто-то смотрит или TraceAllRpcs; результат — И всех аудитов | `bool Inspect(Hashtable, Player)` |
| IRpcAudit | Network/IRpcAudit.cs | Контракт аудита RPC | `bool Watches(string)`, `bool Inspect(RpcCall)` — «выполнять ли» |
| RpcCall | Network/RpcCall.cs | Данные вызова и разбор аргументов | `Sender, View, Owner, FromHost, FromOwner, FromLocal, Argument/StringAt/MagnitudeAt/ViewAt(i), SentBy(p), Component<T>()` |
| LatencyMeter | Network/LatencyMeter.cs | Пинг других игроков (игра его не передаёт): по каждому обновлению синхронизации (события 201/206, элемент 0 — серверное время отправки) задержка «его клиент → сервер → ты», сглаженная; пинг ≈ (задержка − половина своего пинга) × 2; нет данных 5 с — «—». У заглушённого не меряется (его пакеты отбрасываются) | `Note(actor, sentServerTime)`, `PingOf(actor)` |
| RpcNames | Network/RpcNames.cs | Имена ванильных RPC | const string |

### Instruments — Detection
| Class | File | Role |
|---|---|---|
| ItemRpcAudit | Detection/ItemRpcAudit.cs | Правила предметов (таблица ниже); блок у хоста при BlockCheatSpawns, свои запросы хоста не блокируются при AllowMySpawns  RPC_RequestStickFakeItemToPlayer / InstantiateAndGrabRPC / RPCAddItemToBackpack — персонаж в аргументах должен принадлежать отправителю (или отправитель хост), иначе «stuck a fake item onto / spawned … into the hands of / moved items into a backpack in the name of X». | `RPC_Clone` (шлётся хосту): честно — только с амулетом клонирования (зелёный артефакт, Action_CloneSelectedItem) в руке, цель не дальше его range + 2 м и без тегов ScoutAmulet/NonCloneable — notice «cloned X with the clone amulet»; иначе «cloned X without the clone amulet», хост клон не делает. ||
| HarmAudit | Detection/HarmAudit.cs | Свои RPC не проверяет (это наши починки). Починки: убийство — `RPCA_ReviveAtPosition` в месте смерти, обморок — `RPCA_UnPassOut`, телепорт и рывок — `WarpPlayerRPC` туда, где жертва стояла перед атакой; зомби/окаменение — без починки. Отправитель не владелец персонажа и не хост: Die/SetDead — убил (кроме каннибализма: у жертвы включён каннибализм, едок в сознании, голод ≥ 0,7 − 0,05, не дальше 6 м — notice «ate X»), PassOut — обморок, Zombify/Petrify, RPC_SyncPetrify (окаменение PEAK-MX, Fix возвращает прежний процент), RocketExplodeRPC (взрыв PEAK-MX: клиент жертвы сам создаёт взрыв ракеты у себя, и его физика раскидывает — честно шлёт только владелец, летящий на ракете; Fix — вернуть на место), WarpPlayerRPC — телепорт, сила ≥ 1500 — рывок (законные удары до ~500). Самотелепорт (WarpPlayerRPC владельца на себя) без предмета Action_WarpToRandomPlayer в руке и без своего брошенного предмета WarpOnThrow рядом с точкой (6 м — он телепортирует бросившего) — по сети себя честно телепортирует только этот предмет (остальные телепорты игра делает локально или шлёт хост): красная находка «teleported themselves next to X / to (…)», метка «teleports themselves»; при BlockHarmToOthers (вкл.) у тебя не выполняется, а хост сразу возвращает его на прежнее место у всех (WarpPlayerRPC всем); Fix — то же вручную. Телепорт в NaN или дальше 100 000 по любой оси («в бесконечность», чёрный экран) блокируется всегда, от кого угодно. RPCA_Fall не проверяется — его законно шлют чужие клиенты |
| WorldAudit | Detection/WorldAudit.cs | Свои RPC не проверяет. Починки: потушенный костёр — `Light_Rpc` с прежним временем горения (кроме костра конца мини-забега), туман — `RPCA_SyncFog` с прежними размером и движением; вертолёт, конец игры, мосты, верёвки — без починки. Сложность (RPC_SyncAscent, Fix — вернуть прежнюю всем), настройки забега (RPC_SyncRunSettings), лава (RPC_SyncLava), спаун ресурса (RPC_SpawnResourceAtPosition), сброс тумана (RPC_InitFog) — только хост. Время суток (RPCA_SyncTime) и таймер забега (RPC_SyncTime) — только хост; Fix — `RPCA_SyncTime` / `RPC_SyncTime` всем с днём, временем суток и таймером, какими они были до атаки (хост и так пересылает время суток раз в 5 с). Сосулька и трясущийся камень (ShakeRock) — от игрока рядом (15 м: трясёт тот, кто встал); ненадёжный крюк (ShittyPiton) и отрыв верёвки (Detach_Rpc) — от владельца объекта (их ставят игроки). Таблица как в PEAKER (метод → компонент на view, кто вправе: хост / владелец / жертва): банан, пчёлы, вертолёт (кроме законного), костёр, конец игры, призрак, извержение, лиана, туман, старт/возврат, мосты, верёвки, сосульки, крюки |
| LegitForces | Detection/LegitForces.cs | Честные причины толчка чужого игрока не от хоста: жертва в венериной мухоловке (статус FlyTrap — её притягивание шлют все клиенты рядом); рядом (6 м) лежащий/летящий предмет толкающего (Bonkable — снежок, тарелка, кокос и т.п.; Breakable — сломанный кокос; мобы-предметы); его спасательный крюк (isPulling/targetPlayer) или язык лягушки (_targetCharacter) тянет именно эту жертву. Используют PushAudit (в начале, до всех проверок) и HarmAudit (рывок) |
| PushAudit | Detection/PushAudit.cs | RPCA_AddForceAtPosition / RPCA_AddForceToBodyPart по чужому персонажу не от владельца, хоста или нас и не в течение 2 с после RPCA_Kick того же игрока. Взрыв — 2+ толчка от одного отправителя за 0,15 с (толчки по своему персонажу тоже считаются в серию: PEAK-MX сначала толкает себя); пульс — 4+ толчка одной жертве за 3 с; одиночный толчок («пнуть» PEAK-MX) — если рядом с жертвой (5 м) нет лежащего/летящего предмета отправителя (удар брошенным предметом, Bonkable), это «pushed X (no kick, nothing they threw hit them)». Группа HarmToOthers/BlockHarmToOthers; при блоке все толчки отправителя молча отбрасываются, и каждый продлевает блок на 3 с (одна находка на атаку). Fix — `WarpPlayerRPC` туда, где жертва стояла в начале серии |
| InstantiateAudit (дополнение) | Detection/InstantiateAudit.cs | Предмет, созданный не из инвентаря в течение 15 с после отбитой выдачи себе (ItemRpcAudit.RefusedGiveWithin), — находка, и хост удаляет его у всех (ForeignObjects) при BlockCheatSpawns |
| AreaEffects | Detection/AreaEffects.cs | IRpcAudit: помнит 4 с эффекты по области — `AddMassAffliction` (амулет бесконечной стамины раздаёт её всем рядом каждые 0,5 с) и `TryAddAfflictionToLocalCharacter` (предметы с массовым эффектом). `SourceNear(character)` — источник эффекта, если игрок был в радиусе (+5 м), рядом (25 м) игрок с RadiateInfiniteStam или с амулетом стамины в карманах (ItemLocator). Используется EffectAudit |
| ItemLocator | Detection/ItemLocator.cs | Поиск предметов. Слоты (itemSlots + tempFullSlot + backpackSlot — выброшенный рюкзак создаётся из него): `Holds<T>(character)` — предмет с компонентом T (амулеты работают из кармана), `Carries(owner, itemId)` — есть ли предмет в инвентаре у хоста. Мир (по `Item.ALL_ITEMS` игры, не по всем PhotonView): `LooseItemNear(owner, point, reach)` — лежащий/летящий предмет владельца рядом (удар брошенным, содержимое сломанного), `OwnedNear<T>(owner, point, reach)` — его предмет с компонентом T рядом (WarpOnThrow). `PullsWithRescueHook(owner, victim)` — его спасательный крюк сейчас тянет именно эту жертву (поля RescueHook `isPulling`/`targetPlayer` есть у всех клиентов; крюк тянет через RPCA_AddForceToBodyPart каждый кадр). Используют PushAudit, InstantiateAudit, HarmAudit, AreaEffects, StatusAudit |
| PunCodes | Network/PunCodes.cs | static: коды событий PUN (200 RPC, 201/206 сериализация, 202 спаун, 204 удаление, 209/210/212 владение, 220+ серверные) и ключи данных (0 ViewID/префаб, 3 имя метода, 4 аргументы, 5 индекс метода, 7 instantiationId) |
| StatusAudit | Detection/StatusAudit.cs | MonoBehaviour + IRpcAudit, группа SourcelessEffects. На Consume чужим игроком предмета с Action_InflictPoison (яд через delay) или Action_ModifyStatus (+статус) запоминает статус до еды; через 2 с (+ delay яда) находка, только если статус застыл (за это время не изменился ни он, ни противоположный для тепла/холода) и не вырос — честное отравление может перекрываться естественным снятием, а лок PEAK-MX держит значения неподвижно; иначе — подозрение «ate X but their Poison did not rise», метка «ignores poison and other statuses» (лок статусов PEAK-MX). Не проверяет, когда игра сама не накладывает статус: мёртв, неуязвим (молоко и т.п.), скелет, зомби, статусы заблокированы игрой, аэропорт, голод при NoHunger; а также с лечащим амулетом в кармане или с HealAll |
| StateLedger | Network/StateLedger.cs | Два вида записей по игроку (от него самого или хоста). Снимки — всегда, последний по (view, метод): статусы, эффекты, окаменение, инвентарь, предмет в руке, присед, шипы, блок. События — только пропущенные под mute (смерть, обморок, воскрешение, «выглядит мёртвым/без сознания», призрак). Ещё под mute: созданные (202) и удалённые (204) им объекты. `CatchUp` (Unmute): удалить удалённое → создать созданное (`NetworkInstantiate`) → пропущенные события по порядку → снимки; пропущенное после этого забывается. `Reapply` (Resync): только снимки — события из старой истории не проигрываются, иначе залипает «живой труп». Всё через `ExecuteRpc`, то есть через проверки. RPC на ещё не созданные у нас views относит к игроку по диапазону ViewID. Очищается при смене комнаты |
| ForeignObjects | Network/ForeignObjects.cs | Удаление чужого сетевого объекта: `RemoveLocal` — внутренний `RemoveInstantiatedGO` PUN (только у себя); `RemoveForEveryone` — у себя + событие 204 остальным + удаление события создания (202, ключ 7) из кэша комнаты для создателя, чтобы объект не появился у зашедших позже |
| SpawnClock | Network/SpawnClock.cs | MonoBehaviour: время входа в комнату и первого появления каждого персонажа; `JoinedRoomWithin(s)`, `SpawnedWithin(Character/Player, s)`. Используют ItemRpcAudit, ReviveAudit, FlightExemptions |
| BackpackSwap | Detection/BackpackSwap.cs | Исключение для мода Piggyback (SwapBackpack): при подъёме и опускании несущий рассылает оба инвентаря с forceSync. Пропускается, если отправитель рядом (4 м) с владельцем, все слоты кроме рюкзака совпадают с данными хоста, а рюкзак либо отдан (запоминается на 3 с), либо получен из только что отданных — один раз, так что новый рюкзак или дубль не выдать |
| CarryAudit | Detection/CarryAudit.cs | `RPCA_StartCarry` (группа ForcedActions/BlockForcedActions): нести можно только как в игре — отправитель сам несущий, цель полностью без сознания (или в сознании, но вплотную — до 3 м: так носит мод Piggyback, у него предел 2 м), не мертва, не на чужих руках и не дальше 6 м. Иначе «picked up X while they were conscious and not next to them / dead / from far away / out of someone else's arms», при блоке у тебя переноска не начинается (тебя не тащит) |
| ControlAudit | Detection/ControlAudit.cs | Команды, которые игра шлёт только с клиента самого игрока (или хоста): JumpRpc, RPCA_SetCrouch, StopClimbingRpc, StopRopeClimbingRpc, DropItemFromSlotRPC, RPCA_PlayRemove (эмоция), CharacterDied / CharacterPassedOut (вид мёртвого / без сознания), SyncStatusesRPC (поддельные статусы), StopVineClimbingRpc, RPCA_ClimbJump, RPCA_Stick, RPCA_ClearStickData, RPCA_ClearJoint, DestroyHeldItemRpc, DropItemRpc (правила из CubeX), RPC_ApplyStatusesFromFloatArray (игра сама принимает только от хоста; в находке — расшифровка статусов, напр. «Petrify 100%»), RPCA_UnPassOut. От другого — «made X <action>», метка «controls other players», при BlockForcedActions не выполняется |
| BuildAudit | Detection/BuildAudit.cs | `CreatePrefabRPC` (Constructable: флаги и т. п.): чужим предметом — чит; больше [Flood] MaxBuildsPerMinute (6) за минуту — спам; при BlockBuildSpam постройка не появляется у тебя |
| IdentityAudit | Detection/IdentityAudit.cs | Раз в 5 с: ник сменился посреди игры — чит; ник не совпадает с именем Steam его Steam ID (свойство UserID, SteamFriends) — подозрение; Steam ID совпадает с твоим или с другим игроком — чит (подмена). Каждое — один раз за комнату |
| ReviveAudit | Detection/ReviveAudit.cs | IRpcAudit (время появления — SpawnClock), группа HostOnlyEvents/BlockHostOnlyEvents. `RPCA_ReviveAtPosition` честно шлют: хост (статуя-сундук, Пустота, случайное воскрешение), сам воскрешаемый в первые 20 с после появления его персонажа (заход в забег), игрок с чучелом скаута (ScoutEffigy) или Книгой костей (тег BookOfBones) в руке не дальше 12 м от точки воскрешения. Иначе — «revived themselves / revived X with no source», метка «revives without a source»; с Block не выполняется у тебя. Также честно: в Пустоте (финальный участок — каждый клиент поднимает мёртвых рядом с собой) и у скелета погибшего (Skelleton рядом с точкой воскрешения, spawnedFromCharacter — цель; оживляет любой игрок). Свои RPC (наши Fix) не проверяет |
| EffectAudit | Detection/EffectAudit.cs | MonoBehaviour + IRpcAudit. `Consume(consumerID)` от любого — запоминает, кто что съел; `SyncAfflictionsRPC(data, newType)` от владельца с новым эффектом Invincibility (молоко, амулет, гриб), BingBongShield или InfiniteStamina — ищет источник: щит — золотой Bing Bong (BingBongShieldWhileHolding) в руке сейчас; остальное — съедено что-то в пределах 5 с (решение через 2 с: «съел» может прийти позже эффекта). Нет источника — подозрение «got X with no source», метка; строка EFFECT в трассе с названием съеденного  NoHunger рядом с горящим костром (1,5 × moraleBoostRadius, список Campfire.ALL_CAMPFIRES) — законный источник: костёр раздаёт его всем рядом каждые 2 с. | Эффект в течение 60 с после отбитой хостом выдачи себе (ItemRpcAudit.RefusedGiveWithin) — красная находка «got X from an item they gave themselves»: у себя читер предмет всё равно использует, остановить это нельзя. | Кормление (`SendFeedDataRPC`: кто, кого, каким предметом) считается едой получателя в момент окончания кормления — Consume при кормлении не приходит. | Предмет в руке, который сам даёт этот эффект (камень лечения — синий Amulet_Healing — неуязвимость, Action_ApplyInfiniteStamina, Action_ApplyAffliction с этим типом), — источник: такие предметы применяются без Consume. ||
| InstantiateAudit | Detection/InstantiateAudit.cs | Опасные префабы не от хоста (BeeSwarm, Tornado, Bingbong_*, MushroomZombie, Character_Scoutmaster — честно их создаёт только хост; зомби игрока `MushroomZombie_Player` не трогаем) — при BlockDangerousSpawns у клиента не создаются, у хоста создаются и сразу удаляются у всех через ForeignObjects (`PhotonNetwork.Destroy` хосту чужое удалить не даёт); предмет, созданный не хостом: есть в слотах — notice, нет — подозрение (ниже y −400 — визуал рюкзака, пропуск)  Предмет не из инвентаря: если рядом (3 м) лежит/летит предмет создателя (ItemState.Ground — сломанный кокос, облачный гриб, гриб на полке, «предмет-обманка»: так игра честно создаёт предметы у клиента) — notice «got X out of an item they broke»; иначе красная находка «spawned X out of nowhere», и хост при BlockCheatSpawns удаляет его у всех (ForeignObjects). | Создания из кэша комнаты (время отправки старше 3 с — объекты, появившиеся до твоего входа) не проверяются. ||
| FlightExemptions | Detection/Flight/FlightExemptions.cs | Энергетик/амулеты прыжка (FasterBoi, DoubleJumpAmulet, MassSuperJump). Первые 15 с после появления персонажа (SpawnClock), аэропорт, кинематическое тело (игра держит персонажа), лазание, несут/держат/кондор, без сознания, упал (ragdoll, fallSeconds > 0 — тело может лежать на выступе, которого не видит проверка земли), шарики, планер/зонтик, LowGravity, зоны (EnvironmentMarks, 1,5 с), Rope Carabiner (RopeLinks: свойства комнаты `rkb.link.*`), стоит на игроке |
| FlightTrack / HorizontalSpeed | Detection/Flight/ | Высоты за окно; путь по XZ за окно, скачок > 20 м за сэмпл — телепорт, окно заново |

### Instruments — Protection
| Class | File | Role |
|---|---|---|
| HandGuard | Protection/HandGuard.cs | Не хост: свои RequestPickup / RPC_RequestFakeItemPickup / RPC_SpawnItemInHandMaster запоминаются на 5 с (патч PhotonView.RPC); OnPickupAccepted без такого запроса — не брать в руку и очистить слот (RefuseForcedPickups) |
| KickShield | Protection/KickShield.cs | RPCA_Kick запоминает пинающего на 2 с; его RPCA_Fall / RPCA_AddForceAtPosition по тебе отменяются (IgnoreKicks; с AllowHonestKicks — только при выключенных пинках в забеге); при выключенных пинках в забеге каждый пинок — находка; при защите всех упавшему шлётся RPCA_UnFall — только при выключенных пинках, а с ShieldEveryoneInKickMode и при честных пинках |
| DesyncFixer | Protection/DesyncFixer.cs | `Resync(player)` (кнопка Resync) — StateLedger.Reapply (только снимки состояния) + Fix тела. Известный баг PEAK: после захода тело другого игрока у тебя носится по карте и отбрасывает подошедших, у остальных всё нормально, помогает только перезаход. Раз в 0,25 с: тело у тебя дальше 8 м от его настоящей позиции (CharacterSyncer.RemoteValue.hipLocation) или быстрее 35 м/с и дальше 3 м — 1,5 с подряд → местная копия `Character.RagdollExorcism` разработчиков: удалить FixedJoint в рэгдолле, WarpPlayer в настоящую позицию локально, 3 с SnapToAnimation + демпфирование 20, затем lastPosition = удалённая позиция. Без сетевых команд; не чаще раза в 10 с на игрока; notice «snapped back locally». Кнопка Resync в Players — то же вручную. [Other] FixDesyncedPlayers (вкл.) |
| SentinelPresence | Protection/SentinelPresence.cs | MonoBehaviour: при входе в комнату публикует свойство игрока `peaksentinel.version`; через 3 с (как появится лента игры, но не дольше 20 с) и при каждой смене хоста пишет в ленту и лог: «you are the host: the lobby is protected by your Peak Sentinel» / «the host runs Peak Sentinel X: the lobby is protected» / «the host has no Peak Sentinel: the lobby is not protected». `SuspicionReport.Announce` = notice + строка в ленте игры. Лента игры: метка [Sentinel] всегда синяя, текст по смыслу — красный (чит, блок), оранжевый (подозрение, флуд, лобби без Sentinel), зелёный (защита сработала: лобби защищено, Fix). Отбитый пинок — красный, если пинки в забеге выключены (это чит), и оранжевый, если игра их разрешает. Тон задаёт `FeedTone` (Cheat, Suspect, Good). `Runs(player)` — стоит ли Sentinel у игрока (ты сам или есть метка): во вкладке Players рядом со смайликом голубой щит с галочкой (ShieldSprite, рисуется кодом) |
| HostGuard | Protection/HostGuard.cs | Подписка на события Photon (OnEnable/OnDisable): PropertiesChanged с ключом MasterClientId от игрока, который не был хостом, пока прежний хост ещё в комнате, — захват (так делают только читы, сервер Photon принимает смену хоста от любого и запретить её нельзя). Всем Sentinel-клиентам — находка Cheats «took the host from X», метка «steals the host». Если хостом был ты и ReclaimHost — сразу `SetMasterClient(себя)` (BLOCKED «taken back»), при KickHostThieves — кик вора, как только хост снова твой |
| PacketGate | Protection/PacketGate.cs | Префикс `PhotonNetwork.OnEvent`, до разбора PUN: события (коды < 220, серверные не трогает) от заглушённого игрока выбрасываются, считаются, а пакеты его состояния запоминаются в StateLedger; флуд (хоста не считает; не считает покадровые синхронизации своего состояния — SyncAfflictionsRPC, SyncStatusesRPC, SyncThornsRPC_Remote, игра шлёт их каждый кадр, пока меняется эффект, например у пламеники; не считается первые 15 с после входа в комнату и 20 с после смены участка — зажжённый костёр-чекпоинт, когда все клиенты разом шлют пакеты, и для пакетов старше 3 с — это кэш комнаты, который сервер выдаёт при входе разом) — больше [Flood] MaxRpcPerSecond команд (200), MaxSpawnsPerSecond спавнов (202) или MaxObjectEventsPerSecond, FloodSeconds удалений и смен владельца (204, 209, 210, 212) за секунду несколько секунд подряд ([Flood] FloodSeconds, 3 — игра сама даёт короткие честные всплески, например при входе игрока): подозрение (оранжевое) «floods the network for N s in a row» с реальным счётом за последнюю секунду (раз в 10 с), при MuteFlooders — mute; запрос или передача владения (209, 210) персонажем другого игрока — «tried to take control of the character of X», при BlockCharacterTakeover событие отбрасывается; удаление (204) объекта, который принадлежит другому игроку, не хостом — «destroyed … of X», при BlockObjectDestroy отбрасывается |
| MuteList | Protection/MuteList.cs | Заглушённые в этой комнате по аккаунту (UserId, иначе номер игрока): перезаход игрока mute не снимает, сброс — когда ты сменил комнату и число отброшенных пакетов; Mute / Unmute пишут notice в Other. Голос тоже глушится: громкость игрока в игровом AudioLevels ставится в 0, при Unmute и смене комнаты возвращается прежняя. Хоста заглушить нельзя |
| BanList | Protection/BanList.cs | Файл `BepInEx/config/PeakSentinel.bans.txt`, строка «UserId\tник»; `Export()` / `Import(text)` — обмен списком через буфер обмена (вкладка Bans: Copy list / Add from clipboard), импорт только добавляет новых |

### Reporting
| Class | File | Role | Key API |
|---|---|---|---|
| SuspicionReport | Reporting/SuspicionReport.cs | Одна точка записи находки | `Flag` (чит), `Suspect` (эвристика, к тексту дописывается «(heuristic, may be wrong)»), `Refuse(culprit, category, tag, action, block, repair) → allowed`, `Notice`, `Tag`, `Fix(finding)` — применить починку, отметить находку Fixed, добавить у себя зелёную строку «fix: …» (видна только тому, кто починил; другие Sentinel-клиенты честно видят его RPC как вмешательство) |
| FindingLog | Reporting/FindingLog.cs | Последние MaxLogEntries находок, метки и счётчики игроков | `Version, Count, Capacity, Resize, ProblemsOf, StandingOf` |
| Repair | Reporting/Repair.cs | Починка, привязанная к находке в момент детекта: подпись («revived X where they died») и действие (RPC всем) | `Action`, `Apply()` |
| FindingKind / FindingCategory / PlayerStanding | Reporting/ | Notice, Suspicious, Cheat, Blocked, Fix / Items, Cheats, Other / Clean, Suspect, Cheater | |
| PlayerNames | Reporting/PlayerNames.cs | Ник и номер игрока | `Name(p)`, `Actor(p)` |
| ReportExport | Reporting/ReportExport.cs | Кнопка Export на вкладке Log: все находки сессии в `BepInEx/PeakSentinel-report-<дата-время>.txt` |
| DebugTrace | Reporting/DebugTrace.cs | `BepInEx/PeakSentinel.log`: RPC, INSTANTIATE, FLIGHT, STAMINA, EFFECT, находки | |

### View
| Class | File | Role |
|---|---|---|
| ScopeHint, HintScope | Ui/ScopeHint.cs | Текст строки-подсказки «If on: … → кого защищает» по текущей роли (хост / не хост) |
| GameFonts | Ui/GameFonts.cs | Шрифт игры (FontFallbackSwapper.mainBaseFont, иначе TMP_Settings.defaultFontAsset) для окна и меток Esc |
| RichText | Ui/RichText.cs | `Paint(text, color)`, `Warning(text)` — оранжевая пометка в подписи галочки (эвристика, подозрительно для других античитов) |
| UiFactory, UiPalette, CardSprites, FaceSprites | Ui/ | Canvas 1920×1080, элементы в стиле ItemSpawnerPremium (пергамент, скругления генерируются кодом), шрифт игры (FontFallbackSwapper.mainBaseFont), смайлики статуса |
| AvatarBadge | Ui/AvatarBadge.cs | Заглушённый (MuteList) — аватарка серая и поверх тёмный «X» шрифтом игры. Метка в правом верхнем углу аватарки игрока в меню Esc (PlayerUI): оранжевый «?» — подозрения (Suspect), пульсирующий (DOTween) красный «!» — чит или блок (Cheater, приоритет выше); шрифт игры, без обводки и тени; рамка — дочерний объект аватарки, отражение аватарки по Y компенсируется |
| HotkeyButton | Ui/HotkeyButton.cs | Кнопка «Window key: F6» в шапке: клик → «Press a key... (Esc cancels)» → любая клавиша становится [Manager] ToggleKey |
| SettingsBinder | Ui/SettingsBinder.cs | Галочки ↔ ConfigEntry, пара «проверка + вторая галочка справа» (BindPair), строка-подсказка под галочкой (Hint, HintScope): «If on: <что будет>» + ярлык охвата по текущей роли — зелёный «protects everyone», «protects only you», «only writes to the log», оранжевый «does nothing now: you are not the host», «not needed while you are the host»; не хосту у функций, которые как хост защищают всех, дописывается зелёное «(as host: protects everyone)»; обновляется вместе с галочками, зависимые галочки (гаснут), пометки «только у хоста» |
| FeedOptions / FeedFilter / FindingFeed | Ui/ | Show notices, Follow newest, поиск (общий для вкладок), Clear; повторы одной находки за 30 с схлопываются в «×N (last …)»; лента кусками по 40 строк (лимит вершин TMP), цвет по виду находки, тексты в `<noparse>`; находка с починкой — отдельная строка с кнопкой Fix (не хосту — второе нажатие «Not host! Fix?»), после — «Fixed» |
| PlayerListView, AvatarCache | Ui/ | Строки игроков (аватарка Steam, смайлик и цвет статуса, Mute у всех, Kick/Ban у хоста с подтверждением), профиль с лентой игрока и числом отброшенных пакетов |
| PlayerActions / PlayerProfileView | Ui/ | Список (PlayerListView) и профиль игрока (PlayerProfileView) разделены; общие Mute/Kick/Ban с подтверждением, надписи кнопок, аватар и цвет статуса — PlayerActions |
| Pages: LogPage, ItemsPage, CheatsPage, OtherPage, PlayersPage, BansPage | Ui/Pages/ | В шапке окна (SentinelWindow.ShowLobby, обновляется с окном раз в 0,25 с) — строка состояния лобби: зелёная «Lobby protected» (ты хост или у хоста Sentinel по SentinelPresence.Runs), оранжевая «Lobby not protected: host X has no Peak Sentinel», «Not in a lobby». Вкладки окна (ITabPage), общий PageContext, FeedSection = опции + лента. На Log — легенда цветов: точки (DotSprite, рисуется кодом) красная — чит или блок, оранжевая — подозрение, зелёная — Fix, коричневая — информация |

### Data / Settings
| Class | Section | What it holds |
|---|---|---|
| FloodSettings | [Flood] | MaxRpcPerSecond 60, MaxSpawnsPerSecond 20, MaxObjectEventsPerSecond, FloodSeconds 3, MaxBuildsPerMinute 6 |
| ManagerSettings | [Manager] | ToggleKey (F6), MaxLogEntries (500, 50–5000) |
| ProtectionSettings | [Protection] | BlockCheatSpawns, AllowMySpawns, RefuseForcedPickups (все вкл.) |
| DetectionSettings | [Cheat detection] | HarmToOthers + BlockHarmToOthers, HostOnlyEvents + BlockHostOnlyEvents, DangerousSpawns + BlockDangerousSpawns, Flight + BlockFlight (выкл.), Stamina, SourcelessEffects (оба без блока), ForcedActions + BlockForcedActions, ObjectDestroy + BlockObjectDestroy, BuildSpam + BlockBuildSpam, NameSpoofing, Flood + MuteFlooders (выкл.), CharacterTakeover + BlockCharacterTakeover, CheatModMarkers + KickCheatModUsers |
| OtherSettings | [Other] | FixDesyncedPlayers (вкл.); ReclaimHost (вкл.) + KickHostThieves (вкл.); IgnoreKicks (+ AllowHonestKicks, вкл. по умолчанию: сама выключается при включённых пинках в забеге) → ShieldEveryoneFromKicks → ShieldEveryoneInKickMode / ShieldEveryoneFromKicksAsClient (цепочка, все выкл.) |
| StaminaSettings | [Stamina] | FlagSeconds 4, MinDrop 0.02, MinSprintSpeed 3, TraceSamples |
| FlightSettings | [Flight] | MinGroundDistance, GroundProbeRadius (0.5), WindowSeconds, MaxGravityDrop, FastFallSpeed, FlagSeconds, MaxRunSpeed, SpeedWindowSeconds, SpeedFlagSeconds, TraceSamples |
| DebugSettings | [Debug] | TraceFile (выкл. по умолчанию — за долгую игру трейс вырастает до десятков МБ), TraceAllRpcs |

## Data Flow
```
Photon RPC → NetworkPatches.ExecuteRpc → RpcRouter → IRpcAudit.Inspect(RpcCall) → SuspicionReport → FindingLog → FindingFeed / PlayerListView
                                                                         └→ false = RPC не выполняется на этом клиенте
NetworkInstantiate → InstantiateAudit.AllowCreate / Inspect → SuspicionReport
FlightAudit.Update → FlightExemptions + FlightTrack → SuspicionReport.Suspect (+ WarpPlayerRPC при Snap back)
```

## Entry Points
| Trigger | Who fires | Who handles |
|---|---|---|
| Любое входящее/локальное RPC | Photon (`PhotonNetwork.ExecuteRpc`) | RpcRouter |
| Своё исходящее RPC | `PhotonView.RPC` | HandGuard.NoteOutgoing |
| Сетевое создание объекта | `PhotonNetwork.NetworkInstantiate` | InstantiateAudit |
| Клавиша окна | SentinelWindow.Update | открыть/закрыть или захват клавиши |
| Таймеры | FlightAudit, StaminaAudit, CheatModScan, BanGuard | сами |
| Событие Photon PropertiesChanged | NetworkingClient.EventReceived | HostGuard |

## Cross-Feature Connections
| This feature uses | From | Via |
|---|---|---|
| Привязка Rope Carabiner | мод Rope Carabiner | свойства комнаты `rkb.link.<a>_<b>` = «1\|1\|…» |
| Кик | игра | `PlayerHandler.Kick`, `NetCode.Session.Kick` |
| Аватарки | игра | `PlatformManager.Platform.GetAvatarAsync` |

## Правила ItemRpcAudit
| RPC | Кто вправе | Что пишет |
|---|---|---|
| `RPC_SpawnItemInHandMaster` | никто; себе — только паспорт в аэропорту и стартовый предмет Grapple | «spawned X into the hands of Y» / «into own hands» |
| `RequestPickup` | владелец персонажа из аргумента или хост | «made Y pick up X» |
| `OnPickupAccepted` | хост | «forced an item pickup on Y» |
| `SyncInventoryRPC` | хост; с forceSync=true — только владелец в первые 15 с после появления своего персонажа (CharacterSpawner), иначе (кроме обмена рюкзаком Piggyback, см. BackpackSwap) это выдача себе предметов: хост отказывает и сразу шлёт ему настоящий инвентарь (`SyncInventoryRPC` хоста, как в ванили), так что выданное пропадает и у самого читера; с forceSync=false — первые 15 с после нашего входа в комнату (при входе все клиенты шлют новичку инвентари) | «forced a new inventory onto Y (item give cheat)» / «sent a fake inventory of Y» |
| `RPC_SetInventory`, `RPCRemoveItemFromSlot` | владелец или хост | «rewrote the inventory of Y» |
| `RPC_InitHoldingItem`, `EquipSlotRpc` | владелец или хост | «forced an item into the hands of Y» |
| `RPC_Clone` | — | подозрение «cloned an item» |

## Ограничения
- Видно только то, что дошло до этого клиента: RPC хосту видит только хост; клиенту не видно, кто попросил хоста заспавнить предмет.
- Хост-читер не ловится (хост создаёт предметы сам).
- Каким модом сделан спавн, не определить: спавнеры идут через ту же ванильную команду.
- Сборка кладёт DLL в профиль Thunderstore Default: `BepInEx/plugins/PeakSentinel`. Мод — одна `PeakSentinel.dll`; архив для Thunderstore / друзей собирает `tools/Pack-Release.ps1` в `dist/PeakSentinel-<версия>-thunderstore.zip`.

## Reuse Candidates
| Class | Why reusable |
|---|---|
| RpcCall, RpcRouter, IRpcAudit | любой аудит Photon RPC в PEAK-модах |
| UiFactory, CardSprites, UiPalette | окно в стиле ItemSpawnerPremium для любого мода |
| HotkeyButton | выбор клавиши в любом окне |
