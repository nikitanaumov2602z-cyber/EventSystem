using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using MEC;
using UnityEngine;
using Newtonsoft.Json;

namespace EventSystem
{
    public class EventHandler
    {
        private readonly EventPlugin plugin;
        private readonly HttpClient httpClient = new HttpClient();
        private readonly Random random = new Random();

        // ---- Основные поля ----
        private bool isEventActive = false;
        private string eventName;
        private string rpLevel;
        private Player host;
        private readonly List<Player> helpers = new List<Player>();
        private DateTime eventStartTime;
        private bool rpMode = false;
        private DateTime roundStartTime;

        // ---- Роли ----
        private readonly HashSet<Player> roleGunOwners = new HashSet<Player>();
        private readonly Dictionary<Player, CustomRole> selectedCustomRole = new Dictionary<Player, CustomRole>();
        private readonly List<CustomRole> customRoles = new List<CustomRole>();
        private readonly Dictionary<string, int> nameCounters = new Dictionary<string, int>();
        private readonly Dictionary<Player, string> originalNames = new Dictionary<Player, string>();

        // ---- RP-механики ----
        private readonly Dictionary<Player, CoroutineHandle> healingCoroutines = new Dictionary<Player, CoroutineHandle>();
        private readonly Dictionary<Player, CoroutineHandle> hackingGateCoroutines = new Dictionary<Player, CoroutineHandle>();
        private readonly Dictionary<Player, CoroutineHandle> hackingIntercomCoroutines = new Dictionary<Player, CoroutineHandle>();
        private DateTime intercomHackEndTime = DateTime.MinValue;
        private bool isIntercomHacked = false;
        private Player intercomHacker = null;
        private readonly Dictionary<Player, DateTime> deadPlayers = new Dictionary<Player, DateTime>();
        private readonly Dictionary<Player, string> rpNames = new Dictionary<Player, string>();

        // ---- Обыск и изъятие ----
        private readonly Dictionary<Player, DateTime> lastSearchTime = new Dictionary<Player, DateTime>();
        private readonly Dictionary<Player, CoroutineHandle> searchCoroutines = new Dictionary<Player, CoroutineHandle>();
        private readonly Dictionary<Player, DateTime> stealCooldown = new Dictionary<Player, DateTime>();

        // ---- Рация ----
        private readonly Dictionary<Player, int> radioFrequencies = new Dictionary<Player, int>();
        private readonly Dictionary<Player, string> radioCodes = new Dictionary<Player, string>();

        // ---- ПНВ ----
        private readonly Dictionary<Player, bool> nvgStates = new Dictionary<Player, bool>();
        private readonly Dictionary<Player, float> nvgBattery = new Dictionary<Player, float>();
        private readonly Dictionary<Player, CoroutineHandle> nvgCoroutines = new Dictionary<Player, CoroutineHandle>();

        // ---- Кандалы ----
        private readonly Dictionary<Player, Player> shackledPlayers = new Dictionary<Player, Player>();
        private readonly Dictionary<Player, DateTime> shackleStartTime = new Dictionary<Player, DateTime>();
        private readonly Dictionary<Player, CoroutineHandle> shackleCoroutines = new Dictionary<Player, CoroutineHandle>();
        private readonly HashSet<Player> shackleCooldown = new HashSet<Player>();

        // ---- Клетка ----
        private bool isCageBuilding = false;
        private Player cageTarget = null;
        private readonly Dictionary<Player, DateTime> cageViewers = new Dictionary<Player, DateTime>();
        private CoroutineHandle cageBuildCoroutine;
        private GameObject cageObject = null;
        private readonly Dictionary<Player, bool> draggingPlayers = new Dictionary<Player, bool>();
        private Player dragLeader = null;
        private CoroutineHandle dragCoroutine;
        private bool isCagePlaced = false;

        // ---- Маска ----
        private readonly Dictionary<Player, bool> maskedPlayers = new Dictionary<Player, bool>();

        // ---- Робот-пылесос ----
        private GameObject roombaObject;
        private CoroutineHandle roombaCoroutine;
        private bool roombaActive = false;
        private Vector3 roombaDestination;

        // ---- SCP-079 / СБК / Камеры ----
        private readonly Dictionary<Player, bool> scp079Authorized = new Dictionary<Player, bool>();
        private readonly Dictionary<Player, int> currentCamera = new Dictionary<Player, int>();
        private readonly List<Room> cameraRooms = new List<Room>();
        private readonly Dictionary<Player, CoroutineHandle> cameraLoop = new Dictionary<Player, CoroutineHandle>();
        private readonly Dictionary<Player, bool> cameraViewActive = new Dictionary<Player, bool>();

        private readonly Dictionary<Player, bool> secModeActive = new Dictionary<Player, bool>();
        private readonly Dictionary<Player, int> secLevel = new Dictionary<Player, int>();
        private readonly Dictionary<Player, float> secEnergy = new Dictionary<Player, float>();
        private readonly Dictionary<Player, int> secExperience = new Dictionary<Player, int>();
        private readonly Dictionary<Player, DateTime> secLastExpGain = new Dictionary<Player, DateTime>();
        private readonly Dictionary<Player, DateTime> secCooldowns = new Dictionary<Player, DateTime>();
        private readonly Dictionary<Player, CoroutineHandle> secPassiveCoroutine = new Dictionary<Player, CoroutineHandle>();

        // ---- Swap ----
        private readonly Dictionary<Player, Player> swapRequests = new Dictionary<Player, Player>();
        private readonly Dictionary<Player, DateTime> swapRequestTime = new Dictionary<Player, DateTime>();

        // ---- Судебная система ----
        private bool courtInProgress = false;
        private Player courtJudge = null;
        private Player courtDefendant = null;
        private Player courtProsecutor = null;
        private string courtCaseName = "";
        private DateTime courtStartTime;
        private CoroutineHandle courtCoroutine;
        private readonly Dictionary<Player, string> playerSentences = new Dictionary<Player, string>();
        private readonly Dictionary<Player, bool> playerAppealed = new Dictionary<Player, bool>();
        private readonly Dictionary<Player, DateTime> appealTime = new Dictionary<Player, DateTime>();

        // ---- Копирование 079 ----
        private readonly Dictionary<Player, DateTime> copy079Cooldown = new Dictionary<Player, DateTime>();
        private bool copy079InProgress = false;
        private Player copy079Initiator = null;
        private CoroutineHandle copy079Coroutine;
        private bool copy079GateBlocked = false;

        // ---- Предупреждения ----
        private readonly Dictionary<Player, List<string>> playerWarnings = new Dictionary<Player, List<string>>();
        private readonly Dictionary<Player, DateTime> warnCooldown = new Dictionary<Player, DateTime>();

        // ---- Call & Report ----
        private readonly Dictionary<Player, DateTime> callCooldown = new Dictionary<Player, DateTime>();
        private readonly Dictionary<Player, DateTime> reportCooldown = new Dictionary<Player, DateTime>();

        // ---- Система документов ----
        private readonly Dictionary<Player, List<Document>> playerDocuments = new Dictionary<Player, List<Document>>();
        private int documentIdCounter = 0;

        // RP
        private readonly Dictionary<Player, string> playerAppearance = new Dictionary<Player, string>();
        private readonly Dictionary<Player, string> playerWeaponDesc = new Dictionary<Player, string>();
        private readonly Dictionary<string, (string Appearance, string WeaponDesc)> roleRPInfo = new Dictionary<string, (string, string)>();

        // ---- HUD ----
        private CoroutineHandle hudCoroutine;

        private class Document
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public Player Author { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // ---- Конструктор ----
        public EventHandler(EventPlugin plugin)
        {
            this.plugin = plugin;
            rpMode = plugin.Config.RPEnabledByDefault;

            Exiled.Events.Handlers.Player.ConsoleCommand += OnConsoleCommand;
            Exiled.Events.Handlers.Player.RemoteAdminCommand += OnRemoteAdminCommand;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;
            Exiled.Events.Handlers.Player.Jumping += OnJumping;
            Exiled.Events.Handlers.Player.Speaking += OnSpeaking;
            Exiled.Events.Handlers.Player.UsingItem += OnUsingItem;
            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            Exiled.Events.Handlers.Player.IntercomSpeaking += OnIntercomSpeaking;
            Exiled.Events.Handlers.Player.ChangedMovementState += OnChangedMovementState;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.Left += OnLeft;
            Exiled.Events.Handlers.Player.Verified += OnVerified;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Scp049.Attacking += OnScp049Attacking;
            Exiled.Events.Handlers.Scp096.Enraging += OnScp096Enraging;
            Exiled.Events.Handlers.Scp096.Calm += OnScp096Calm;
            Exiled.Events.Handlers.Scp173.BeingLookedAt += OnScp173BeingLookedAt;
            Exiled.Events.Handlers.Map.Explosion += OnExplosion;

            LoadCustomRoles();
        }

        ~EventHandler()
        {
            Exiled.Events.Handlers.Player.ConsoleCommand -= OnConsoleCommand;
            Exiled.Events.Handlers.Player.RemoteAdminCommand -= OnRemoteAdminCommand;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;
            Exiled.Events.Handlers.Player.Jumping -= OnJumping;
            Exiled.Events.Handlers.Player.Speaking -= OnSpeaking;
            Exiled.Events.Handlers.Player.UsingItem -= OnUsingItem;
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
            Exiled.Events.Handlers.Player.IntercomSpeaking -= OnIntercomSpeaking;
            Exiled.Events.Handlers.Player.ChangedMovementState -= OnChangedMovementState;
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Player.Verified -= OnVerified;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Scp049.Attacking -= OnScp049Attacking;
            Exiled.Events.Handlers.Scp096.Enraging -= OnScp096Enraging;
            Exiled.Events.Handlers.Scp096.Calm -= OnScp096Calm;
            Exiled.Events.Handlers.Scp173.BeingLookedAt -= OnScp173BeingLookedAt;
            Exiled.Events.Handlers.Map.Explosion -= OnExplosion;

            if (hudCoroutine.IsRunning)
                Timing.KillCoroutines(hudCoroutine);
        }

       

        private void OnConsoleCommand(ConsoleCommandEventArgs ev)
        {
            if (!plugin.Config.IsEnabled || ev.Player == null) return;
            string[] args = ev.Command.Split(' ');
            if (args.Length == 0) return;
            string cmd = args[0].ToLower();

            // Event commandes
            if (cmd.StartsWith("ev"))
            {
                var subArgs = args.Skip(1).ToArray();
                if (subArgs.Length == 0) { SendHelp(ev.Player); return; }
                switch (subArgs[0].ToLower())
                {
                    case "start": HandleStart(ev.Player, subArgs.Skip(1).ToArray()); break;
                    case "end": HandleEnd(ev.Player); break;
                    case "give": HandleGive(ev.Player, subArgs.Skip(1).ToArray()); break;
                    case "helper": HandleHelper(ev.Player, subArgs.Skip(1).ToArray()); break;
                    case "removehelper": HandleRemoveHelper(ev.Player, subArgs.Skip(1).ToArray()); break;
                    case "list": HandleList(ev.Player); break;
                    case "role": HandleRoleGun(ev.Player); break;
                    case "next": HandleNextRole(ev.Player); break;
                    case "prev": HandlePrevRole(ev.Player); break;
                    case "select": HandleSelectRole(ev.Player, subArgs.Skip(1).ToArray()); break;
                    case "roles": HandleListRoles(ev.Player); break;
                    case "rp": HandleRP(ev.Player, subArgs.Skip(1).ToArray()); break;
                    default: SendHelp(ev.Player); break;
                }
                return;
            }

            // --- Команды без точки ---
            if (cmd == "cinfo") { HandleCInfo(ev.Player); return; }
            if (cmd == "teamhealth") { HandleTeamHealth(ev.Player); return; }
            if (cmd == "hackintercom") { HandleHackIntercom(ev.Player); return; }
            if (cmd == "setname") { HandleSetName(ev.Player, args.Skip(1).ToArray()); return; }
            if (cmd == "setrpname") { HandleSetRPName(ev.Player, args.Skip(1).ToArray()); return; }
            if (cmd == "ooc") { HandleOOC(ev.Player, args.Skip(1).ToArray()); return; }
            if (cmd == "looc") { HandleLOOC(ev.Player, args.Skip(1).ToArray()); return; }
            if (cmd == "help") { SendHelp(ev.Player); return; }

            // --- Команды с точкой ---
            if (cmd == ".search")
            {
                if (args.Length > 1 && args[1].ToLower() == "fast") HandleSearchFast(ev.Player);
                else HandleSearch(ev.Player);
                return;
            }
            if (cmd == ".impound") { HandleImpound(ev.Player, args); return; }
            if (cmd == ".steal") { HandleSteal(ev.Player, args); return; }
            if (cmd == ".inventory") { HandleInventory(ev.Player); return; }
            if (cmd == ".radio")
            {
                if (args.Length < 2) { ev.Player.SendConsoleMessage("Используйте connect/code/info", "yellow"); return; }
                switch (args[1].ToLower())
                {
                    case "connect": HandleRadioConnect(ev.Player, args.Skip(2).ToArray()); break;
                    case "code": HandleRadioCode(ev.Player, args.Skip(2).ToArray()); break;
                    case "info": HandleRadioInfo(ev.Player); break;
                    default: ev.Player.SendConsoleMessage("Используйте connect/code/info", "yellow"); break;
                }
                return;
            }
            if (cmd == ".nvg")
            {
                if (args.Length > 1 && args[1].ToLower() == "charge") HandleNVGCharge(ev.Player, args.Skip(2).ToArray());
                else HandleNVG(ev.Player);
                return;
            }
            if (cmd == ".shackle") { HandleShackle(ev.Player); return; }
            if (cmd == ".cage") { HandleCage(ev.Player); return; }
            if (cmd == ".drag") { HandleDrag(ev.Player); return; }
            if (cmd == ".drop") { HandleDrop(ev.Player); return; }
            if (cmd == ".evacuate") { HandleEvacuate(ev.Player); return; }
            if (cmd == ".mask") { HandleMask(ev.Player); return; }
            if (cmd == ".copy" || cmd == ".копия") { HandleCopy079(ev.Player); return; }
            if (cmd == ".auth") { HandleAuth(ev.Player); return; }
            if (cmd == ".camera")
            {
                if (args.Length < 2) { ev.Player.SendConsoleMessage("Используйте list/view/next/prev/exit", "yellow"); return; }
                switch (args[1].ToLower())
                {
                    case "list": HandleCameraList(ev.Player); break;
                    case "view": HandleCameraView(ev.Player); break;
                    case "next": HandleCameraNext(ev.Player); break;
                    case "prev": HandleCameraPrev(ev.Player); break;
                    case "exit": HandleCameraExit(ev.Player); break;
                    default: ev.Player.SendConsoleMessage("Используйте list/view/next/prev/exit", "yellow"); break;
                }
                return;
            }
            if (cmd == ".079") { HandleScp079(ev.Player, args.Skip(1).ToArray()); return; }
            if (cmd == ".sec")
            {
                if (args.Length > 1 && args[1].ToLower() == "activate") HandleSecActivate(ev.Player);
                else HandleSecCommand(ev.Player, args.Skip(1).ToArray());
                return;
            }
            if (cmd == ".swap")
            {
                if (args.Length < 2) { ev.Player.SendConsoleMessage("Используйте request/accept/deny", "yellow"); return; }
                switch (args[1].ToLower())
                {
                    case "request": HandleSwapRequest(ev.Player, args.Skip(2).ToArray()); break;
                    case "accept": HandleSwapAccept(ev.Player); break;
                    case "deny": HandleSwapDeny(ev.Player); break;
                    default: ev.Player.SendConsoleMessage("Используйте request/accept/deny", "yellow"); break;
                }
                return;
            }
            if (cmd == ".escape") { HandleEscape(ev.Player); return; }
            if (cmd == ".pocket") { HandlePocketDimension(ev.Player); return; }
            if (cmd == ".leap") { HandleLeap(ev.Player); return; }
            if (cmd == ".request") { HandleRequest(ev.Player); return; }
            if (cmd == "roomba") { HandleRoomba(ev.Player); return; }
            if (cmd == ".court")
            {
                if (args.Length < 2) { ev.Player.SendConsoleMessage("Используйте start/judge/prosecutor/verdict/appeal/overturn/close", "yellow"); return; }
                switch (args[1].ToLower())
                {
                    case "start": HandleCourtStart(ev.Player, args.Skip(2).ToArray()); break;
                    case "judge": HandleCourtJudge(ev.Player, args.Skip(2).ToArray()); break;
                    case "prosecutor": HandleCourtProsecutor(ev.Player, args.Skip(2).ToArray()); break;
                    case "verdict": HandleCourtVerdict(ev.Player, args.Skip(2).ToArray()); break;
                    case "appeal": HandleCourtAppeal(ev.Player); break;
                    case "overturn": HandleCourtOverturn(ev.Player); break;
                    case "close": HandleCourtClose(ev.Player); break;
                    default: ev.Player.SendConsoleMessage("Используйте start/judge/prosecutor/verdict/appeal/overturn/close", "yellow"); break;
                }
                return;
            }
            if (cmd == ".execute") { HandleExecute(ev.Player, args); return; }
            if (cmd == ".warn") { HandleWarn(ev.Player, args.Skip(1).ToArray()); return; }
            if (cmd == ".warnings") { HandleWarnings(ev.Player); return; }
            if (cmd == ".unwarn") { HandleUnwarn(ev.Player, args.Skip(1).ToArray()); return; }
            if (cmd == ".call") { HandleCall(ev.Player, args.Skip(1).ToArray()); return; }
            if (cmd == ".report") { HandleReport(ev.Player, args.Skip(1).ToArray()); return; }

            // ---- НОВЫЕ КОМАНДЫ ----
            if (cmd == ".otz")
            {
                if (args.Length < 2)
                {
                    string info = 
                        $"<color=#00B7EB>=== EventSystem v2.0 ===</color>\n" +
                        $"Автор: YourName\n" +
                        $"Используйте .otz <текст> для отправки отзыва проводящему.\n" +
                        $"Ваше мнение поможет улучшить плагин!";
                    ev.Player.SendConsoleMessage(info, "white");
                    return;
                }
                string message = string.Join(" ", args.Skip(1));
                SendDiscordLog($"**ОТЗЫВ ОТ ИГРОКА**\nИгрок: {ev.Player.Nickname}\nТекст: {message}");
                ev.Player.SendConsoleMessage("✅ Спасибо за ваш отзыв! Он отправлен проводящему.", "green");
                return;
            }

            if (cmd == ".appealwarn")
            {
                if (args.Length < 3) { ev.Player.SendConsoleMessage("Использование: .appealwarn <ID варна> <причина>", "yellow"); return; }
                if (!int.TryParse(args[1], out int warnId)) { ev.Player.SendConsoleMessage("Укажите ID варна (число).", "red"); return; }
                string reason = string.Join(" ", args.Skip(2));
                SendDiscordLog($"**АПЕЛЛЯЦИЯ НА ВАРН**\nИгрок: {ev.Player.Nickname}\nID варна: {warnId}\nПричина апелляции: {reason}");
                ev.Player.SendConsoleMessage("✅ Ваша апелляция отправлена на рассмотрение администрации.", "green");
                return;
            }

            if (cmd == ".appealban")
            {
                if (args.Length < 2) { ev.Player.SendConsoleMessage("Использование: .appealban <причина>", "yellow"); return; }
                string reason = string.Join(" ", args.Skip(1));
                SendDiscordLog($"**АПЕЛЛЯЦИЯ НА БАН**\nИгрок: {ev.Player.Nickname}\nПричина: {reason}");
                ev.Player.SendConsoleMessage("✅ Ваша апелляция на бан отправлена администрации.", "green");
                return;
            }

            // ---- Система документов ----
            if (cmd == ".doc")
            {
                if (args.Length < 2) { ev.Player.SendConsoleMessage("Используйте create/send/read/list/delete", "yellow"); return; }
                switch (args[1].ToLower())
                {
                    case "create": HandleDocCreate(ev.Player, args.Skip(2).ToArray()); break;
                    case "send": HandleDocSend(ev.Player, args.Skip(2).ToArray()); break;
                    case "read": HandleDocRead(ev.Player, args.Skip(2).ToArray()); break;
                    case "list": HandleDocList(ev.Player); break;
                    case "delete": HandleDocDelete(ev.Player, args.Skip(2).ToArray()); break;
                    default: ev.Player.SendConsoleMessage("Используйте create/send/read/list/delete", "yellow"); break;
                }
                return;
            }

            // ---- RP-команды ----
            if (cmd == ".me")
            {
                string action = string.Join(" ", args.Skip(1));
                if (string.IsNullOrEmpty(action)) { ev.Player.SendConsoleMessage("Укажите действие: .me <действие>", "yellow"); return; }
                string message = $"{ev.Player.DisplayName} {action}";
                foreach (var p in Player.List.Where(p => Vector3.Distance(p.Position, ev.Player.Position) < 15f))
                    p.SendConsoleMessage($"[ME] {message}", "silver");
                return;
            }
            if (cmd == ".do")
            {
                string desc = string.Join(" ", args.Skip(1));
                if (string.IsNullOrEmpty(desc)) { ev.Player.SendConsoleMessage("Укажите описание: .do <текст>", "yellow"); return; }
                string message = $"{ev.Player.DisplayName} | {desc}";
                foreach (var p in Player.List.Where(p => Vector3.Distance(p.Position, ev.Player.Position) < 15f))
                    p.SendConsoleMessage($"[DO] {message}", "gray");
                return;
            }
            if (cmd == ".whisper")
            {
                if (args.Length < 3) { ev.Player.SendConsoleMessage("Использование: .whisper <игрок> <текст>", "yellow"); return; }
                string targetName = args[1];
                string text = string.Join(" ", args.Skip(2));
                var target = Player.Get(targetName);
                if (target == null) { ev.Player.SendConsoleMessage("Игрок не найден.", "red"); return; }
                if (Vector3.Distance(ev.Player.Position, target.Position) > 5f)
                { ev.Player.SendConsoleMessage("Цель слишком далеко (нужно < 5 м).", "red"); return; }
                target.SendConsoleMessage($"[Шёпот от {ev.Player.DisplayName}] {text}", "magenta");
                ev.Player.SendConsoleMessage($"[Шёпот для {target.DisplayName}] {text}", "magenta");
                return;
            }
            if (cmd == ".inspect")
            {
                if (args.Length < 2) { ev.Player.SendConsoleMessage("Укажите игрока: .inspect <игрок>", "yellow"); return; }
                var target = Player.Get(args[1]);
                if (target == null) { ev.Player.SendConsoleMessage("Игрок не найден.", "red"); return; }
                if (Vector3.Distance(ev.Player.Position, target.Position) > 10f)
                { ev.Player.SendConsoleMessage("Цель слишком далеко (нужно < 10 м).", "red"); return; }
                string rpName = rpNames.ContainsKey(target) ? rpNames[target] : "Неизвестно";
                string health = $"{target.Health}/{target.MaxHealth}";
                string role = target.Role.Type.ToString();
                string items = string.Join(", ", target.Items.Select(i => i.Type.ToString()));
                if (string.IsNullOrEmpty(items)) items = "пусто";
                string info = $"=== ОСМОТР {target.DisplayName} ===\n" +
                              $"RP-имя: {rpName}\n" +
                              $"Здоровье: {health}\n" +
                              $"Роль: {role}\n" +
                              $"Инвентарь: {items}";
                ev.Player.SendConsoleMessage(info, "white");
                return;
            }
            if (cmd == ".look")
            {
                if (args.Length < 2) { ev.Player.SendConsoleMessage("Укажите игрока: .look <игрок>", "yellow"); return; }
                var target = Player.Get(args[1]);
                if (target == null) { ev.Player.SendConsoleMessage("Игрок не найден.", "red"); return; }
                if (Vector3.Distance(ev.Player.Position, target.Position) > 15f)
                { ev.Player.SendConsoleMessage("Цель слишком далеко (нужно < 15 м).", "red"); return; }
                string rpName = rpNames.ContainsKey(target) ? rpNames[target] : "Неизвестно";
                string status = target.IsAlive ? "Жив" : "Мёртв";
                ev.Player.SendConsoleMessage($"Вы смотрите на {target.DisplayName} (RP: {rpName}, статус: {status}).", "white");
                return;
            }
            if (cmd == ".dropitem")
            {
                if (args.Length < 2) { ev.Player.SendConsoleMessage("Укажите тип предмета: .dropitem <тип>", "yellow"); return; }
                string itemType = args[1];
                var item = ev.Player.Items.FirstOrDefault(i => i.Type.ToString().Equals(itemType, StringComparison.OrdinalIgnoreCase));
                if (item == null) { ev.Player.SendConsoleMessage($"Предмет {itemType} не найден в инвентаре.", "red"); return; }
                ev.Player.RemoveItem(item);
                ev.Player.SendConsoleMessage($"✅ Вы выбросили {item.Type}.", "green");
                return;
            }
            if (cmd == ".giveitem")
            {
                if (args.Length < 3) { ev.Player.SendConsoleMessage("Использование: .giveitem <игрок> <тип предмета>", "yellow"); return; }
                string targetName = args[1];
                string itemType = args[2];
                var target = Player.Get(targetName);
                if (target == null) { ev.Player.SendConsoleMessage("Игрок не найден.", "red"); return; }
                if (Vector3.Distance(ev.Player.Position, target.Position) > 3f)
                { ev.Player.SendConsoleMessage("Цель слишком далеко (нужно < 3 м).", "red"); return; }
                var item = ev.Player.Items.FirstOrDefault(i => i.Type.ToString().Equals(itemType, StringComparison.OrdinalIgnoreCase));
                if (item == null) { ev.Player.SendConsoleMessage($"Предмет {itemType} не найден в инвентаре.", "red"); return; }
                ev.Player.RemoveItem(item);
                target.AddItem(item.Type);
                ev.Player.SendConsoleMessage($"✅ Вы передали {item.Type} игроку {target.DisplayName}.", "green");
                target.SendConsoleMessage($"📦 {ev.Player.DisplayName} передал вам {item.Type}.", "green");
                return;
            }

            // ---- Команды для ролей ----
            if (cmd == ".arrest")
            {
                if (!HasRole(ev.Player, "Охранник") && !HasRole(ev.Player, "МОГ") && !HasRole(ev.Player, "Судья"))
                { ev.Player.SendConsoleMessage("Только охрана, МОГ или судья могут арестовать.", "red"); return; }
                if (args.Length < 2) { ev.Player.SendConsoleMessage("Укажите игрока: .arrest <игрок> [причина]", "yellow"); return; }
                string targetName = args[1];
                string reason = args.Length > 2 ? string.Join(" ", args.Skip(2)) : "Нарушение";
                var target = Player.Get(targetName);
                if (target == null) { ev.Player.SendConsoleMessage("Игрок не найден.", "red"); return; }
                if (Vector3.Distance(ev.Player.Position, target.Position) > 5f)
                { ev.Player.SendConsoleMessage("Цель слишком далеко (нужно < 5 м).", "red"); return; }
                target.Broadcast(5, $"<color=red>Вас арестовал {ev.Player.DisplayName} по причине: {reason}</color>");
                ev.Player.SendConsoleMessage($"✅ {target.DisplayName} арестован.", "green");
                SendDiscordLog($"**АРЕСТ**\nАрестовал: {ev.Player.DisplayName}\nЦель: {target.DisplayName}\nПричина: {reason}");
                return;
            }
            if (cmd == ".unlock")
            {
                if (!HasRole(ev.Player, "Инженер") && !HasRole(ev.Player, "МОГ") && !HasRole(ev.Player, "Директор"))
                { ev.Player.SendConsoleMessage("Только инженеры, МОГ или директор могут открывать двери.", "red"); return; }
                Door targetDoor = null;
                float minDist = 5f;
                foreach (var door in Door.List)
                {
                    float dist = Vector3.Distance(ev.Player.Position, door.Position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        targetDoor = door;
                    }
                }
                if (targetDoor == null) { ev.Player.SendConsoleMessage("Нет дверей поблизости.", "yellow"); return; }
                targetDoor.IsOpen = true;
                targetDoor.LockoutDuration = 0;
                ev.Player.SendConsoleMessage($"✅ Дверь {targetDoor.Name} открыта.", "green");
                return;
            }
            if (cmd == ".research")
            {
                if (!HasRole(ev.Player, "Научный сотрудник") && !HasRole(ev.Player, "Исследователь"))
                { ev.Player.SendConsoleMessage("Только учёные могут исследовать предметы.", "red"); return; }
                var item = ev.Player.CurrentItem;
                if (item == null) { ev.Player.SendConsoleMessage("Возьмите предмет в руки.", "yellow"); return; }
                ev.Player.SendConsoleMessage($"🔬 Вы исследовали {item.Type}.", "green");
                return;
            }
            if (cmd == ".repair")
            {
                if (!HasRole(ev.Player, "Инженер") && !HasRole(ev.Player, "МОГ"))
                { ev.Player.SendConsoleMessage("Только инженеры или МОГ могут ремонтировать.", "red"); return; }
                ev.Player.SendConsoleMessage("🔧 Вы начали ремонт генератора...", "green");
                return;
            }
            if (cmd == ".tase")
            {
                if (!HasRole(ev.Player, "Охранник") && !HasRole(ev.Player, "МОГ"))
                { ev.Player.SendConsoleMessage("Только охрана может использовать электрошокер.", "red"); return; }
                if (args.Length < 2) { ev.Player.SendConsoleMessage("Укажите игрока: .tase <игрок>", "yellow"); return; }
                var target = Player.Get(args[1]);
                if (target == null) { ev.Player.SendConsoleMessage("Игрок не найден.", "red"); return; }
                if (Vector3.Distance(ev.Player.Position, target.Position) > 5f)
                { ev.Player.SendConsoleMessage("Цель слишком далеко (нужно < 5 м).", "red"); return; }
                target.Broadcast(3, "<color=yellow>⚡ Вас оглушили!</color>");
                target.EnableEffect(EffectType.Stun, 3f);
                ev.Player.SendConsoleMessage($"✅ {target.DisplayName} оглушён.", "green");
                return;
            }

            // ---- ev role ----
            if (cmd == "evrole")
            {
                if (!CheckPermission(ev.Player, plugin.Config.PermissionHost) && !helpers.Contains(ev.Player))
                { ev.Player.SendConsoleMessage("Недостаточно прав.", "red"); return; }
                if (args.Length < 3) { ev.Player.SendConsoleMessage("Использование: ev role <ник> <название_роли>", "yellow"); return; }
                string name = args[1];
                string roleName = string.Join(" ", args.Skip(2));
                var target = Player.Get(name);
                if (target == null) { ev.Player.SendConsoleMessage("Игрок не найден.", "red");
