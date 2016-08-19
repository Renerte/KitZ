﻿using System.Collections.Generic;
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;
using TShockAPI.DB;

namespace KitZ.Db
{
    public class KitManager
    {
        private IDbConnection db;
        private List<Kit> Kits = new List<Kit>();
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
                new SqlColumn("Quantities", MySqlDbType.Text),
                new SqlColumn("MaxUses", MySqlDbType.Int32),
                new SqlColumn("RefreshTime", MySqlDbType.Int32)));

            sqlCreator.EnsureTableStructure(new SqlTable("KitUses",
                new SqlColumn("ID", MySqlDbType.Int32) {AutoIncrement = true, Primary = true},
                new SqlColumn("UserID", MySqlDbType.Int32),
                new SqlColumn("KitID", MySqlDbType.Int32),
                new SqlColumn("ExpireTime", MySqlDbType.DateTime)));
        }
    }
}