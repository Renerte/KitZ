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

        public static async void Kit(CommandArgs e)
        {
            if (e.Parameters.Count == 0)
            {
                e.Player.SendErrorMessage(KitZ.Config.NoKitEntered);
                return;
            }
            if (!e.Player.HasPermission($"kitz.use.{e.Parameters[0]}"))
            {
                e.Player.SendErrorMessage(string.Format(KitZ.Config.KitNoPerm, e.Parameters[0]));
                return;
            }

            var kit = await KitZ.Kits.GetAsync(e.Parameters.First());
            if (kit != null)
            {
                e.Player.SendInfoMessage(string.Format(KitZ.Config.KitGiven, e.Parameters[0]));
                foreach (var kitItem in kit.ItemList)
                    e.Player.SendInfoMessage(
                        $"{TShock.Utils.GetPrefixById(kitItem.Modifier)} {TShock.Utils.GetItemById(kitItem.Id).name} x {kitItem.Amount}");
            }
            else
            {
                e.Player.SendErrorMessage(string.Format(KitZ.Config.KitNotFound, e.Parameters[0]));
            }
        }
    }
}