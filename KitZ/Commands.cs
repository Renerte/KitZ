using System.Reflection;
using TShockAPI;

namespace KitZ
{
    public static class Commands
    {
        public static void About(CommandArgs e)
        {
            e.Player.SendInfoMessage($"KitZ v{Assembly.GetExecutingAssembly().GetName().Version} made by Renerte - totally customizable kits!");
        }
    }
}
