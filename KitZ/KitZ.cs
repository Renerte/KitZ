using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace KitZ
{
    [ApiVersion(1, 23)]
    public class KitZ : TerrariaPlugin
    {
        public static Config Config { get; private set; }

        public KitZ(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            GeneralHooks.ReloadEvent += OnReload;
            PlayerHooks.PlayerCommand += OnPlayerCommand;

            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
        }

        public override string Author => "Renerte";
        public override string Description => "Customizable kits for your TShock server!";
        public override string Name => "KitZ";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private void OnPlayerCommand(PlayerCommandEventArgs e)
        {
            if (e.Handled || e.Player == null)
            {
                return;
            }

            Command command = e.CommandList.FirstOrDefault();
            if (command == null || (command.Permissions.Any() && !command.Permissions.Any(s => e.Player.Group.HasPermission(s))))
            {
                return;
            }
        }

        private void OnReload(ReloadEventArgs e)
        {
            string path = Path.Combine(TShock.SavePath, "kitz.json");
            Config = Config.Read(path);
            if (!File.Exists(path))
                Config.Write(path);
            e.Player.SendSuccessMessage($"[KitZ] {Config.ReloadSuccess}");
        }

        private void OnInitialize(EventArgs e)
        {
            string path = Path.Combine(TShock.SavePath, "kitz.json");
            Config = Config.Read(path);
            if (!File.Exists(path))
                Config.Write(path);
        }
    }
}
