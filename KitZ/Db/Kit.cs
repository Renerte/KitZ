using System.Collections.Generic;

namespace KitZ.Db
{
    public class Kit
    {
        public Kit(string name, List<KitItem> itemList, int maxUses, int refreshTime, List<string> regionList)
        {
            Name = name;
            ItemList = itemList;
            MaxUses = maxUses;
            RefreshTime = refreshTime;
            RegionList = regionList;
        }

        public string Name { get; private set; }
        public List<KitItem> ItemList { get; private set; }
        public int MaxUses { get; private set; }
        public int RefreshTime { get; private set; }
        public List<string> RegionList { get; private set; }
    }
}