using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerMod.Randomization.MultiWorld
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
    }
}
