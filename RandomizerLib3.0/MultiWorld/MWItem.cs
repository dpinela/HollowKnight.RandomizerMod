namespace RandomizerLib.MultiWorld
{
    public class MWItem
    {
        public readonly int playerId;
        public readonly string item;

        public MWItem(int playerId, string item)
        {
            this.playerId = playerId;
            this.item = item;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != GetType()) return false;
            MWItem other = (MWItem) obj;
            return playerId == other.playerId && item == other.item;
        }

        public override int GetHashCode()
        {
            return (playerId, item).GetHashCode();
        }

        public override string ToString()
        {
            return "MW(" + (playerId + 1) + ")_" + item;
        }
    }
}
