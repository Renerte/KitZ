using System;
using TShockAPI.DB;

namespace KitZ.Db
{
    public class KitUse
    {
        public DateTime ExpireTime;
        public int Uses;

        public KitUse(User user, Kit kit, int uses, DateTime expireTime)
        {
            User = user;
            Kit = kit;
            Uses = uses;
            ExpireTime = expireTime;
        }

        public User User { get; }
        public Kit Kit { get; }
    }
}