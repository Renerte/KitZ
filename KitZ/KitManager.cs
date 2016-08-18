using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using TShockAPI.DB;

namespace KitZ
{
    public class KitManager
    {
        private IDbConnection db;
        private ReaderWriterLockSlim slimLock = new ReaderWriterLockSlim();

        public KitManager(IDbConnection db)
        {
            this.db = db;

            var sqlCreator = new SqlTableCreator(db,
                db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder) new SqliteQueryCreator() : new MysqlQueryCreator());

            sqlCreator.EnsureTableStructure(new SqlTable("Kits",
                new SqlColumn("ID", MySqlDbType.Int32) { AutoIncrement = true, Primary = true},
                new SqlColumn("Name", MySqlDbType.Text) {Unique = true},
                new SqlColumn("Items", MySqlDbType.Text),
                new SqlColumn("Quantities", MySqlDbType.Text),
                new SqlColumn("MaxUses", MySqlDbType.Int32),
                new SqlColumn("RefreshTime", MySqlDbType.Int32)));

            //TODO: KitUses table
        }
    }
}