using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Flood", "Bazz3l", "1.0.2")]
    [Description("Flood the server with bad weather.")]
    public class Flood : RustPlugin
    {
        #region Fields
        private const string PermUse = "flood.use";
        private readonly FloodManager _manager = new FloodManager();
        private static Flood _instance;
        #endregion

        #region Oxide
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string> {
                { "InvalidSyntax", "Invalid syntax: /flood start|stop|level <amount>, ocean level." },
                { "NoPermission", "No permission." },
                { "FloodInProgress", "Flood is already in progress." },
                { "FloodLevelSet", "Flood level set to {0}." },
                { "FloodStarted", "Flood has started." },
                { "FloodEnded", "Flood has ended." }
            }, this);
        }

        private void OnServerInitialized()
        {
            permission.RegisterPermission(PermUse, this);
        }

        private void Init()
        {
            _instance = this;
        }

        private void Unload()
        {
            _manager.StopFlood();

            _instance = null;
        }
        #endregion

        #region Core
        private class FloodManager
        {
            private float _floodMaxLevel = 1f;
            private float _floodLevel = 0f;
            private bool _floodReverse;
            private Timer _floodTimer;
            private bool _floodInProgress;

            public void StartFlood()
            {
                _floodInProgress = true;

                _floodTimer = _instance.timer.Every(0.1f, CheckFlood);
            }

            public void StopFlood()
            {
                _floodTimer?.Destroy();

                _floodInProgress = false;
                _floodReverse = false;
                _floodLevel = 0f;

                SetWeather(0.0);
                SetOceanLevel(0f);
            }

            public bool InProgress()
            {
                return _floodInProgress;
            }

            private void SetWeather(double amount)
            {
                RunCommand($"weather.clouds {amount}");
                RunCommand($"weather.rain {amount}");
            }

            private void SetOceanLevel(float amount) => RunCommand($"env.oceanlevel {amount}");

            public void SetMaxLevel(float level)
            {
                _floodMaxLevel = level;
            }

            private void CheckFlood()
            {
                if (!_floodReverse && _floodLevel >= _floodMaxLevel)
                {
                    _floodReverse = true;
                }

                if (!_floodReverse)
                    _floodLevel += 0.01f;
                else
                    _floodLevel -= 0.01f;

                SetWeather((_floodLevel / _floodMaxLevel));

                SetOceanLevel(_floodLevel);

                if (_floodLevel <= 0f)
                {
                    StopFlood();
                }
            }
        }

        private void StartFlood(BasePlayer player)
        {
            if (_manager.InProgress())
            {
                player.ChatMessage(Lang("FloodInProgress", player.UserIDString));
                return;
            }

            _manager.StartFlood();

            player.ChatMessage(Lang("FloodStarted", player.UserIDString));
        }

        private void StopFlood(BasePlayer player)
        {
            _manager.StopFlood();

            player.ChatMessage(Lang("FloodEnded", player.UserIDString));
        }

        private void SetFlood(BasePlayer player, string[] args)
        {
            if (args.Length != 1)
            {
                player.ChatMessage(Lang("InvalidSyntax", player.UserIDString));
                return;
            }

            float maxLevel;

            if (!float.TryParse(args[0], out maxLevel))
            {
                player.ChatMessage(Lang("InvalidSyntax", player.UserIDString));
                return;
            }

            _manager.SetMaxLevel(maxLevel);

            player.ChatMessage(Lang("FloodLevelSet", player.UserIDString, maxLevel));
        }
        #endregion

        #region Command
        [ChatCommand("flood")]
        private void FloodCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermUse))
            {
                player.ChatMessage(Lang("NoPermission", player.UserIDString));
                return;
            }

            if (args.Length < 1)
            {
                player.ChatMessage(Lang("InvalidSyntax", player.UserIDString));
                return;
            }

            switch(args[0].ToLower())
            {
                case "start":
                    StartFlood(player);
                    break;
                case "stop":
                    StopFlood(player);
                    break;
                case "level":
                    SetFlood(player, args.Skip(1).ToArray());
                    break;
                default:
                    player.ChatMessage(Lang("InvalidSyntax", player.UserIDString));
                    break;
            }
        }
        #endregion

        #region Helpers
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private static void RunCommand(string command) => ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), command);
        #endregion
    }
}