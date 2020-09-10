using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Flood", "Bazz3l", "1.0.0")]
    [Description("Flood the server with bad weather.")]
    class Flood : RustPlugin
    {
        #region Fields
        const string permUse = "flood.use";
        FloodManager manager = new FloodManager();
        static Flood Instance;
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

        void OnServerInitialized()
        {
            permission.RegisterPermission(permUse, this);
        }

        void Init()
        {
            Instance = this;
        }

        void Unload()
        {
            manager.StopFlood();

            Instance = null;
        }
        #endregion

        #region Core
        class FloodManager
        {
            float floodMaxLevel = 1f;
            float floodLevel = 0f;
            float floodPercent = 0f;
            bool floodReverse;
            Timer floodTimer;
            bool floodInProgress;

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

            void SetWeather(double amount)
            {
                RunCommand($"weather.clouds " + amount);
                RunCommand($"weather.rain " + amount);
            }

            void SetOceanLevel(float amount) => RunCommand("env.oceanlevel " + amount);

            public void SetMaxLevel(float level)
            {
                floodMaxLevel = level;
            }

            void CheckFlood()
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

        void StartFlood(BasePlayer player)
        {
            if (manager.InProgress())
            {
                player.ChatMessage(Lang("FloodInProgress", player.UserIDString));
                return;
            }

            manager.StartFlood();

            player.ChatMessage(Lang("FloodStarted", player.UserIDString));
        }

        void StopFlood(BasePlayer player)
        {
            manager.StopFlood();

            player.ChatMessage(Lang("FloodEnded", player.UserIDString));
        }

        void SetFlood(BasePlayer player, string[] args)
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

        public static void RunCommand(string command) => Instance.Server.Command(command);
        #endregion

        #region Command
        [ChatCommand("flood")]
        void FloodCommand(BasePlayer player, string command, string[] args)
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
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        #endregion
    }
}