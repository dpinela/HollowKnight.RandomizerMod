using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MultiWorldProtocol.Messaging;
using MultiWorldProtocol.Messaging.Definitions.Messages;

namespace MultiWorldServer
{
    class PlayerSession
    {
        public string Name;
        public int randoId;
        public int playerId;

        public readonly List<ResendEntry> MessagesToConfirm = new List<ResendEntry>();

        public PlayerSession(string Name, int randoId, int playerId)
        {
            this.Name = Name;
            this.randoId = randoId;
            this.playerId = playerId;
        }

        public void QueueConfirmableMessage(MWMessage message)
        {
            if (message.MessageType != MWMessageType.ItemReceiveMessage)
            {
                throw new InvalidOperationException("Server should only queue ItemConfiguration and ItemReceive messages for confirmation");
            }
            lock (MessagesToConfirm)
            {
                MessagesToConfirm.Add(new ResendEntry(message));
            }
        }

        public void ConfirmMessage(MWMessage message)
        {
            if (message.MessageType == MWMessageType.ItemReceiveConfirmMessage)
            {
                ConfirmItemReceive((MWItemReceiveConfirmMessage)message);
            }
            else
            {
                throw new InvalidOperationException("Must only confirm ItemConfiguration and ItemReceive messages.");
            }
        }

        private void ConfirmItemReceive(MWItemReceiveConfirmMessage message)
        {
            lock (MessagesToConfirm)
            {
                for (int i = MessagesToConfirm.Count - 1; i >= 0; i--)
                {
                    MWItemReceiveMessage icm = MessagesToConfirm[i].Message as MWItemReceiveMessage;
                    if (icm.Item == message.Item && icm.From == message.From)
                    {
                        MessagesToConfirm.RemoveAt(i);
                    }
                }
            }
        }
    }
}
