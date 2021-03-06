using System;
using MultiWorldProtocol.Messaging;
using MultiWorldProtocol.Messaging.Definitions;

namespace MultiWorldProtocol.Messaging.Definitions.Messages
{
    [MWMessageType(MWMessageType.SaveMessage)]
    public class MWSaveMessage : MWMessage
    {
        public int ReadyID { get; set; }
        public MWSaveMessage()
        {
            MessageType = MWMessageType.SaveMessage;
        }
    }

    public class MWSaveMessageDefinition : MWMessageDefinition<MWSaveMessage>
    {
        public MWSaveMessageDefinition() : base(MWMessageType.SaveMessage)
        {
            Properties.Add(new MWMessageProperty<int, MWSaveMessage>(nameof(MWSaveMessage.ReadyID)));
        }
    }
}
