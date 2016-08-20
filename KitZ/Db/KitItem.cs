namespace KitZ.Db
{
    public class KitItem
    {
        public KitItem(int id, int amount, int modifier)
        {
            Id = id;
            Amount = amount;
            Modifier = modifier;
        }

        public int Id { get; private set; }
        public int Amount { get; private set; }
        public int Modifier { get; private set; }

        public override string ToString()
        {
            return $"{Id}:{Amount}:{Modifier}";
        }
    }
}