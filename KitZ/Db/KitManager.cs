using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace KitZ.Db
{
    public class KitManager
    {
        private readonly IDbConnection db;
        private readonly ReaderWriterLockSlim slimLock = new ReaderWriterLockSlim();
        private readonly List<Kit> kits = new List<Kit>();

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
                new SqlColumn("RefreshTime", MySqlDbType.Int32),
                new SqlColumn("Regions", MySqlDbType.Text)));

            sqlCreator.EnsureTableStructure(new SqlTable("KitUses",
                new SqlColumn("ID", MySqlDbType.Int32) {AutoIncrement = true, Primary = true},
                new SqlColumn("UserID", MySqlDbType.Int32),
                new SqlColumn("KitID", MySqlDbType.Int32),
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
                    var refreshTime = result.Get<int>("RefreshTime");
                    kits.Add(new Kit(name, itemList, maxUses, refreshTime, regionList));
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
                                var refreshTime = result.Get<int>("RefreshTime");
                                kits.Add(new Kit(name, itemList, maxUses, refreshTime, regionList));
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

        public async Task<bool> AddAsync(string name, List<KitItem> itemList, int maxUses, int refreshTime,
            List<string> regionList)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (slimLock)
                    {
                        kits.Add(new Kit(name, itemList, maxUses, refreshTime, regionList));
                        return db.Query(
                                   "INSERT INTO Kits (Name, Items, MaxUses, RefreshTime, Regions) VALUES (@0, @1, @2, @3, @4)",
                                   name,
                                   string.Join(",", itemList),
                                   maxUses,
                                   refreshTime,
                                   string.Join(",", regionList)) > 0;
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

        public async Task<bool> AddItemAsync(string name, KitItem item)
        {
            var query = db.GetSqlType() == SqlType.Mysql
                ? "UPDATE Kits SET Items = @0 WHERE Name = @1"
                : "UPDATE Kits SET Items = @0 WHERE Name = @1 COLLATE NOCASE";

            return await Task.Run(() =>
            {
                var kit = GetAsync(name).Result;
                if ((kit == null) || (item.Id == 0) || (item.Amount == 0))
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
                        var kit = kits.First(k => k.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
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
    }
}