using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerMod.MultiWorld
{
    public class ConnectionState
    {
        public ulong Uid;
        public bool Connected;
        public bool Joined;
        public DateTime LastPing = DateTime.Now;
    }
}
