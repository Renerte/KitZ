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

        public int Id { get; }
        public int Amount { get; }
        public int Modifier { get; }

        public override string ToString()
        {
            return $"{Id}:{Amount}:{Modifier}";
        }
    }
}