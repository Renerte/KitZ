using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using MySql.Data.MySqlClient;
using TShockAPI.DB;

namespace KitZ.Db
{
    public class KitManager
    {
        private readonly List<Kit> kits = new List<Kit>();
        private IDbConnection db;
        private ReaderWriterLockSlim slimLock = new ReaderWriterLockSlim();

        public KitManager(IDbConnection db)
        {
            this.db = db;

            var sqlCreator = new SqlTableCreator(db,
                db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder) new SqliteQueryCreator() : new MysqlQueryCreator());

            sqlCreator.EnsureTableStructure(new SqlTable("Kits",
                new SqlColumn("ID", MySqlDbType.Int32) {AutoIncrement = true, Primary = true},
                new SqlColumn("Name", MySqlDbType.Text) {Unique = true},
                new SqlColumn("Items", MySqlDbType.Text),
                new SqlColumn("Amounts", MySqlDbType.Text),
                new SqlColumn("MaxUses", MySqlDbType.Int32),
                new SqlColumn("RefreshTime", MySqlDbType.Int32),
                new SqlColumn("Regions", MySqlDbType.String)));

            sqlCreator.EnsureTableStructure(new SqlTable("KitUses",
                new SqlColumn("ID", MySqlDbType.Int32) {AutoIncrement = true, Primary = true},
                new SqlColumn("UserID", MySqlDbType.Int32),
                new SqlColumn("KitID", MySqlDbType.Int32),
                new SqlColumn("Uses", MySqlDbType.Int32),
                new SqlColumn("ExpireTime", MySqlDbType.DateTime)));

            using (var result = db.QueryReader("SELECT * FROM Kits"))
            {
                while (result.Read())
                {
                    var items = result.Get<string>("Items").Split(',');
                    var amounts = result.Get<string>("Amounts").Split(',');
                    var itemList = items.Select((t, i) => new KitItem(int.Parse(t), int.Parse(amounts[i]))).ToList();
                    var regionList = result.Get<string>("Regions").Split(',').ToList();
                    var name = result.Get<string>("Name");
                    var maxUses = result.Get<int>("MaxUses");
                    var refreshTime = result.Get<int>("RefreshTime");
                    kits.Add(new Kit(name, itemList, maxUses, refreshTime, regionList));
                }
            }
        }
    }
}