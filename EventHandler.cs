using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;

namespace EventSystem
{
    public class EventHandler
    {
        private readonly EventPlugin plugin;
        private bool isEventActive = false;
        private string eventName;
        private string rpLevel;
        private Player host;
        private readonly List<Player> helpers = new List<Player>();
        private DateTime eventStartTime;
        private CoroutineHandle hudCoroutine;

        public EventHandler(EventPlugin plugin)
        {
            this.plugin = plugin;
            Exiled.Events.Handlers.Player.ConsoleCommand += OnConsoleCommand;
            Exiled.Events.Handlers.Player.RemoteAdminCommand += OnRemoteAdminCommand;
        }

        ~EventHandler()
        {
            Exiled.Events.Handlers.Player.ConsoleCommand -= OnConsoleCommand;
            Exiled.Events.Handlers.Player.RemoteAdminCommand -= OnRemoteAdminCommand;
        }

        private void OnRemoteAdminCommand(RemoteAdminCommandEventArgs ev)
        {
            if (!isEventActive) return;
            if (ev.Player == host || helpers.Contains(ev.Player)) return;

            ev.IsAllowed = false;
            ev.Player.SendConsoleMessage(
                "❌ Во время проведения ивента админ-панель временно заблокирована.\nОбратитесь к проводящему ивента.",
                "red"
            );
            Log.Warn($"[{plugin.Config.ProjectName}] Заблокирована RA-команда от {ev.Player.Nickname}: {ev.Command}");
        }

        private void OnConsoleCommand(ConsoleCommandEventArgs ev)
        {
            if (!plugin.Config.IsEnabled) return;
            if (ev.Player == null || !ev.Player.IsVerified) return;

            string[] args = ev.Command.Split(' ');
            if (args.Length == 0) return;
            if (!args[0].StartsWith("ev", StringComparison.OrdinalIgnoreCase)) return;

            var commandArgs = args.Skip(1).ToArray();
            if (commandArgs.Length == 0)
            {
                SendHelp(ev.Player);
                return;
            }

            string subCommand = commandArgs[0].ToLower();
            switch (subCommand)
            {
                case "start":
                    HandleStart(ev.Player, commandArgs.Skip(1).ToArray());
                    break;
                case "end":
                    HandleEnd(ev.Player);
                    break;
                case "give":
                    HandleGive(ev.Player, commandArgs.Skip(1).ToArray());
                    break;
                case "helper":
                    HandleHelper(ev.Player, commandArgs.Skip(1).ToArray());
                    break;
                case "removehelper":
                    HandleRemoveHelper(ev.Player, commandArgs.Skip(1).ToArray());
                    break;
                case "list":
                    HandleList(ev.Player);
                    break;
                default:
                    SendHelp(ev.Player);
                    break;
            }
        }

        private void HandleStart(Player caller, string[] args)
        {
            if (!CheckPermission(caller, plugin.Config.PermissionHost))
            {
                caller.SendConsoleMessage("У вас нет прав на запуск ивента.", "red");
                return;
            }

            if (isEventActive)
            {
                caller.SendConsoleMessage("Ивент уже активен.", "yellow");
                return;
            }

            string evName = args.Length > 0 ? string.Join(" ", args) : plugin.Config.DefaultEventName;
            string rp = plugin.Config.DefaultRPLevel;
            StartEvent(caller, evName, rp);
        }

        private void StartEvent(Player hostPlayer, string evName, string rpLevel)
        {
            isEventActive = true;
            this.eventName = evName;
            this.rpLevel = rpLevel;
            this.host = hostPlayer;
            this.helpers.Clear();
            this.eventStartTime = DateTime.Now;

            string project = plugin.Config.ProjectName;
            string startMsg = $"🟢 {project} | {plugin.Config.BroadcastStart}";
            foreach (var player in Player.List)
                player.Broadcast(5, startMsg);

            Log.Info($"[{project}] Ивент начат ведущим {hostPlayer.Nickname}. Название: {evName}");

            hudCoroutine = Timing.RunCoroutine(UpdateHUDCoroutine());
        }

        private void HandleEnd(Player caller)
        {
            if (!isEventActive)
            {
                caller.SendConsoleMessage("Ивент не активен.", "yellow");
                return;
            }

            if (caller != host && !CheckPermission(caller, plugin.Config.PermissionHost))
            {
                caller.SendConsoleMessage("Только ведущий или администратор могут завершить ивент.", "red");
                return;
            }

            EndEvent();
        }

        private void EndEvent()
        {
            isEventActive = false;
            if (hudCoroutine.IsRunning)
                Timing.KillCoroutines(hudCoroutine);

            string project = plugin.Config.ProjectName;
            string endMsg = $"🔴 {project} | {plugin.Config.BroadcastEnd}";
            foreach (var player in Player.List)
            {
                player.Broadcast(5, endMsg);
                player.SendHint("", 1);
            }

            Log.Info($"[{project}] Ивент завершён ведущим {host?.Nickname ?? "неизвестно"}");
            host = null;
            helpers.Clear();
        }

        private void HandleGive(Player caller, string[] args)
        {
            if (!isEventActive || host != caller)
            {
                caller.SendConsoleMessage("Вы не являетесь текущим ведущим.", "red");
                return;
            }

            if (args.Length == 0)
            {
                caller.SendConsoleMessage("Укажите игрока: ev give <ник>", "yellow");
                return;
            }

            string targetName = string.Join(" ", args);
            Player target = Player.Get(targetName);
            if (target == null)
            {
                caller.SendConsoleMessage("Игрок не найден.", "red");
                return;
            }

            host = target;
            Log.Info($"[{plugin.Config.ProjectName}] Права ведущего переданы от {caller.Nickname} к {target.Nickname}");
            caller.SendConsoleMessage($"Права ведущего переданы игроку {target.Nickname}.", "green");
            target.SendConsoleMessage("Вы теперь ведущий ивента!", "green");
        }

        private void HandleHelper(Player caller, string[] args)
        {
            if (!isEventActive || (caller != host && !CheckPermission(caller, plugin.Config.PermissionHost)))
            {
                caller.SendConsoleMessage("Только ведущий или администратор могут добавлять помощников.", "red");
                return;
            }

            if (args.Length == 0)
            {
                caller.SendConsoleMessage("Укажите игрока: ev helper <ник>", "yellow");
                return;
            }

            string targetName = string.Join(" ", args);
            Player target = Player.Get(targetName);
            if (target == null)
            {
                caller.SendConsoleMessage("Игрок не найден.", "red");
                return;
            }

            if (helpers.Contains(target))
            {
                caller.SendConsoleMessage($"{target.Nickname} уже является помощником.", "yellow");
                return;
            }

            helpers.Add(target);
            Log.Info($"[{plugin.Config.ProjectName}] Помощник {target.Nickname} добавлен ведущим {caller.Nickname}");
            caller.SendConsoleMessage($"Игрок {target.Nickname} назначен помощником.", "green");
            target.SendConsoleMessage("Вы назначены помощником ведущего!", "green");
        }

        private void HandleRemoveHelper(Player caller, string[] args)
        {
            if (!isEventActive || (caller != host && !CheckPermission(caller, plugin.Config.PermissionHost)))
            {
                caller.SendConsoleMessage("Только ведущий или администратор могут удалять помощников.", "red");
                return;
            }

            if (args.Length == 0)
            {
                caller.SendConsoleMessage("Укажите игрока: ev removehelper <ник>", "yellow");
                return;
            }

            string targetName = string.Join(" ", args);
            Player target = Player.Get(targetName);
            if (target == null)
            {
                caller.SendConsoleMessage("Игрок не найден.", "red");
                return;
            }

            if (!helpers.Remove(target))
            {
                caller.SendConsoleMessage($"{target.Nickname} не является помощником.", "yellow");
                return;
            }

            Log.Info($"[{plugin.Config.ProjectName}] Помощник {target.Nickname} удалён ведущим {caller.Nickname}");
            caller.SendConsoleMessage($"Игрок {target.Nickname} больше не помощник.", "green");
            target.SendConsoleMessage("Вы больше не помощник ведущего.", "yellow");
        }

        private void HandleList(Player caller)
        {
            if (!isEventActive)
            {
                caller.SendConsoleMessage("Ивент не активен.", "yellow");
                return;
            }

            string helperNames = helpers.Count > 0 ? string.Join(", ", helpers.Select(p => p.Nickname)) : "нет";
            caller.SendConsoleMessage(
                $"{plugin.Config.ProjectName} — текущий ивент:\n" +
                $"Ведущий: {host?.Nickname ?? "нет"}\n" +
                $"Помощники: {helperNames}",
                "green"
            );
        }

        private void SendHelp(Player player)
        {
            player.SendConsoleMessage(
                $"{plugin.Config.ProjectName} — Команды ивента:" +
                "ev start [название] – начать ивент" +
                "ev end – завершить ивент"  +
                "ev give <игрок> – передать права проводящего " +
                "ev helper <игрок> – добавить помощника" +
                "ev removehelper <игрок> – убрать помощника" +
                "ev list – список ведущего и помощников",
                "white"
            );
        }

        private bool CheckPermission(Player player, string permission)
        {
            return player.CheckPermission(permission);
        }

        private IEnumerator<float> UpdateHUDCoroutine()
        {
            while (isEventActive)
            {
                string hudText = BuildHUD();
                foreach (var player in Player.List)
                {
                    player.SendHint(hudText, 1.5f);
                }
                yield return Timing.WaitForSeconds(plugin.Config.UpdateInterval);
            }
        }

        private string BuildHUD()
        {
            var config = plugin.Config;
            var elapsed = DateTime.Now - eventStartTime;
            var roundTime = Round.ElapsedTime;
            string helpersStr = helpers.Count > 0 ? string.Join(", ", helpers.Select(p => p.Nickname)) : "—";

            return $@"╔════════════════════════════════════════════╗
        🟢 {config.ProjectName} • EVENT SYSTEM
╠════════════════════════════════════════════╣

📌 Ивент: {eventName}
🎭 RP: {rpLevel}

👤 Проводящий: {host?.Nickname ?? "—"}
🛡️ Helper: {helpersStr}

⏱️ Врямя Ивента: {elapsed:hh\\:mm\\:ss}
⌛ Раунд: {roundTime:hh\\:mm\\:ss}

╠════════════════════════════════════════════╣
{config.HintInstruction}
╚════════════════════════════════════════════╝";
        }
    }
}
