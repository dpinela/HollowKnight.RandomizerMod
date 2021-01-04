using System;
using System.Collections.Generic;
using MultiWorldProtocol.Binary;
using MultiWorldProtocol.Messaging;
using MultiWorldProtocol.Messaging.Definitions.Messages;
using System.Net.Sockets;
using System.Threading;
using Modding;
using static RandomizerMod.LogHelper;
using Newtonsoft.Json;

using RandomizerLib;

namespace RandomizerMod.MultiWorld
{
    public class ClientConnection
    {
        private const int PING_INTERVAL = 10000;

        private readonly MWMessagePacker Packer = new MWMessagePacker(new BinaryMWMessageEncoder());
        private TcpClient _client;
        private Timer PingTimer;
        private ConnectionState State;
        private List<MWItemSendMessage> ItemSendQueue = new List<MWItemSendMessage>();
        private Thread ReadThread;

        public delegate void ItemReceiveEvent(string from, string itemName);
        public delegate void NumReadyEvent(int num);

        public event ItemReceiveEvent ItemReceived;
        public event NumReadyEvent NumReadyReceived;

        public RandoResult LastResult = null;

        private List<MWMessage> messageEventQueue = new List<MWMessage>();

        public ClientConnection()
        {
            State = new ConnectionState();

            ModHooks.Instance.HeroUpdateHook += SynchronizeEvents;
        }

        public void Connect()
        {
            if (_client != null && _client.Connected)
            {
                Disconnect();
            }

            Reconnect();
        }

        private void Reconnect()
        {
            if (_client != null && _client.Connected)
            {
                return;
            }

            Log("Attempting to connect to server");

            State.Uid = 0;
            State.LastPing = DateTime.Now;

            _client = new TcpClient
            {
                ReceiveTimeout = 2000,
                SendTimeout = 2000
            };

            _client.Connect(RandomizerMod.Instance.MWSettings.IP, RandomizerMod.Instance.MWSettings.Port);

            if (ReadThread != null && ReadThread.IsAlive)
            {
                ReadThread.Abort();
            }

            PingTimer = new Timer(DoPing, State, 1000, PING_INTERVAL);

            ReadThread = new Thread(ReadWorker);
            ReadThread.Start();

            SendMessage(new MWConnectMessage());
            Log("Connected to server!");
        }

        public void JoinRando(int randoId, int playerId)
        {
            Log("Joining rando session");
            Log(RandomizerMod.Instance.MWSettings.UserName);
            Log(randoId);
            Log(playerId);
            SendMessage(new MWJoinMessage
            {
                DisplayName = RandomizerMod.Instance.MWSettings.UserName,
                RandoId = randoId,
                PlayerId = playerId
            });
        }

        public void Disconnect()
        {
            Log("Disconnecting from server");
            PingTimer?.Dispose();

            try
            {
                ReadThread.Abort();
                ReadThread = null;
                Log($"Disconnecting (UID = {State.Uid})");
                byte[] buf = Packer.Pack(new MWDisconnectMessage {SenderUid = State.Uid}).Buffer;
                _client.GetStream().Write(buf, 0, buf.Length);
                _client.Close();
            }
            catch (Exception e)
            {
                Log("Error disconnection:\n" + e);
            }
            finally
            {
                State.Connected = false;
                State.Joined = false;
                _client = null;
                messageEventQueue.Clear();
            }
        }

        private void SynchronizeEvents()
        {
            MWMessage message = null;

            lock (messageEventQueue)
            {
                if (messageEventQueue.Count > 0)
                {
                    message = messageEventQueue[0];
                    messageEventQueue.RemoveAt(0);
                }
            }

            if (message == null)
            {
                return;
            }

            switch (message)
            {
                case MWItemReceiveMessage item:
                    GiveItemActions.GiveItemMW(item.Item, item.From);
                    break;
                default:
                    Log("Unknown type in message queue: " + message.MessageType);
                    break;
            }
        }

        private void DoPing(object state)
        {
            if (_client == null || !_client.Connected)
            {
                if (State.Connected)
                {
                    State.Connected = false;
                    State.Joined = false;

                    Log("Disconnected from server");
                }

                Reconnect();
            }

            if (State.Connected)
            {
                if (DateTime.Now - State.LastPing > TimeSpan.FromMilliseconds(PING_INTERVAL * 3.5))
                {
                    Log("Connection timed out");

                    Disconnect();
                    Reconnect();
                }
                else
                {
                    SendMessage(new MWPingMessage());
                    //If there are items in the queue that the server hasn't confirmed yet
                    if (ItemSendQueue.Count > 0 && State.Joined)
                    {
                        ResendItemQueue();
                    }
                }
            }
        }

        private void SendMessage(MWMessage msg)
        {
            try
            {
                //Always set Uid in here, if uninitialized will be 0 as required.
                //Otherwise less work resuming session etc.
                msg.SenderUid = State.Uid;
                byte[] bytes = Packer.Pack(msg).Buffer;
                NetworkStream stream = _client.GetStream();
                stream.BeginWrite(bytes, 0, bytes.Length, WriteToServer, stream);
            }
            catch (Exception e)
            {
                Log($"Failed to send message '{msg}' to server:\n{e}");
            }
        }

        private void ReadWorker()
        {
            NetworkStream stream = _client.GetStream();
            while(true)
            {
                var message = new MWPackedMessage(stream);
                ReadFromServer(message);
            }
        }

        private void WriteToServer(IAsyncResult res)
        {
            NetworkStream stream = (NetworkStream)res.AsyncState;
            stream.EndWrite(res);
        }

        private void ReadFromServer(MWPackedMessage packed)
        {
            MWMessage message;
            try
            {
                message = Packer.Unpack(packed);
            }
            catch (Exception e)
            {
                Log(e);
                return;
            }

            switch (message.MessageType)
            {
                case MWMessageType.SharedCore:
                    break;
                case MWMessageType.ConnectMessage:
                    HandleConnect((MWConnectMessage)message);
                    break;
                case MWMessageType.ReconnectMessage:
                    break;
                case MWMessageType.DisconnectMessage:
                    HandleDisconnectMessage((MWDisconnectMessage)message);
                    break;
                case MWMessageType.JoinMessage:
                    break;
                case MWMessageType.JoinConfirmMessage:
                    HandleJoinConfirm((MWJoinConfirmMessage)message);
                    break;
                case MWMessageType.LeaveMessage:
                    HandleLeaveMessage((MWLeaveMessage)message);
                    break;
                case MWMessageType.ItemReceiveMessage:
                    HandleItemReceive((MWItemReceiveMessage)message);
                    break;
                case MWMessageType.ItemReceiveConfirmMessage:
                    break;
                case MWMessageType.ItemSendMessage:
                    break;
                case MWMessageType.ItemSendConfirmMessage:
                    HandleItemSendConfirm((MWItemSendConfirmMessage)message);
                    break;
                case MWMessageType.NotifyMessage:
                    HandleNotify((MWNotifyMessage)message);
                    break;
                case MWMessageType.PingMessage:
                    State.LastPing = DateTime.Now;
                    break;
                case MWMessageType.NumReadyMessage:
                    NumReadyReceived?.Invoke(((MWNumReadyMessage)message).Ready);
                    break;
                case MWMessageType.ResultMessage:
                    HandleResult((MWResultMessage)message);
                    break;
                case MWMessageType.InvalidMessage:
                default:
                    throw new InvalidOperationException("Received Invalid Message Type");
            }
        }

        private void ResendItemQueue()
        {
            foreach(MWItemSendMessage message in ItemSendQueue)
            {
                SendMessage(message);
            }
        }

        private void ClearFromSendQueue(int playerId, string item)
        {
            for(int i=ItemSendQueue.Count-1; i>=0; i--)
            {
                var queueItem = ItemSendQueue[i];
                if (playerId == queueItem.To && item == queueItem.Item)
                    ItemSendQueue.RemoveAt(i);
            }
        }

        private void HandleConnect(MWConnectMessage message)
        {
            State.Uid = message.SenderUid;
            State.Connected = true;
            Log($"Connected! (UID = {State.Uid})");
        }

        private void HandleJoinConfirm(MWJoinConfirmMessage message)
        {
            State.Joined = true;
        }

        private void HandleLeaveMessage(MWLeaveMessage message)
        {
            State.Joined = false;
        }

        private void HandleDisconnectMessage(MWDisconnectMessage message)
        {
            State.Connected = false;
            State.Joined = false;
        }

        private void HandleNotify(MWNotifyMessage message)
        {
            lock (messageEventQueue)
            {
                messageEventQueue.Add(message);
            }
        }

        private void HandleItemReceive(MWItemReceiveMessage message)
        {
            lock (messageEventQueue)
            {
                messageEventQueue.Add(message);
            }

            //Do whatever we want to do when we get an item here, then confirm
            SendMessage(new MWItemReceiveConfirmMessage { Item = message.Item, From = message.From });
        }

        private void HandleItemSendConfirm(MWItemSendConfirmMessage message)
        {
            ClearFromSendQueue(message.To, message.Item);
        }

        private void HandleResult(MWResultMessage message)
        {
            RandoResult result = message.Result;
            LastResult = result;
        }

        public void ReadyUp()
        {
            SendMessage(new MWReadyMessage { Settings = RandomizerMod.Instance.Settings.RandomizerSettings });
        }

        public void Unready()
        {
            SendMessage(new MWUnreadyMessage());
        }

        public void Start()
        {
            LastResult = null;
            SendMessage(new MWStartMessage());
        }

        public void SendItem(string loc, string item, int playerId)
        {
            Log($"Sending item {item} to {playerId}");
            ItemSendQueue.Add(new MWItemSendMessage { Item = item, To = playerId });
        }

        /*private void GiveItem(string from, string item)
        {
            ItemReceived?.Invoke(from, item);
        }*/

        public ConnectionStatus GetStatus()
        {
            if (!State.Connected)
            {
                return ConnectionStatus.NotConnected;
            }

            if (!State.Joined)
            {
                return ConnectionStatus.Unready;
            }

            return ConnectionStatus.Connected;
        }

        public enum ConnectionStatus
        {
            NotConnected,
            Unready,
            Ready,
            Connected
        }
    }
}
