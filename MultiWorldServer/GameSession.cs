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
        private Dictionary<int, List<(string, string)>> unsentItems;

        public GameSession(int id)
        {
            randoId = id;
            players = new Dictionary<int, PlayerSession>();
            unsentItems = new Dictionary<int, List<(string, string)>>();
        }

        public void AddPlayer(Client c, MWJoinMessage join)
        {
            PlayerSession session = new PlayerSession(join.DisplayName, join.RandoId, join.PlayerId);
            players[join.PlayerId] = session;
            c.Session = session;

            Server.Log($"Player {join.PlayerId + 1} joined session {join.RandoId}");

            if (unsentItems.ContainsKey(join.PlayerId))
            {
                foreach ((string Item, string From) in unsentItems[join.PlayerId])
                {
                    players[join.PlayerId].QueueConfirmableMessage(new MWItemReceiveMessage { From = From, Item = Item });
                }
                unsentItems.Remove(join.PlayerId);
            }
        }

        public void RemovePlayer(Client c)
        {
            Server.Log($"Player {c.Session.playerId + 1} removed from session {c.Session.randoId}");
            players.Remove(c.Session.playerId);
        }

        public bool isEmpty()
        {
            return players.Count == 0;
        }

        public void SendItemTo(int player, string item, string from)
        {
            if (players.ContainsKey(player))
            {
                Server.Log($"Sending item '{item}' to '{players[player].Name}', from '{from}'");

                players[player].QueueConfirmableMessage(new MWItemReceiveMessage { From = from, Item = item });
            }
            else    // Trying to send to an offline player
            {
                Server.Log($"Queuing item '{item}' for offline player '{player + 1}', from '{from}'");
                if (!unsentItems.ContainsKey(player))
                {
                    unsentItems.Add(player, new List<(string, string)>());
                }
                unsentItems[player].Add((item, from));
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
