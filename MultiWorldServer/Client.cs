using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using MultiWorldProtocol.Messaging;
using MultiWorldProtocol.Messaging.Definitions.Messages;

namespace MultiWorldServer
{
    class Client
    {
        public string nickname;
        public ulong UID;
        public TcpClient TcpClient;
        public object SendLock = new object();
        public DateTime lastPing;
        public Thread ReadWorker;

        public PlayerSession Session;
    }
}
