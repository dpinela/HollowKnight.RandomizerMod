using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerMod.MultiWorld
{
    public class ConnectionState
    {
        public string Token;
        public ulong Uid;
        public string UserName;

        //public GameInformation GameInfo = null;

        public bool Connected;
        public bool Joined;
        public bool FullWorldInformation;
        public DateTime LastPing = DateTime.Now;
    }
}
