# PeakSentinel — Feature Index

> Анти-чит для PEAK (BepInEx 5): по сетевым сообщениям Photon определяет, КТО сделал запрещённое действие (отправителя RPC ставит сервер, подделать его нельзя), пишет это в лог, окно F6 и ленту подключений и по настройкам блокирует или защищает.

## Layer Map
```
SentinelPlugin                          — composition root: настройки, отчёт, аудиты, хост-объект, Harmony
├── Patches/NetworkPatches              — ExecuteRpc → RpcRouter; NetworkInstantiate → InstantiateAudit; PhotonView.RPC → HandGuard; курсор окна
├── Patches/PauseMenuPatches            — PlayerUI.Initialize → StandingBadge на аватарке в меню Esc
├── Patches/EnvironmentPatches          — AntiSphere / WindChillZone / PlayerMoveZone помечают персонажей для FlightExemptions
├── Network/RpcRouter                   — разбирает RPC в RpcCall, пишет трассу, опрашивает все IRpcAudit
│   ├── Detection/ItemRpcAudit          — выдача и подмена предметов
│   ├── Protection/HandGuard            — подсунутый в руки предмет (клиент)
│   ├── Protection/KickShield           — пинки
│   ├── Detection/HarmAudit             — вред другим игрокам
│   ├── Detection/WorldAudit            — события, которые вправе слать только хост
│   ├── Detection/ReviveAudit           — воскрешение без источника (само-рес)
│   └── Detection/EffectAudit           — защита / бесконечная стамина без источника
├── Detection/InstantiateAudit          — сетевое создание предметов и опасных префабов
├── Detection/CheatModScan              — метки Cherry / Atlas
├── Detection/Stamina/StaminaAudit      — стамина не тратится (+ StaminaTrack)
├── Detection/Flight/FlightAudit        — полёт и сверхскорость
│   ├── FlightTrack, HorizontalSpeed    — история высот и пути
│   └── FlightExemptions                — законные причины быть в воздухе (RopeLinks, EnvironmentMarks)
├── Protection/BanGuard + BanList       — постоянный бан
├── Protection/HostGuard                — захват хоста: запись, возврат, кик
├── Reporting/SuspicionReport           — запись находки: BepInEx-лог, DebugTrace, FindingLog, лента подключений
└── Ui/SentinelWindow                   — окно F6 (Back)
    ├── HotkeyButton, SettingsBinder, FeedOptions, FindingFeed, PlayerListView
    └── Pages/ Log, Items, Cheats, Other, Players, Bans
```

## Classes

### Back
| Class | File | Role |
|---|---|---|
| SentinelPlugin | SentinelPlugin.cs | Создаёт настройки, отчёт, BanList, связывает RpcRouter со всеми аудитами, вешает MonoBehaviour на скрытый DontDestroyOnLoad-объект, Harmony PatchAll |
| SentinelWindow | Ui/SentinelWindow.cs | Окно F6: клавиша открытия (или захват новой клавиши через HotkeyButton), Canvas строится при первом открытии и заново при смене шрифта, свой EventSystem если в сцене нет, вкладки, обновление открытой вкладки 4 раза в секунду; `IsOpen` читает патч курсора |
| FlightAudit | Detection/Flight/FlightAudit.cs | 10 раз в секунду по каждому чужому персонажу: высота (своя сфера радиусом GroundProbeRadius вниз по TerrainMap — тонкий луч промахивался мимо уступа, когда игрок стоял краем ступни), вертикальная скорость по позициям, горизонтальная скорость; находки и откат «Snap back» |
| StaminaAudit | Detection/Stamina/StaminaAudit.cs | 4 раза в секунду по каждому чужому персонажу: стамина (обычная + бонусная) из CharacterSyncer, пока игрок лезет (стена, верёвка, лиана) или бежит (isSprinting, на земле, быстрее MinSprintSpeed); если за усилие стамина не опустилась ниже минимума на MinDrop дольше FlagSeconds — подозрение «stamina does not go down», метка «infinite stamina», раз за усилие; не проверяется: мёртв/без сознания, аэропорт, эффекты InfiniteStamina/FasterBoi, стамина пуста; блокировать нельзя — стамина считается на клиенте игрока; строки STAMINA в PeakSentinel.log |
| CheatModScan | Detection/CheatModScan.cs | Раз в 5 с: свойства игроков CherryUser/CherryOwner/AtlUser/AtlOwner, хост кикает при KickCheatModUsers |
| BanGuard | Protection/BanGuard.cs | Раз в секунду у хоста кикает игроков из BanList; `Ban(player)` |

### Network
| Class | File | Role | Key API |
|---|---|---|---|
| RpcRouter | Network/RpcRouter.cs | Hashtable RPC → RpcCall (ключи 0 view, 3 имя, 4 аргументы, 5 индекс в RpcList); трасса, если RPC кто-то смотрит или TraceAllRpcs; результат — И всех аудитов | `bool Inspect(Hashtable, Player)` |
| IRpcAudit | Network/IRpcAudit.cs | Контракт аудита RPC | `bool Watches(string)`, `bool Inspect(RpcCall)` — «выполнять ли» |
| RpcCall | Network/RpcCall.cs | Данные вызова и разбор аргументов | `Sender, View, Owner, FromHost, FromOwner, FromLocal, Argument/StringAt/MagnitudeAt/ViewAt(i), SentBy(p), Component<T>()` |
| RpcNames | Network/RpcNames.cs | Имена ванильных RPC | const string |

### Instruments — Detection
| Class | File | Role |
|---|---|---|
| ItemRpcAudit | Detection/ItemRpcAudit.cs | Правила предметов (таблица ниже); блок у хоста при BlockCheatSpawns, свои запросы хоста не блокируются при AllowMySpawns |
| HarmAudit | Detection/HarmAudit.cs | Свои RPC не проверяет (это наши починки). Починки: убийство — `RPCA_ReviveAtPosition` в месте смерти, обморок — `RPCA_UnPassOut`, телепорт и рывок — `WarpPlayerRPC` туда, где жертва стояла перед атакой; зомби/окаменение — без починки. Отправитель не владелец персонажа и не хост: Die/SetDead — убил, PassOut — обморок, Zombify/Petrify, WarpPlayerRPC — телепорт, сила ≥ 1500 — рывок (законные удары до ~500). RPCA_Fall не проверяется — его законно шлют чужие клиенты |
| WorldAudit | Detection/WorldAudit.cs | Свои RPC не проверяет. Починки: потушенный костёр — `Light_Rpc` с прежним временем горения (кроме костра конца мини-забега), туман — `RPCA_SyncFog` с прежними размером и движением; вертолёт, конец игры, мосты, верёвки — без починки. Таблица как в PEAKER (метод → компонент на view, кто вправе: хост / владелец / жертва): банан, пчёлы, вертолёт (кроме законного), костёр, конец игры, призрак, извержение, лиана, туман, старт/возврат, мосты, верёвки, сосульки, крюки |
| ReviveAudit | Detection/ReviveAudit.cs | MonoBehaviour + IRpcAudit, группа HostOnlyEvents/BlockHostOnlyEvents. `RPCA_ReviveAtPosition` честно шлют: хост (статуя-сундук, Пустота, случайное воскрешение), сам воскрешаемый в первые 20 с после появления его персонажа (заход в забег), игрок с чучелом скаута (ScoutEffigy) или Книгой костей (тег BookOfBones) в руке не дальше 12 м от точки воскрешения. Иначе — «revived themselves / revived X with no source», метка «revives without a source»; с Block не выполняется у тебя. Свои RPC (наши Fix) не проверяет |
| EffectAudit | Detection/EffectAudit.cs | MonoBehaviour + IRpcAudit. `Consume(consumerID)` от любого — запоминает, кто что съел; `SyncAfflictionsRPC(data, newType)` от владельца с новым эффектом Invincibility (молоко, амулет, гриб), BingBongShield или InfiniteStamina — ищет источник: щит — золотой Bing Bong (BingBongShieldWhileHolding) в руке сейчас; остальное — съедено что-то в пределах 5 с (решение через 2 с: «съел» может прийти позже эффекта). Нет источника — подозрение «got X with no source», метка; строка EFFECT в трассе с названием съеденного |
| InstantiateAudit | Detection/InstantiateAudit.cs | Опасные префабы не от хоста (BeeSwarm, Tornado, Bingbong_*) — не создаются при BlockDangerousSpawns; предмет, созданный не хостом: есть в слотах — notice, нет — подозрение (ниже y −400 — визуал рюкзака, пропуск) |
| FlightExemptions | Detection/Flight/FlightExemptions.cs | Лазание, несут/держат/кондор, без сознания, шарики, планер/зонтик, LowGravity, зоны (EnvironmentMarks, 1,5 с), Rope Carabiner (RopeLinks: свойства комнаты `rkb.link.*`), стоит на игроке |
| FlightTrack / HorizontalSpeed | Detection/Flight/ | Высоты за окно; путь по XZ за окно, скачок > 20 м за сэмпл — телепорт, окно заново |

### Instruments — Protection
| Class | File | Role |
|---|---|---|
| HandGuard | Protection/HandGuard.cs | Не хост: свои RequestPickup / RPC_RequestFakeItemPickup / RPC_SpawnItemInHandMaster запоминаются на 5 с (патч PhotonView.RPC); OnPickupAccepted без такого запроса — не брать в руку и очистить слот (RefuseForcedPickups) |
| KickShield | Protection/KickShield.cs | RPCA_Kick запоминает пинающего на 2 с; его RPCA_Fall / RPCA_AddForceAtPosition по тебе отменяются (IgnoreKicks; с AllowHonestKicks — только при выключенных пинках в забеге); при выключенных пинках в забеге каждый пинок — находка; при защите всех упавшему шлётся RPCA_UnFall — только при выключенных пинках, а с ShieldEveryoneInKickMode и при честных пинках |
| HostGuard | Protection/HostGuard.cs | Подписка на события Photon (OnEnable/OnDisable): PropertiesChanged с ключом MasterClientId от игрока, который не был хостом, пока прежний хост ещё в комнате, — захват (так делают только читы, сервер Photon принимает смену хоста от любого и запретить её нельзя). Всем Sentinel-клиентам — находка Cheats «took the host from X», метка «steals the host». Если хостом был ты и ReclaimHost — сразу `SetMasterClient(себя)` (BLOCKED «taken back»), при KickHostThieves — кик вора, как только хост снова твой |
| BanList | Protection/BanList.cs | Файл `BepInEx/config/PeakSentinel.bans.txt`, строка «UserId\tник» |

### Reporting
| Class | File | Role | Key API |
|---|---|---|---|
| SuspicionReport | Reporting/SuspicionReport.cs | Одна точка записи находки | `Flag` (чит), `Suspect` (эвристика, к тексту дописывается «(heuristic, may be wrong)»), `Refuse(culprit, category, tag, action, block, repair) → allowed`, `Notice`, `Tag`, `Fix(finding)` — применить починку, отметить находку Fixed, добавить у себя зелёную строку «fix: …» (видна только тому, кто починил; другие Sentinel-клиенты честно видят его RPC как вмешательство) |
| FindingLog | Reporting/FindingLog.cs | Последние MaxLogEntries находок, метки и счётчики игроков | `Version, Count, Capacity, Resize, ProblemsOf, StandingOf` |
| Repair | Reporting/Repair.cs | Починка, привязанная к находке в момент детекта: подпись («revived X where they died») и действие (RPC всем) | `Action`, `Apply()` |
| FindingKind / FindingCategory / PlayerStanding | Reporting/ | Notice, Suspicious, Cheat, Blocked, Fix / Items, Cheats, Other / Clean, Suspect, Cheater | |
| PlayerNames | Reporting/PlayerNames.cs | Ник и номер игрока | `Name(p)`, `Actor(p)` |
| DebugTrace | Reporting/DebugTrace.cs | `BepInEx/PeakSentinel.log`: RPC, INSTANTIATE, FLIGHT, STAMINA, EFFECT, находки | |

### View
| Class | File | Role |
|---|---|---|
| ScopeHint, HintScope | Ui/ScopeHint.cs | Текст строки-подсказки «If on: … → кого защищает» по текущей роли (хост / не хост) |
| GameFonts | Ui/GameFonts.cs | Шрифт игры (FontFallbackSwapper.mainBaseFont, иначе TMP_Settings.defaultFontAsset) для окна и меток Esc |
| RichText | Ui/RichText.cs | `Paint(text, color)`, `Warning(text)` — оранжевая пометка в подписи галочки (эвристика, подозрительно для других античитов) |
| UiFactory, UiPalette, CardSprites, FaceSprites | Ui/ | Canvas 1920×1080, элементы в стиле ItemSpawnerPremium (пергамент, скругления генерируются кодом), шрифт игры (FontFallbackSwapper.mainBaseFont), смайлики статуса |
| StandingBadge | Ui/StandingBadge.cs | Метка в правом верхнем углу аватарки игрока в меню Esc (PlayerUI): оранжевый «?» — подозрения (Suspect), пульсирующий (DOTween) красный «!» — чит или блок (Cheater, приоритет выше); шрифт игры, без обводки и тени; рамка — дочерний объект аватарки, отражение аватарки по Y компенсируется |
| HotkeyButton | Ui/HotkeyButton.cs | Кнопка «Window key: F6» в шапке: клик → «Press a key... (Esc cancels)» → любая клавиша становится [Manager] ToggleKey |
| SettingsBinder | Ui/SettingsBinder.cs | Галочки ↔ ConfigEntry, пара «проверка + вторая галочка справа» (BindPair), строка-подсказка под галочкой (Hint, HintScope): «If on: <что будет>» + ярлык охвата по текущей роли — зелёный «protects everyone», «protects only you», «only writes to the log», оранжевый «does nothing now: you are not the host», «not needed while you are the host»; не хосту у функций, которые как хост защищают всех, дописывается зелёное «(as host: protects everyone)»; обновляется вместе с галочками, зависимые галочки (гаснут), пометки «только у хоста» |
| FeedOptions / FeedFilter / FindingFeed | Ui/ | Show notices, Follow newest, поиск (общий для вкладок), Clear; лента кусками по 40 строк (лимит вершин TMP), цвет по виду находки, тексты в `<noparse>`; находка с починкой — отдельная строка с кнопкой Fix (не хосту — второе нажатие «Not host! Fix?»), после — «Fixed» |
| PlayerListView, AvatarCache | Ui/ | Строки игроков (аватарка Steam, смайлик и цвет статуса, Kick/Ban с подтверждением), профиль с лентой игрока |
| Pages: LogPage, ItemsPage, CheatsPage, OtherPage, PlayersPage, BansPage | Ui/Pages/ | Вкладки окна (ITabPage), общий PageContext, FeedSection = опции + лента |

### Data / Settings
| Class | Section | What it holds |
|---|---|---|
| ManagerSettings | [Manager] | ToggleKey (F6), MaxLogEntries (500, 50–5000) |
| ProtectionSettings | [Protection] | BlockCheatSpawns, AllowMySpawns, RefuseForcedPickups (все вкл.) |
| DetectionSettings | [Cheat detection] | HarmToOthers + BlockHarmToOthers, HostOnlyEvents + BlockHostOnlyEvents, DangerousSpawns + BlockDangerousSpawns, Flight + BlockFlight (выкл.), Stamina, SourcelessEffects (оба без блока), CheatModMarkers + KickCheatModUsers |
| OtherSettings | [Other] | ReclaimHost (вкл.) + KickHostThieves (выкл.); IgnoreKicks (+ AllowHonestKicks: сама выключается при включённых пинках в забеге) → ShieldEveryoneFromKicks → ShieldEveryoneInKickMode / ShieldEveryoneFromKicksAsClient (цепочка, все выкл.) |
| StaminaSettings | [Stamina] | FlagSeconds 4, MinDrop 0.02, MinSprintSpeed 3, TraceSamples |
| FlightSettings | [Flight] | MinGroundDistance, GroundProbeRadius (0.5), WindowSeconds, MaxGravityDrop, FastFallSpeed, FlagSeconds, MaxRunSpeed, SpeedWindowSeconds, SpeedFlagSeconds, TraceSamples |
| DebugSettings | [Debug] | TraceFile, TraceAllRpcs |

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
| `SyncInventoryRPC`, `RPC_SetInventory`, `RPCRemoveItemFromSlot` | владелец или хост | «rewrote the inventory of Y» |
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
