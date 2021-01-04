using System;
using System.Diagnostics.Eventing.Reader;
using System.Threading;

using RandomizerLib;

namespace MultiWorldServer
{
    internal class Program
    {
        private static Server Serv;

        private static void Main()
        {
            LogicManager.ParseXML();

            Serv = new Server(38281);
            while (Serv.Running)
                Thread.Sleep(1000);
        }
    }
}
