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
        public string Nickname;
        public ulong UID;
        public string Room = null;
        public TcpClient TcpClient;
        public object SendLock = new object();
        public DateTime lastPing;
        public Thread ReadWorker;

        public PlayerSession Session;
    }
}
