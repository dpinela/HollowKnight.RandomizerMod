using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using RandomizerLib;
using RandomizerLib.MultiWorld;
using Modding;

namespace FakeClient
{
    class GameClient
    {
        static void Main(string[] args)
        {
            ClientConnection cc = new ClientConnection();
            cc.Connect();
        }
    }
}
