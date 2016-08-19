using System.Collections.Generic;

namespace KitZ.Db
{
    public class Kit
    {
        public Kit(string name, List<int> itemList, int maxUses, int refreshTime)
        {
            Name = name;
            ItemList = itemList;
            MaxUses = maxUses;
            RefreshTime = refreshTime;
        }

        public string Name { get; private set; }
        public List<int> ItemList { get; private set; }
        public int MaxUses { get; private set; }
        public int RefreshTime { get; private set; }
    }
}