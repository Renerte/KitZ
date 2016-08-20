using System.Linq;
using System.Reflection;
using TShockAPI;

namespace KitZ
{
    public static class Commands
    {
        public static void About(CommandArgs e)
        {
            e.Player.SendInfoMessage(
                $"KitZ v{Assembly.GetExecutingAssembly().GetName().Version} made by Renerte - totally customizable kits!");
        }

        public static void Kit(CommandArgs e)
        {
            if (e.Parameters.Count == 0 || !e.Player.HasPermission($"kit.use.{e.Parameters.First()}")) return;
            e.Player.SendInfoMessage(string.Format(KitZ.Config.KitGiven, e.Parameters.First()));
        }
    }
}