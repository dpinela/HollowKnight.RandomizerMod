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

        public void SendItemTo(int player, string Item, string From)
        {
            if (players.ContainsKey(player))
            {
                Server.Log($"Sending item '{Item}' to '{players[player].Name}', from '{From}'");

                players[player].QueueConfirmableMessage(new MWItemReceiveMessage { From = From, Item = Item });
            }
            else    // Trying to send to an offline player
            {
                Server.Log($"Queuing item '{Item}' for offline player '{players[player].Name}', from '{From}'");
                if (!unsentItems.ContainsKey(player))
                {
                    unsentItems.Add(player, new List<(string, string)>());
                    unsentItems[player].Add((Item, From));
                }
            }
        }
    }
}
