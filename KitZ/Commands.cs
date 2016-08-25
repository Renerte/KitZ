using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using KitZ.Db;
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

        public static async void Manage(CommandArgs e)
        {
            switch (e.Parameters[0])
            {
                case "add":
                    if (e.Parameters.Count < 2)
                    {
                        e.Player.SendErrorMessage("Use: /kitz add name");
                        return;
                    }
                    if (await KitZ.Kits.AddAsync(e.Parameters[1], new List<KitItem>(), 0, 0, new List<string>()))
                        e.Player.SendInfoMessage($"Kit {e.Parameters[1]} added.");
                    else
                        e.Player.SendErrorMessage($"Could not add kit {e.Parameters[1]}! Details in server log.");
                    break;
                case "del":
                    if (e.Parameters.Count < 2)
                    {
                        e.Player.SendErrorMessage("Use: /kitz del name");
                        return;
                    }
                    if (await KitZ.Kits.DeleteAsync(e.Parameters[1]))
                    {
                        e.Player.SendInfoMessage($"Kit {e.Parameters[1]} was removed.");
                    }
                    else
                    {
                        e.Player.SendErrorMessage($"Could not remove kit {e.Parameters[1]}!");
                    }
                    break;
                case "additem":
                    var itemtag = new Regex(@"\[i(?:\/p(\d+?))?(?:\/(?:x|s)(\d+?))?:(\d+?)\]", RegexOptions.IgnoreCase);
                    KitItem item;
                    switch (e.Parameters.Count)
                    {
                        case 2:
                            e.Player.SendErrorMessage("Please provide item!");
                            return;
                        case 3:
                            var match = itemtag.Match(e.Parameters[2]);
                            if (match.Success)
                            {
                                var id = !string.IsNullOrWhiteSpace(match.Groups[3].Value)
                                    ? int.Parse(match.Groups[3].Value)
                                    : 0;
                                var amount = !string.IsNullOrWhiteSpace(match.Groups[2].Value)
                                    ? int.Parse(match.Groups[2].Value)
                                    : 0;
                                var modifier = !string.IsNullOrWhiteSpace(match.Groups[1].Value)
                                    ? int.Parse(match.Groups[1].Value)
                                    : 0;
                                item = new KitItem(id, amount, modifier);
                                break;
                            }
                            item = new KitItem(TShock.Utils.GetItemByIdOrName(e.Parameters[2]).First().netID, 1, 0);
                            break;
                        case 4:
                            item = new KitItem(TShock.Utils.GetItemByIdOrName(e.Parameters[2]).First().netID,
                                int.Parse(e.Parameters[3]), 0);
                            break;
                        case 5:
                            item = new KitItem(TShock.Utils.GetItemByIdOrName(e.Parameters[2]).First().netID,
                                int.Parse(e.Parameters[3]), int.Parse(e.Parameters[4]));
                            break;
                        default:
                            e.Player.SendErrorMessage("Use: /kitz additem kit item");
                            return;
                    }
                    if (await KitZ.Kits.AddItemAsync(e.Parameters[1], item))
                        e.Player.SendInfoMessage(
                            $"Added {TShock.Utils.GetItemById(item.Id).name} to kit {e.Parameters[1]}.");
                    else
                        e.Player.SendErrorMessage(
                            $"Could not add {TShock.Utils.GetItemById(item.Id).name} to kit {e.Parameters[1]}!");
                    break;
                case "delitem":
                    if (e.Parameters.Count > 3)
                    {
                        e.Player.SendErrorMessage("Use: /kitz delitem name itemid");
                        return;
                    }
                    if (await KitZ.Kits.DeleteItemAsync(e.Parameters[1], int.Parse(e.Parameters[2])))
                        e.Player.SendInfoMessage($"Removed item id {e.Parameters[2]} from kit {e.Parameters[1]}.");
                    else
                    {
                        e.Player.SendErrorMessage(
                            $"Could not remove item id {e.Parameters[2]} from kit {e.Parameters[1]}!");
                    }
                    break;
                case "list":
                    if (e.Parameters.Count < 2)
                    {
                        e.Player.SendErrorMessage("Use: /kitz list name");
                        return;
                    }
                    var kit = await KitZ.Kits.GetAsync(e.Parameters[1]);
                    if (kit != null)
                    {
                        e.Player.SendInfoMessage($"Items in kit {kit.Name}:");
                        var i = 0;
                        foreach (var kitItem in kit.ItemList)
                            e.Player.SendInfoMessage(
                                $"{++i}: {TShock.Utils.GetPrefixById(kitItem.Modifier)} {TShock.Utils.GetItemById(kitItem.Id).name} x {kitItem.Amount}");
                        e.Player.SendInfoMessage("End of items.");
                    }
                    break;
                case "maxuse":
                    //TODO: Set max amount of kit uses before refresh is required.
                    break;
                case "time":
                    //TODO: Set time, after which player's kit uses are reset.
                    break;
                case "addregion":
                    //TODO: Add region to kit.
                    break;
                case "delregion":
                    //TODO: Remove region from kit.
                    break;
                case "help":
                    //TODO: List of possible commands.
                    break;
                default:
                    e.Player.SendInfoMessage("Unrecognized action!");
                    break;
            }
        }
    }
}