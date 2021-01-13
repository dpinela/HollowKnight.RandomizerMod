﻿using System;
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

        // These are to try to prevent items being lost. When items are sent, they go to unconfirmed. Once the confirmation message is received,
        // they are moved to unsaved items. When we receive a message letting us know that 
        private Dictionary<int, List<MWItemReceiveMessage>> unconfirmedItems;
        private Dictionary<int, List<MWItemReceiveMessage>> unsavedItems;

        public GameSession(int id)
        {
            randoId = id;
            players = new Dictionary<int, PlayerSession>();
            unconfirmedItems = new Dictionary<int, List<MWItemReceiveMessage>>();
            unsavedItems = new Dictionary<int, List<MWItemReceiveMessage>>();
        }

        // We know that the client received the message, but until the game is saved we can't be sure it isn't lost in a crash
        public void ConfirmItem(int playerId, MWItemReceiveMessage msg)
        {
            unconfirmedItems.GetOrCreateDefault(playerId).Remove(msg);
            unsavedItems.GetOrCreateDefault(playerId).Add(msg);
        }

        // If items have been both confirmed and the player saves and we STILL lose the item, they didn't deserve it anyway
        public void Save(int playerId)
        {
            if (!unsavedItems.ContainsKey(playerId)) return;
            unsavedItems[playerId].Clear();
        }

        public void AddPlayer(Client c, MWJoinMessage join)
        {
            PlayerSession session = new PlayerSession(join.DisplayName, join.RandoId, join.PlayerId);
            players[join.PlayerId] = session;
            c.Session = session;

            Server.Log($"Player {join.PlayerId + 1} joined session {join.RandoId}");

            if (unconfirmedItems.ContainsKey(join.PlayerId))
            {
                foreach (var msg in unconfirmedItems[join.PlayerId])
                {
                    Server.Log($"Resending {msg.Item} to {join.PlayerId} on join");
                    players[join.PlayerId].QueueConfirmableMessage(msg);
                }
            }
        }

        public void RemovePlayer(Client c)
        {
            Server.Log($"Player {c.Session.playerId + 1} removed from session {c.Session.randoId}");
            players.Remove(c.Session.playerId);

            // If there are unsaved items when player is leaving, copy them to unconfirmed to be resent later
            if (unsavedItems.ContainsKey(c.Session.playerId))
            {
                unconfirmedItems.GetOrCreateDefault(c.Session.playerId).AddRange(unsavedItems[c.Session.playerId]);
                unsavedItems[c.Session.playerId].Clear();
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

            // Always add to unconfirmed, which doubles as holding items for offline players
            unconfirmedItems.GetOrCreateDefault(player).Add(msg);
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
