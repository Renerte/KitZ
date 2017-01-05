using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using KitZ.Db;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace KitZ
{
    [ApiVersion(2, 0)]
    public class KitZ : TerrariaPlugin
    {
        public KitZ(Main game) : base(game)
        {
        }

        public static Config Config { get; private set; }
        public static IDbConnection Db { get; private set; }
        public static KitManager Kits { get; private set; }

        public override string Author => "Renerte";
        public override string Description => "Customizable kits for your TShock server!";
        public override string Name => "KitZ";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public override void Initialize()
        {
            GeneralHooks.ReloadEvent += OnReload;
            PlayerHooks.PlayerCommand += OnPlayerCommand;

            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GeneralHooks.ReloadEvent -= OnReload;
                PlayerHooks.PlayerCommand -= OnPlayerCommand;

                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
            }
        }

        private void OnPlayerCommand(PlayerCommandEventArgs e)
        {
            if (e.Handled || e.Player == null)
                return;

            var command = e.CommandList.FirstOrDefault();
            if (command == null ||
                command.Permissions.Any() && !command.Permissions.Any(s => e.Player.Group.HasPermission(s)))
            {
            }
        }

        private async void OnReload(ReloadEventArgs e)
        {
            var path = Path.Combine(TShock.SavePath, "kitz.json");
            Config = Config.Read(path);
            if (!File.Exists(path))
                Config.Write(path);
            await Kits.ReloadAsync();
            e.Player.SendSuccessMessage($"[KitZ] {Config.ReloadSuccess}");
        }

        private void OnInitialize(EventArgs e)
        {
            #region Config

            var path = Path.Combine(TShock.SavePath, "kitz.json");
            Config = Config.Read(path);
            if (!File.Exists(path))
                Config.Write(path);

            #endregion

            #region Database

            if (TShock.Config.StorageType.Equals("mysql", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(Config.MySqlHost) ||
                    string.IsNullOrWhiteSpace(Config.MySqlDbName))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(
                        "[KitZ] MySQL is enabled, but the KitZ MySQL Configuration has not been set.");
                    Console.WriteLine(
                        "[KitZ] Please configure your MySQL server information in kitz.json, then restart the server.");
                    Console.WriteLine("[KitZ] This plugin will now disable itself...");
                    Console.ResetColor();

                    Dispose(true);

                    return;
                }

                var host = Config.MySqlHost.Split(':');
                Db = new MySqlConnection
                {
                    ConnectionString =
                        $"Server={host[0]}; Port={(host.Length == 1 ? "3306" : host[1])}; Database={Config.MySqlDbName}; Uid={Config.MySqlUsername}; Pwd={Config.MySqlPassword};"
                };
            }
            else if (TShock.Config.StorageType.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
            {
                Db = new SqliteConnection(
                    "uri=file://" + Path.Combine(TShock.SavePath, "kitz.sqlite") + ",Version=3");
            }
            else
            {
                throw new InvalidOperationException("Invalid storage type!");
            }

            #endregion

            #region Commands

            //Allows overriding of already created commands.
            Action<Command> Add = c =>
            {
                //Finds any commands with names and aliases that match the new command and removes them.
                TShockAPI.Commands.ChatCommands.RemoveAll(c2 => c2.Names.Exists(s2 => c.Names.Contains(s2)));
                //Then adds the new command.
                TShockAPI.Commands.ChatCommands.Add(c);
            };

            Add(new Command("kit.use", Commands.Kit, "kit")
            {
                HelpText = "Gives kits. /kit name",
                AllowServer = false
            });

            Add(new Command("kitz.manage", Commands.Manage, "kitz")
            {
                HelpText = "Manages kits."
            });

            #endregion
        }

        private void OnPostInitialize(EventArgs e)
        {
            Kits = new KitManager(Db);
            Kits.CleanupKitUsesAsync();
        }
    }
}