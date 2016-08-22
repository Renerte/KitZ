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
        private readonly List<Kit> kits = new List<Kit>();
        private readonly ReaderWriterLockSlim slimLock = new ReaderWriterLockSlim();
        private IDbConnection db;

        public KitManager(IDbConnection db)
        {
            this.db = db;

            var sqlCreator = new SqlTableCreator(db,
                db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder) new SqliteQueryCreator() : new MysqlQueryCreator());

            sqlCreator.EnsureTableStructure(new SqlTable("Kits",
                new SqlColumn("ID", MySqlDbType.Int32) {AutoIncrement = true, Primary = true},
                new SqlColumn("Name", MySqlDbType.Text) {Unique = true},
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
                    var itemList =
                        items.Select(item => new KitItem(int.Parse(item[0]), int.Parse(item[1]), int.Parse(item[2])))
                            .ToList();
                    var regionList = result.Get<string>("Regions").Split(',').ToList();
                    var name = result.Get<string>("Name");
                    var maxUses = result.Get<int>("MaxUses");
                    var refreshTime = result.Get<int>("RefreshTime");
                    kits.Add(new Kit(name, itemList, maxUses, refreshTime, regionList));
                }
            }

            TShock.Log.ConsoleInfo($"[KitZ] Loaded {kits.Count} kits.");
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
    }
}