using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KitZ.Db;
using TShockAPI;

namespace KitZ
{
    public static class Commands
    {
        public static async void Kit(CommandArgs e)
        {
            if (e.Parameters.Count == 0)
            {
                e.Player.SendErrorMessage(KitZ.Config.NoKitEntered);
                return;
            }

            var kit = await KitZ.Kits.GetAsync(e.Parameters.First());
            if (kit != null)
            {
                if (kit.Protect && !e.Player.HasPermission($"kitz.use.{e.Parameters[0]}"))
                {
                    e.Player.SendErrorMessage(string.Format(KitZ.Config.KitNoPerm, kit.Name));
                    return;
                }
                var playerRegion = e.Player.CurrentRegion;
                var regionName = playerRegion != null ? playerRegion.Name : "";
                if (kit.RegionList.Count > 0 && !kit.RegionList.Contains(regionName))
                {
                    e.Player.SendErrorMessage(string.Format(KitZ.Config.OutsideRequiredRegion, kit.Name));
                    return;
                }
                if (e.Player.InventorySlotAvailable && !await KitZ.Kits.DoKitUseAsync(e.Player, kit))
                {
                    e.Player.SendErrorMessage(string.Format(KitZ.Config.KitUseLimitReached, kit.Name));
                    return;
                }
                e.Player.SendInfoMessage(string.Format(KitZ.Config.KitGiven, e.Parameters[0]));
                foreach (var kitItem in kit.ItemList)
                {
                    var item = TShock.Utils.GetItemById(kitItem.Id);
                    if (kitItem.Amount == 0 || kitItem.Amount > item.maxStack)
                        item.stack = item.maxStack;
                    else
                        item.stack = kitItem.Amount;
                    if (!e.Player.GiveItemCheck(item.netID, item.Name, item.width, item.height, item.stack,
                        kitItem.Modifier))
                        e.Player.SendErrorMessage(string.Format(KitZ.Config.ItemNotGiven,
                            TShock.Utils.GetItemById(kitItem.Id).Name));
                }
                var kitUse = await KitZ.Kits.GetKitUseAsync(e.Player, kit);
                if (kitUse == null || kit.RefreshTime == TimeSpan.Zero) return;
                await Task.Delay(kitUse.ExpireTime - DateTime.UtcNow);
                await KitZ.Kits.DeleteKitUseAsync(kitUse);
            }
            else
            {
                e.Player.SendErrorMessage(string.Format(KitZ.Config.KitNotFound, e.Parameters[0]));
            }
        }

        public static async void Manage(CommandArgs e)
        {
            if (e.Parameters.Count < 1)
            {
                e.Player.SendErrorMessage("Use /kitz help for a list of commands.");
                return;
            }
            switch (e.Parameters[0])
            {
                case "add":
                    if (e.Parameters.Count < 2)
                    {
                        e.Player.SendErrorMessage("Use: /kitz add name");
                        return;
                    }
                    if (
                        await
                            KitZ.Kits.AddAsync(e.Parameters[1], new List<KitItem>(), 0, TimeSpan.Zero,
                                new List<string>(), false))
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
                        e.Player.SendInfoMessage($"Kit {e.Parameters[1]} was removed.");
                    else
                        e.Player.SendErrorMessage($"Could not remove kit {e.Parameters[1]}!");
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
                            $"Added {TShock.Utils.GetItemById(item.Id).Name} to kit {e.Parameters[1]}.");
                    else
                        e.Player.SendErrorMessage(
                            $"Could not add {TShock.Utils.GetItemById(item.Id).Name} to kit {e.Parameters[1]}!");
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
                        e.Player.SendErrorMessage(
                            $"Could not remove item id {e.Parameters[2]} from kit {e.Parameters[1]}!");
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
                                $"{++i}: {TShock.Utils.GetPrefixById(kitItem.Modifier)} {TShock.Utils.GetItemById(kitItem.Id).Name} x {kitItem.Amount}");
                        e.Player.SendInfoMessage("End of items.");
                    }
                    break;
                case "maxuse":
                    if (e.Parameters.Count < 3)
                    {
                        e.Player.SendErrorMessage("Use: /kitz maxuse name amount");
                        return;
                    }
                    if (await KitZ.Kits.SetMaxKitUsesAsync(e.Parameters[1], int.Parse(e.Parameters[2])))
                        e.Player.SendInfoMessage($"Set max kit uses for kit {e.Parameters[1]} to {e.Parameters[2]}");
                    else
                        e.Player.SendErrorMessage($"Could not set max kit uses for kit {e.Parameters[1]}!");
                    break;
                case "time":
                    if (e.Parameters.Count < 3)
                    {
                        e.Player.SendErrorMessage("Use: /kitz time name time");
                        return;
                    }
                    int time;
                    TShock.Utils.TryParseTime(e.Parameters[2], out time);
                    if (await KitZ.Kits.SetRefreshTimeAsync(e.Parameters[1], TimeSpan.FromSeconds(time)))
                        e.Player.SendInfoMessage($"Set refresh time for kit {e.Parameters[1]}.");
                    else
                        e.Player.SendErrorMessage($"Could not set refresh time for kit {e.Parameters[1]}!");
                    break;
                case "addregion":
                    if (e.Parameters.Count < 3)
                    {
                        e.Player.SendErrorMessage("Use: /kitz addregion name region");
                        return;
                    }
                    if (await KitZ.Kits.AddRegionAsync(e.Parameters[1], e.Parameters[2]))
                        e.Player.SendInfoMessage($"Added region {e.Parameters[2]} to kit {e.Parameters[1]}");
                    else
                        e.Player.SendErrorMessage($"Could not add region to kit {e.Parameters[1]}!");
                    break;
                case "delregion":
                    if (e.Parameters.Count < 3)
                    {
                        e.Player.SendErrorMessage("Use: /kitz delregion name region");
                        return;
                    }
                    if (await KitZ.Kits.DeleteRegionAsync(e.Parameters[1], e.Parameters[2]))
                        e.Player.SendInfoMessage($"Deleted region {e.Parameters[2]} from kit {e.Parameters[1]}");
                    else
                        e.Player.SendErrorMessage($"Could not delete region from kit {e.Parameters[1]}!");
                    break;
                case "protect":
                    if (e.Parameters.Count > 3)
                    {
                        e.Player.SendErrorMessage("Use: /kitz protect name true/false");
                        return;
                    }
                    if (await KitZ.Kits.ProtectAsync(e.Parameters[1], bool.Parse(e.Parameters[2])))
                        e.Player.SendInfoMessage($"Set protection flag for kit {e.Parameters[1]} to {e.Parameters[2]}");
                    else
                        e.Player.SendErrorMessage($"Could not set protection flag!");
                    break;
                case "help":
                    e.Player.SendInfoMessage(
                        $"KitZ v{Assembly.GetExecutingAssembly().GetName().Version} made by Renerte - totally customizable kits!");
                    e.Player.SendInfoMessage("Available commands:");
                    e.Player.SendInfoMessage("/kit name - use kit");
                    e.Player.SendInfoMessage("/kitz add name - adds kit");
                    e.Player.SendInfoMessage("/kitz del name - removes kit");
                    e.Player.SendInfoMessage("/kitz additem name item - adds item to kit");
                    e.Player.SendInfoMessage("/kitz delitem name itemid - removes item from kit");
                    e.Player.SendInfoMessage("/kitz list name - lists all items in kit with their ids");
                    e.Player.SendInfoMessage("/kitz help - this message ;)");
                    break;
                default:
                    e.Player.SendInfoMessage("Unrecognized action!");
                    break;
            }
        }
    }
}