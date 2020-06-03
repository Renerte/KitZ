using System;
using TShockAPI.DB;

namespace KitZ.Db
{
    public class KitUse
    {
        public DateTime ExpireTime;
        public int Uses;

        public KitUse(UserAccount account, Kit kit, int uses, DateTime expireTime)
        {
            Account = account;
            Kit = kit;
            Uses = uses;
            ExpireTime = expireTime;
        }

        public UserAccount Account { get; }
        public Kit Kit { get; }
    }
}