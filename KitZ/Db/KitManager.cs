﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KitZ.Extensions;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace KitZ.Db
{
    public class KitManager
    {
        private readonly IDbConnection db;
        private readonly List<Kit> kits = new List<Kit>();
        private readonly List<KitUse> kitUses = new List<KitUse>();
        private readonly ReaderWriterLockSlim slimLock = new ReaderWriterLockSlim();

        public KitManager(IDbConnection db)
        {
            this.db = db;

            var sqlCreator = new SqlTableCreator(db,
                db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder) new SqliteQueryCreator() : new MysqlQueryCreator());

            sqlCreator.EnsureTableStructure(new SqlTable("Kits",
                new SqlColumn("ID", MySqlDbType.Int32) {AutoIncrement = true, Primary = true},
                new SqlColumn("Name", MySqlDbType.VarChar, 32) {Length = 32, Unique = true},
                new SqlColumn("Items", MySqlDbType.Text),
                new SqlColumn("MaxUses", MySqlDbType.Int32),
                new SqlColumn("RefreshTime", MySqlDbType.Text),
                new SqlColumn("Regions", MySqlDbType.Text),
                new SqlColumn("Protect", MySqlDbType.Int32)));

            sqlCreator.EnsureTableStructure(new SqlTable("KitUses",
                new SqlColumn("ID", MySqlDbType.Int32) {AutoIncrement = true, Primary = true},
                new SqlColumn("UserID", MySqlDbType.Int32),
                new SqlColumn("Kit", MySqlDbType.VarChar, 32),
                new SqlColumn("Uses", MySqlDbType.Int32),
                new SqlColumn("ExpireTime", MySqlDbType.Text)));

            using (var result = db.QueryReader("SELECT * FROM Kits"))
            {
                while (result.Read())
                {
                    var items = result.Get<string>("Items").Split(',').Select((item, i) => item.Split(':'));
                    var itemList = !string.IsNullOrWhiteSpace(items.FirstOrDefault()[0])
                        ? items.Select(item => new KitItem(int.Parse(item[0]), int.Parse(item[1]), int.Parse(item[2])))
                            .ToList()
                        : new List<KitItem>();
                    var regionList = !string.IsNullOrWhiteSpace(result.Get<string>("Regions"))
                        ? result.Get<string>("Regions").Split(',').ToList()
                        : new List<string>();
                    var name = result.Get<string>("Name");
                    var maxUses = result.Get<int>("MaxUses");
                    var refreshTime = TimeSpan.Parse(result.Get<string>("RefreshTime"));
                    var protect = result.Get<bool>("Protect");
                    kits.Add(new Kit(name, itemList, maxUses, refreshTime, regionList, protect));
                }
            }

            using (var result = db.QueryReader("SELECT * FROM KitUses"))
            {
                while (result.Read())
                {
                    var account = TShock.UserAccounts.GetUserAccountByID(result.Get<int>("UserID"));
                    var kit =
                        kits.Find(
                            k => k.Name.Equals(result.Get<string>("Kit"), StringComparison.InvariantCultureIgnoreCase));
                    var uses = result.Get<int>("Uses");
                    var expireTime = DateTime.ParseExact(result.Get<string>("ExpireTime"), "u", null);
                    kitUses.Add(new KitUse(account, kit, uses, expireTime));
                }
            }

            TShock.Log.ConsoleInfo($"[KitZ] Loaded {kits.Count} kits.");
        }

        public async Task<bool> ReloadAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (slimLock)
                    {
                        kits.Clear();
                        using (var result = db.QueryReader("SELECT * FROM Kits"))
                        {
                            while (result.Read())
                            {
                                var items = result.Get<string>("Items").Split(',').Select((item, i) => item.Split(':'));
                                var itemList = !string.IsNullOrWhiteSpace(items.FirstOrDefault()[0])
                                    ? items.Select(
                                            item =>
                                                new KitItem(int.Parse(item[0]), int.Parse(item[1]),
                                                    int.Parse(item[2])))
                                        .ToList()
                                    : new List<KitItem>();
                                var regionList = !string.IsNullOrWhiteSpace(result.Get<string>("Regions"))
                                    ? result.Get<string>("Regions").Split(',').ToList()
                                    : new List<string>();
                                var name = result.Get<string>("Name");
                                var maxUses = result.Get<int>("MaxUses");
                                var refreshTime = TimeSpan.ParseExact(result.Get<string>("RefreshTime"), "c", null);
                                var protect = result.Get<bool>("Protect");
                                kits.Add(new Kit(name, itemList, maxUses, refreshTime, regionList, protect));
                            }
                        }

                        kitUses.Clear();
                        using (var result = db.QueryReader("SELECT * FROM KitUses"))
                        {
                            while (result.Read())
                            {
                                var account = TShock.UserAccounts.GetUserAccountByID(result.Get<int>("UserID"));
                                var kit =
                                    kits.Find(
                                        k =>
                                            k.Name.Equals(result.Get<string>("Kit"),
                                                StringComparison.InvariantCultureIgnoreCase));
                                var uses = result.Get<int>("Uses");
                                var expireTime = DateTime.ParseExact(result.Get<string>("ExpireTime"), "u", null);
                                kitUses.Add(new KitUse(account, kit, uses, expireTime));
                            }
                        }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            });
        }

        #region Managing kits

        public async Task<Kit> GetAsync(string name)
        {
            return await Task.Run(() =>
            {
                lock (slimLock)
                {
                    return kits.Find(k => k.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                }
            });
        }

        public async Task<bool> AddAsync(string name, List<KitItem> itemList, int maxUses, TimeSpan refreshTime,
            List<string> regionList, bool protect)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (slimLock)
                    {
                        kits.Add(new Kit(name, itemList, maxUses, refreshTime, regionList, protect));
                        return db.Query(
                                   "INSERT INTO Kits (Name, Items, MaxUses, RefreshTime, Regions, Protect) VALUES (@0, @1, @2, @3, @4, @5)",
                                   name,
                                   string.Join(",", itemList),
                                   maxUses,
                                   refreshTime.ToString("c"),
                                   string.Join(",", regionList),
                                   false) > 0;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            });
        }

        public async Task<bool> DeleteAsync(string name)
        {
            var query = db.GetSqlType() == SqlType.Mysql
                ? "DELETE FROM Kits WHERE Name = @0"
                : "DELETE FROM Kits WHERE Name = @0 COLLATE NOCASE";

            return await Task.Run(() =>
            {
                try
                {
                    lock (slimLock)
                    {
                        kits.RemoveAll(k => k.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                        return db.Query(query, name) > 0;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            });
        }

        #endregion

        #region Kit settings

        public async Task<bool> AddItemAsync(string name, KitItem item)
        {
            var query = db.GetSqlType() == SqlType.Mysql
                ? "UPDATE Kits SET Items = @0 WHERE Name = @1"
                : "UPDATE Kits SET Items = @0 WHERE Name = @1 COLLATE NOCASE";

            return await Task.Run(() =>
            {
                var kit = kits.Find(k => k.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                if (kit == null || item.Id == 0 || item.Amount == 0)
                    return false;
                try
                {
                    lock (slimLock)
                    {
                        kit.ItemList.Add(item);
                        return db.Query(query, string.Join(",", kit.ItemList), name) > 0;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            });
        }

        public async Task<bool> DeleteItemAsync(string name, int itemid)
        {
            var query = db.GetSqlType() == SqlType.Mysql
                ? "UPDATE Kits SET Items = @0 WHERE Name = @1"
                : "UPDATE Kits SET Items = @0 WHERE Name = @1 COLLATE NOCASE";

            return await Task.Run(() =>
            {
                try
                {
                    lock (slimLock)
                    {
                        var kit = kits.Find(k => k.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                        kit.ItemList.RemoveAt(itemid - 1);
                        return db.Query(query, string.Join(",", kit.ItemList), name) > 0;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            });
        }

        public async Task<bool> SetMaxKitUsesAsync(string name, int maxUses)
        {
            var query = db.GetSqlType() == SqlType.Mysql
                ? "UPDATE Kits SET MaxUses = @0 WHERE Name = @1"
                : "UPDATE Kits SET MaxUses = @0 WHERE Name = @1 COLLATE NOCASE";

            return await Task.Run(() =>
            {
                try
                {
                    lock (slimLock)
                    {
                        var kit = kits.Find(k => k.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                        kit.MaxUses = maxUses;
                        return db.Query(query, maxUses, name) > 0;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            });
        }

        public async Task<bool> SetRefreshTimeAsync(string name, TimeSpan time)
        {
            var query = db.GetSqlType() == SqlType.Mysql
                ? "UPDATE Kits SET RefreshTime = @0 WHERE Name = @1"
                : "UPDATE Kits SET RefreshTime = @0 WHERE Name = @1 COLLATE NOCASE";

            return await Task.Run(() =>
            {
                try
                {
                    lock (slimLock)
                    {
                        var kit = kits.Find(k => k.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                        kit.RefreshTime = time;
                        return db.Query(query, time.ToString("c"), name) > 0;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            });
        }

        public async Task<bool> AddRegionAsync(string name, string region)
        {
            var query = db.GetSqlType() == SqlType.Mysql
                ? "UPDATE Kits SET Regions = @0 WHERE Name = @1"
                : "UPDATE Kits SET Regions = @0 WHERE Name = @1 COLLATE NOCASE";

            return await Task.Run(() =>
            {
                try
                {
                    lock (slimLock)
                    {
                        var kit = kits.Find(k => k.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                        kit.RegionList.Add(region);
                        return db.Query(query, string.Join(",", kit.RegionList), name) > 0;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            });
        }

        public async Task<bool> DeleteRegionAsync(string name, string region)
        {
            var query = db.GetSqlType() == SqlType.Mysql
                ? "UPDATE Kits SET Regions = @0 WHERE Name = @1"
                : "UPDATE Kits SET Regions = @0 WHERE Name = @1 COLLATE NOCASE";

            return await Task.Run(() =>
            {
                try
                {
                    lock (slimLock)
                    {
                        var kit = kits.Find(k => k.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                        kit.RegionList.Remove(region);
                        return db.Query(query, string.Join(",", kit.RegionList), name) > 0;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            });
        }

        public async Task<bool> ProtectAsync(string name, bool protect)
        {
            var query = db.GetSqlType() == SqlType.Mysql
                ? "UPDATE Kits SET Protect = @0 WHERE Name = @1"
                : "UPDATE Kits SET Protect = @0 WHERE Name = @1 COLLATE NOCASE";

            return await Task.Run(() =>
            {
                try
                {
                    lock (slimLock)
                    {
                        var kit = kits.Find(k => k.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                        kit.Protect = protect;
                        return db.Query(query, protect, name) > 0;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            });
        }

        #endregion

        #region Kit uses

        public async Task<KitUse> GetKitUseAsync(TSPlayer player, Kit kit)
        {
            return await Task.Run(() =>
            {
                lock (slimLock)
                {
                    return kitUses.Find(u => u.Kit.Equals(kit) && u.Account.Equals(player.Account));
                }
            });
        }

        public async Task<bool> DoKitUseAsync(TSPlayer player, Kit kit)
        {
            if (kit.MaxUses == 0)
                return await Task.Run(() => true);

            var kitUse = await GetKitUseAsync(player, kit);
            if (kitUse == null)
            {
                await AddKitUseAsync(player, kit);
                kitUse = await GetKitUseAsync(player, kit);
            }
            var query = db.GetSqlType() == SqlType.Mysql
                ? "UPDATE KitUses SET Uses = @0 WHERE UserID = @1 AND Kit = @2"
                : "UPDATE KitUses SET Uses = @0 WHERE UserID = @1 AND Kit = @2 COLLATE NOCASE";

            return await Task.Run(() =>
            {
                try
                {
                    lock (slimLock)
                    {
                        if (kitUse.Uses >= kitUse.Kit.MaxUses)
                            return false;
                        kitUse.Uses += 1;
                        return db.Query(query, kitUse.Uses, player.Account.ID, kit.Name) > 0;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            });
        }

        public async Task<bool> AddKitUseAsync(TSPlayer player, Kit kit)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (slimLock)
                    {
                        kitUses.Add(new KitUse(player.Account, kit, 0,
                            kit.RefreshTime.CompareTo(TimeSpan.Zero) == 0
                                ? DateTime.MaxValue
                                : DateTime.UtcNow.Add(kit.RefreshTime)));
                        return db.Query("INSERT INTO KitUses (UserID, Kit, Uses, ExpireTime) VALUES (@0, @1, @2, @3)",
                                   player.Account.ID,
                                   kit.Name,
                                   0,
                                   kit.RefreshTime.CompareTo(TimeSpan.Zero) == 0
                                       ? DateTime.MaxValue.ToString("u")
                                       : DateTime.UtcNow.Add(kit.RefreshTime).ToString("u")) > 0;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            });
        }

        public async Task<List<KitUse>> GetPlayerKitUsesAsync(TSPlayer player)
        {
            return await Task.Run(() =>
            {
                lock (slimLock)
                {
                    return kitUses.FindAll(u => u.Account.Equals(player.Account));
                }
            });
        }

        public async Task<bool> DeleteKitUseAsync(KitUse kitUse)
        {
            var query = db.GetSqlType() == SqlType.Mysql
                ? "DELETE FROM KitUses WHERE UserID = @0 AND Kit = @1"
                : "DELETE FROM KitUses WHERE UserID = @0 AND Kit = @1 COLLATE NOCASE";

            return await Task.Run(() =>
            {
                try
                {
                    lock (slimLock)
                    {
                        kitUses.RemoveAll(u => u.Equals(kitUse));
                        return db.Query(query, kitUse.Account.ID, kitUse.Kit.Name) > 0;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            });
        }

        public async void CleanupKitUsesAsync()
        {
            var kitUsesToDelete = kitUses.Where(kitUse => kitUse.ExpireTime.CompareTo(DateTime.UtcNow) <= 0).ToList();
            foreach (var kitUse in kitUsesToDelete)
                await DeleteKitUseAsync(kitUse);
        }

        public void ScheduleKitUsesExpiration()
        {
            foreach (var kitUse in kitUses.Where(kitUse => kitUse.Kit.RefreshTime != TimeSpan.Zero))
                Task.Run(async () =>
                {
                    await Task.Delay(kitUse.ExpireTime - DateTime.UtcNow);
                    await DeleteKitUseAsync(kitUse);
                }).Forget();
        }

        #endregion
    }
}