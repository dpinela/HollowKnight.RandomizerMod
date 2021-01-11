using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MultiWorldProtocol.Messaging.Definitions.Messages;

namespace MultiWorldServer
{
    class GameSession
    {
        private int randoId;
        private Dictionary<int, PlayerSession> players;
        private Dictionary<int, List<MWItemReceiveMessage>> unsentItems;

        public GameSession(int id)
        {
            randoId = id;
            players = new Dictionary<int, PlayerSession>();
            unsentItems = new Dictionary<int, List<MWItemReceiveMessage>>();
        }

        public void AddPlayer(Client c, MWJoinMessage join)
        {
            PlayerSession session = new PlayerSession(join.DisplayName, join.RandoId, join.PlayerId);
            players[join.PlayerId] = session;
            c.Session = session;

            Server.Log($"Player {join.PlayerId + 1} joined session {join.RandoId}");

            if (unsentItems.ContainsKey(join.PlayerId))
            {
                foreach (var msg in unsentItems[join.PlayerId])
                {
                    players[join.PlayerId].QueueConfirmableMessage(msg);
                }
                unsentItems.Remove(join.PlayerId);
            }
        }

        public void RemovePlayer(Client c)
        {
            Server.Log($"Player {c.Session.playerId + 1} removed from session {c.Session.randoId}");
            players.Remove(c.Session.playerId);

            // If someone has unconfirmed items, put them into unsent items to make sure they aren't lost
            foreach (ResendEntry re in c.Session.MessagesToConfirm)
            {
                if (re.Message.MessageType == MultiWorldProtocol.Messaging.MWMessageType.ItemReceiveMessage)
                {
                    if (!unsentItems.ContainsKey(c.Session.playerId))
                    {
                        unsentItems.Add(c.Session.playerId, new List<MWItemReceiveMessage>());
                    }
                    unsentItems[c.Session.playerId].Add((MWItemReceiveMessage) re.Message);
                }
            }
        }

        public bool isEmpty()
        {
            return players.Count == 0;
        }

        public void SendItemTo(int player, string item, string location, string from)
        {
            MWItemReceiveMessage msg = new MWItemReceiveMessage { Location = location, From = from, Item = item };
            if (players.ContainsKey(player))
            {
                Server.Log($"Sending item '{item}' to '{players[player].Name}', from '{from}'");

                players[player].QueueConfirmableMessage(msg);
            }
            else    // Trying to send to an offline player
            {
                Server.Log($"Queuing item '{item}' for offline player '{player + 1}', from '{from}'");
                if (!unsentItems.ContainsKey(player))
                {
                    unsentItems.Add(player, new List<MWItemReceiveMessage>());
                }
                unsentItems[player].Add(msg);
            }
        }

        public string getPlayerString()
        {
            if (players.Count == 0) return "";

            string playerString = "";
            foreach (var kvp in players)
            {
                playerString += $"{kvp.Key + 1}: {kvp.Value.Name}, ";
            }

            return playerString.Substring(0, playerString.Length - 2);
        }
    }
}
