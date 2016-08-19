namespace KitZ.Db
{
    public class KitItem
    {
        public KitItem(int id, int amount)
        {
            Id = id;
            Amount = amount;
        }

        public int Id { get; private set; }
        public int Amount { get; private set; }
    }
}