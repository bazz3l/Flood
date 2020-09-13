using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Flood", "Bazz3l", "1.0.1")]
    [Description("Flood the server with bad weather.")]
    class Flood : RustPlugin
    {
        #region Fields
        private const string permUse = "flood.use";
        private FloodManager manager = new FloodManager();
        private static Flood Instance;
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
            permission.RegisterPermission(permUse, this);
        }

        private void Init()
        {
            Instance = this;
        }

        private void Unload()
        {
            manager.StopFlood();

            Instance = null;
        }
        #endregion

        #region Core
        class FloodManager
        {
            private float floodMaxLevel = 1f;
            private float floodLevel = 0f;
            private float floodPercent = 0f;
            private bool floodReverse;
            private Timer floodTimer;
            private bool floodInProgress;

            public void StartFlood()
            {
                floodInProgress = true;

                floodTimer = Instance.timer.Every(0.5f, () => CheckFlood());
            }

            public void StopFlood()
            {
                floodTimer?.Destroy();

                floodInProgress = false;
                floodReverse = false;
                floodLevel = 0f;

                SetWeather(0.0);
                SetOceanLevel(0f);
            }

            public bool InProgress()
            {
                return floodInProgress;
            }

            private void SetWeather(double amount)
            {
                RunCommand($"weather.clouds " + amount);
                RunCommand($"weather.rain " + amount);
            }

            private void SetOceanLevel(float amount) => RunCommand("env.oceanlevel " + amount);

            public void SetMaxLevel(float level)
            {
                floodMaxLevel = level;
            }

            private void CheckFlood()
            {
                if (!floodReverse && floodLevel >= floodMaxLevel)
                {
                    floodReverse = true;
                }

                if (!floodReverse)
                    floodLevel += 0.01f;
                else
                    floodLevel -= 0.01f;

                SetWeather((floodLevel / floodMaxLevel));

                SetOceanLevel(floodLevel);

                if (floodLevel <= 0f)
                {
                    StopFlood();
                }
            }
        }

        private void StartFlood(BasePlayer player)
        {
            if (manager.InProgress())
            {
                player.ChatMessage(Lang("FloodInProgress", player.UserIDString));
                return;
            }

            manager.StartFlood();

            player.ChatMessage(Lang("FloodStarted", player.UserIDString));
        }

        private void StopFlood(BasePlayer player)
        {
            manager.StopFlood();

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

            manager.SetMaxLevel(maxLevel);

            player.ChatMessage(Lang("FloodLevelSet", player.UserIDString, maxLevel));
        }
        #endregion

        #region Command
        [ChatCommand("flood")]
        private void FloodCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse))
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

        private static void RunCommand(string command) => Instance.Server.Command(command);
        #endregion
    }
}