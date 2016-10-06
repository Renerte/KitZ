using System;
using System.Collections.Generic;

namespace KitZ.Db
{
    public class Kit
    {
        public Kit(string name, List<KitItem> itemList, int maxUses, TimeSpan refreshTime, List<string> regionList,
            bool protect)
        {
            Name = name;
            ItemList = itemList;
            MaxUses = maxUses;
            RefreshTime = refreshTime;
            RegionList = regionList;
            Protect = protect;
        }

        public string Name { get; private set; }
        public List<KitItem> ItemList { get; private set; }
        public int MaxUses { get; private set; }
        public TimeSpan RefreshTime { get; private set; }
        public List<string> RegionList { get; private set; }
        public bool Protect { get; private set; }
    }
}