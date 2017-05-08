using System;
using System.Collections.Generic;

namespace KitZ.Db
{
    public class Kit
    {
        private int maxUses;

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

        public string Name { get; }
        public List<KitItem> ItemList { get; }
        public List<string> RegionList { get; }

        public int MaxUses
        {
            get => maxUses;

            set => maxUses = value > 0 ? value : 0;
        }

        public TimeSpan RefreshTime { get; set; }
        public bool Protect { get; set; }
    }
}