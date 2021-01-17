using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MultiWorldProtocol.Messaging;
using MultiWorldProtocol.Binary;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using MultiWorldProtocol.Messaging.Definitions.Messages;

using RandomizerLib;
using Newtonsoft.Json;
using RandomizerLib.MultiWorld;
using System.IO;

namespace MultiWorldServer
{
    class Server
    {
        const int PingInterval = 10000; //In milliseconds

        private ulong nextUID = 1;
        private readonly MWMessagePacker Packer = new MWMessagePacker(new BinaryMWMessageEncoder());
        private readonly List<Client> Unidentified = new List<Client>();

        private readonly Timer PingTimer;

        private readonly object _clientLock = new object();
        private readonly Dictionary<ulong, Client> Clients = new Dictionary<ulong, Client>();
        private readonly Dictionary<int, GameSession> GameSessions = new Dictionary<int, GameSession>();
        private readonly Dictionary<string, Dictionary<ulong, RandoSettings>> ready = new Dictionary<string, Dictionary<ulong, RandoSettings>>();
        private TcpListener _server;
        private readonly Timer ResendTimer;

        private static StreamWriter LogWriter;

        public bool Running { get; private set; }

        public Server(int port)
        {
            //Listen on any ip
            _server = new TcpListener(IPAddress.Parse("0.0.0.0"), port);
            _server.Start();

            //_readThread = new Thread(ReadWorker);
            //_readThread.Start();
            _server.BeginAcceptTcpClient(AcceptClient, _server);
            PingTimer = new Timer(DoPing, Clients, 1000, PingInterval);
            ResendTimer = new Timer(DoResends, Clients, 500, 1000);
            Running = true;
            Log($"Server started on port {port}!");
        }

        internal static void OpenLogger(string filename)
        {
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");
            }
            filename = "Logs/" + filename + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
            FileStream fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            LogWriter = new StreamWriter(fileStream, Encoding.UTF8) { AutoFlush = true };
        }

        internal static void Log(string message, int? session = null)
        {
            if (session == null)
            {
                LogWriter.WriteLine($"[{DateTime.Now.ToLongTimeString()}] {message}");
            }
            else
            {
                LogWriter.WriteLine($"[{DateTime.Now.ToLongTimeString()}] [{session}] {message}");
            }
        }

        internal static void LogToConsole(string message)
        {
            Console.WriteLine(message);
            Log(message);
        }

        public void GiveItem(string item, int session, int player)
        {
            LogToConsole($"Giving item {item} to player {player + 1} in session {session}");
            string suffix;
            (item, suffix) = LogicManager.ExtractSuffix(item);
            item = LogicManager.GetItemFromLower(item);

            if (item == null)
            {
                LogToConsole($"Invalid item: {item}");
                return;
            }

            if (!GameSessions.ContainsKey(session))
            {
                LogToConsole($"Session {session} does not exist");
                return;
            }

            GameSessions[session].SendItemTo(player, item + suffix, "Magic", "Server");
        }

        public void ListSessions()
        {
            LogToConsole($"{GameSessions.Count} current sessions");
            foreach (var kvp in GameSessions)
            {
                LogToConsole($"ID: {kvp.Key} players: {kvp.Value.getPlayerString()}");
            }
        }

        public void ListReady()
        {
            LogToConsole($"{ready.Count} current lobbies");
            foreach (var kvp in ready)
            {
                string playerString = "";
                foreach (var kvp2 in kvp.Value)
                {
                    playerString += $"{Clients[kvp2.Key].Nickname}, ";
                }
                if (playerString != "") playerString = playerString.Substring(0, playerString.Length - 2);
                LogToConsole($"Room: {kvp.Key} players: {playerString}");
            }
        }

        private void DoPing(object clients)
        {
            lock (_clientLock)
            {
                var Now = DateTime.Now;
                List<Client> clientList = Clients.Values.ToList();
                for (int i = clientList.Count - 1; i >= 0; i--)
                {
                    Client client = clientList[i];
                    //If a client has missed 3 pings we disconnect them
                    if (Now - client.lastPing > TimeSpan.FromMilliseconds(PingInterval * 3.5))
                    {
                        Log(string.Format("Client {0} timed out. ({1})", client.UID, client.Session?.Name));
                        DisconnectClient(client);
                    }
                    else
                        SendMessage(new MWPingMessage(), client);
                }
            }
        }

        private void DoResends(object clients)
        {
            lock (_clientLock)
            {
                var ClientList = Clients.Values.ToList();
                for (int i = ClientList.Count - 1; i>= 0; i--)
                {
                    var client = ClientList[i];
                    if(client.Session!= null)
                    {
                        lock (client.Session.MessagesToConfirm)
                        {
                            var now = DateTime.Now;
                            for (int j = client.Session.MessagesToConfirm.Count - 1; j >= 0; j--)
                            {
                                var entry = client.Session.MessagesToConfirm[j];
                                if (now - entry.LastSent > TimeSpan.FromSeconds(5))
                                {
                                    var msg = entry.Message;
                                    SendMessage(msg, client);
                                    entry.LastSent = now;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void StartReadThread(Client c)
        {
            //Check that we aren't already reading
            if (c.ReadWorker != null)
                return;
            var start = new ParameterizedThreadStart(ReadWorker);
            c.ReadWorker = new Thread(start);
            c.ReadWorker.Start(c);
        }

        private void ReadWorker(object boxedClient)
        {
            Client client = boxedClient as Client;
            NetworkStream stream = client.TcpClient.GetStream();
            try
            {
                while (client.TcpClient.Connected)
                {
                    MWPackedMessage message = new MWPackedMessage(stream);
                    ReadFromClient(client, message);
                }
            }
            catch(Exception)
            {
                DisconnectClient(client);
            }
        }

        private void AcceptClient(IAsyncResult res)
        {
            try
            {
                Client client = new Client
                {
                    TcpClient = _server.EndAcceptTcpClient(res)
                };

                _server.BeginAcceptTcpClient(AcceptClient, _server);

                if (!client.TcpClient.Connected)
                {
                    return;
                }

                client.TcpClient.ReceiveTimeout = 2000;
                client.TcpClient.SendTimeout = 2000;
                client.lastPing = DateTime.Now;

                lock (_clientLock)
                {
                    Unidentified.Add(client);
                }

                StartReadThread(client);
            } catch (Exception e) // Not sure what could throw here, but have been seeing random rare exceptions in the servers
            {
                Log("Error when accepting client: " + e.Message);
            }
        }

        private bool SendMessage(MWMessage message, Client client)
        {
            if (client?.TcpClient == null || !client.TcpClient.Connected)
            {
                //Log("Returning early due to client not connected");
                return false;
            }

            try
            {
                byte[] bytes = Packer.Pack(message).Buffer;
                lock (client.TcpClient)
                {
                    NetworkStream stream = client.TcpClient.GetStream();
                    stream.WriteTimeout = 2000;
                    stream.Write(bytes, 0, bytes.Length);
                }
                return true;
            }
            catch (Exception e)
            {
                Log($"Failed to send message to '{client.Session?.Name}':\n{e}\nDisconnecting");
                DisconnectClient(client);
                return false;
            }
        }

        private static void WriteToClient(IAsyncResult res)
        {
            Client c = res.AsyncState as Client;
            if (c == null)
                throw new InvalidOperationException("How the fuck was this ever called with a null state object?");
            try
            {
                NetworkStream stream = (NetworkStream)c.TcpClient.GetStream();
                stream.EndWrite(res);
            }
            catch (Exception)
            {
            }
            finally
            {
                Monitor.Exit(c.SendLock);
            }
        }

        private void DisconnectClient(Client client)
        {
            Log(string.Format("Disconnecting UID {0}", client.UID));
            try
            {
                //Remove first from lists so if we get a network exception at least on the server side stuff should be clean
                Unready(client.UID);
                lock (_clientLock)
                {
                    Clients.Remove(client.UID);
                    Unidentified.Remove(client);
                    RemovePlayerFromSession(client);
                }
                SendMessage(new MWDisconnectMessage(), client);
                //Wait a bit to give the message a chance to be sent at least before closing the client
                Thread.Sleep(10);
                client.TcpClient.Close();
            }
            catch(Exception e)
            {
                //Do nothing, we're already disconnecting
                Log("Exception disconnecting client: " + e);
            }
        }

        private void RemovePlayerFromSession(Client client)
        {
            if (client.Session != null)
            {
                GameSessions[client.Session.randoId].RemovePlayer(client);

                // TODO: Leaving this out for now, meaning fully async multiworlds should be possible
                // Maybe put a timeout on sessions so they don't last forever, but for now this is ok

                /*if (GameSessions[client.Session.randoId].isEmpty())
                {
                    Log($"Removing session for rando id: {client.Session.randoId}");
                    GameSessions.Remove(client.Session.randoId);
                }*/
                client.Session = null;
            }
        }

        private void ReadFromClient(Client sender, MWPackedMessage packed)
        {
            MWMessage message;
            try
            {
                message = Packer.Unpack(packed);
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return;
            }

            switch (message.MessageType)
            {
                case MWMessageType.SharedCore:
                    break;
                case MWMessageType.ConnectMessage:
                    HandleConnect(sender, (MWConnectMessage)message);
                    break;
                case MWMessageType.ReconnectMessage:
                    break;
                case MWMessageType.DisconnectMessage:
                    HandleDisconnect(sender, (MWDisconnectMessage)message);
                    break;
                case MWMessageType.JoinMessage:
                    HandleJoin(sender, (MWJoinMessage)message);
                    break;
                case MWMessageType.JoinConfirmMessage:
                    break;
                case MWMessageType.LeaveMessage:
                    HandleLeaveMessage(sender, (MWLeaveMessage)message);
                    break;
                case MWMessageType.ItemReceiveMessage:
                    break;
                case MWMessageType.ItemReceiveConfirmMessage:
                    HandleItemReceiveConfirm(sender, (MWItemReceiveConfirmMessage)message);
                    break;
                case MWMessageType.ItemSendMessage:
                    HandleItemSend(sender, (MWItemSendMessage)message);
                    break;
                case MWMessageType.ItemSendConfirmMessage:
                    break;
                case MWMessageType.NotifyMessage:
                    HandleNotify(sender, (MWNotifyMessage)message);
                    break;
                case MWMessageType.PingMessage:
                    HandlePing(sender, (MWPingMessage)message);
                    break;
                case MWMessageType.ReadyMessage:
                    HandleReadyMessage(sender, (MWReadyMessage)message);
                    break;
                case MWMessageType.UnreadyMessage:
                    HandleUnreadyMessage(sender, (MWUnreadyMessage)message);
                    break;
                case MWMessageType.StartMessage:
                    HandleStartMessage(sender, (MWStartMessage)message);
                    break;
                case MWMessageType.SaveMessage:
                    HandleSaveMessage(sender, (MWSaveMessage)message);
                    break;
                case MWMessageType.InvalidMessage:
                default:
                    throw new InvalidOperationException("Received Invalid Message Type");
            }
        }

        private void HandleConnect(Client sender, MWConnectMessage message)
        {
            //Log(string.Format("Seeing connection with UID={0}", message.SenderUid));
            lock (_clientLock)
            {
                if (Unidentified.Contains(sender))
                {
                    if (message.SenderUid == 0)
                    {
                        sender.UID = nextUID++;
                        Log(string.Format("Assigned UID={0}", sender.UID));
                        SendMessage(new MWConnectMessage {SenderUid = sender.UID}, sender);
                        Log("Connect sent!");
                        Clients.Add(sender.UID, sender);
                        Unidentified.Remove(sender);
                    }
                    else
                    {
                        Unidentified.Remove(sender);
                        sender.TcpClient.Close();
                    }
                }
            }
        }

        private void HandleDisconnect(Client sender, MWDisconnectMessage message)
        {
            DisconnectClient(sender);
        }

        private void HandlePing(Client sender, MWPingMessage message)
        {
            sender.lastPing = DateTime.Now;
        }

        private void HandleJoin(Client sender, MWJoinMessage message)
        {
            lock (_clientLock)
            {
                if (!Clients.ContainsKey(sender.UID))
                {
                    return;
                }

                if (!GameSessions.ContainsKey(message.RandoId))
                {
                    Log($"Starting session for rando id: {message.RandoId}");
                    GameSessions[message.RandoId] = new GameSession(message.RandoId);
                }

                GameSessions[message.RandoId].AddPlayer(sender, message);
                SendMessage(new MWJoinConfirmMessage(), sender);
            }
        }

        private void HandleLeaveMessage(Client sender, MWLeaveMessage message)
        {
            RemovePlayerFromSession(sender);
        }

        private void HandleReadyMessage(Client sender, MWReadyMessage message)
        {
            sender.Nickname = message.Nickname;
            sender.Room = message.Room;
            lock (ready)
            {
                if (!ready.ContainsKey(sender.Room))
                {
                    ready[sender.Room] = new Dictionary<ulong, RandoSettings>();
                }

                ready[sender.Room][sender.UID] = message.Settings;

                string roomText = string.IsNullOrEmpty(sender.Room) ? "default room" : $"room \"{sender.Room}\"";
                Log($"{sender.Nickname} (UID {sender.UID}) readied up in {roomText} ({ready[sender.Room].Count} readied)");

                string names = "";
                foreach (ulong uid in ready[sender.Room].Keys)
                {
                    names += Clients[uid].Nickname;
                    names += ", ";
                }

                if (names.Length >= 2)
                {
                    names = names.Substring(0, names.Length - 2);
                }

                foreach (ulong uid in ready[sender.Room].Keys)
                {
                    SendMessage(new MWNumReadyMessage { Ready = ready[sender.Room].Count, Names = names }, Clients[uid]);
                }
            }
        }

        private void Unready(ulong uid)
        {
            if (!Clients.TryGetValue(uid, out Client c)) return;

            lock (ready)
            {
                if (c.Room == null || !ready.ContainsKey(c.Room) || !ready[c.Room].ContainsKey(uid)) return;
                string roomText = string.IsNullOrEmpty(c.Room) ? "default room" : $"room \"{c.Room}\"";
                Log($"{c.Nickname} (UID {c.UID}) unreadied from {roomText} ({ready[c.Room].Count - 1} readied)");

                ready[c.Room].Remove(c.UID);
                if (ready[c.Room].Count == 0)
                {
                    ready.Remove(c.Room);
                    return;
                }

                string names = "";
                foreach (ulong uid2 in ready[c.Room].Keys)
                {
                    names += Clients[uid2].Nickname;
                    names += ", ";
                }

                if (names.Length >= 2)
                {
                    names = names.Substring(0, names.Length - 2);
                }

                foreach (ulong uid2 in ready[c.Room].Keys)
                {
                    if (!Clients.ContainsKey(uid2)) continue;
                    SendMessage(new MWNumReadyMessage { Ready = ready[c.Room].Count, Names = names }, Clients[uid2]);
                }
            }
        }

        private void HandleUnreadyMessage(Client sender, MWUnreadyMessage message)
        {
            Unready(sender.UID);
        }

        private void HandleSaveMessage(Client sender, MWSaveMessage message)
        {
            if (sender.Session == null) return;

            GameSessions[sender.Session.randoId].Save(sender.Session.playerId);
        }

        private void HandleStartMessage(Client sender, MWStartMessage message)
        {
            List<Client> clients = new List<Client>();
            List<RandoSettings> settings = new List<RandoSettings>();
            List<string> nicknames = new List<string>();

            if (sender.Room == null) return;

            string room = sender.Room;

            if (!ready.ContainsKey(room)) return;

            string roomText = string.IsNullOrEmpty(sender.Room) ? "default room" : $"room \"{sender.Room}\"";
            Log($"Starting MW for {roomText} at request of {sender.Nickname}");

            lock (ready)
            {
                if (!ready[room].ContainsKey(sender.UID)) return;

                // Ensure the person who called this is first, so their seed is used
                clients.Add(sender);
                settings.Add(ready[room][sender.UID]);
                nicknames.Add(sender.Nickname);

                ready[room].Remove(sender.UID);

                foreach (var kvp in ready[room])
                {
                    clients.Add(Clients[kvp.Key]);
                    settings.Add(kvp.Value);
                    nicknames.Add(Clients[kvp.Key].Nickname);
                }

                ready.Remove(room);
            }

            Log("Starting rando with players:");
            foreach (string nickname in nicknames)
            {
                Log(nickname);
            }

            Log("Randomizing world...");
            MWRandomizer randomizer = new MWRandomizer(settings);
            List<RandoResult> results = randomizer.RandomizeMW();
            Log("Done randomization");

            Log("Sending to players...");
            for (int i = 0; i < results.Count; i++)
            {
                results[i].nicknames = nicknames;
                Log($"Sending to player {i + 1}");
                SendMessage(new MWResultMessage { Result = results[i] }, clients[i]);
            }
            Log($"Done sending to players!");
        }

        private void HandleNotify(Client sender, MWNotifyMessage message)
        {
            Log($"[{sender.Session?.Name}]: {message.Message}");
        }

        private void HandleItemReceiveConfirm(Client sender, MWItemReceiveConfirmMessage message)
        {
            List<MWMessage> confirmed = sender.Session.ConfirmMessage(message);

            foreach (MWMessage msg in confirmed)
            {
                switch (msg.MessageType)
                {
                    case MWMessageType.ItemReceiveMessage:
                        MWItemReceiveMessage itemMsg = msg as MWItemReceiveMessage;
                        GameSessions[sender.Session.randoId].ConfirmItem(sender.Session.playerId, itemMsg);
                        break;
                    default:
                        break;
                }
            }
        }

        private void HandleItemSend(Client sender, MWItemSendMessage message)
        {
            if (sender.Session == null) return;  // Throw error?

            //Confirm sending the item to the sender
            SendMessage(new MWItemSendConfirmMessage { Location = message.Location, Item = message.Item, To = message.To}, sender);
            GameSessions[sender.Session.randoId].SendItemTo(message.To, message.Item, message.Location, sender.Session.Name);
        }

        private Client GetClient(ulong uuid)
        {
            lock (_clientLock)
            {
                return Clients.TryGetValue(uuid, out Client client) ? client : null;
            }
        }
    }
}
