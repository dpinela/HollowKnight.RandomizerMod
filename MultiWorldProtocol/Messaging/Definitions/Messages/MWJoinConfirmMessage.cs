using System;
using MultiWorldProtocol.Messaging;
using MultiWorldProtocol.Messaging.Definitions;

namespace MultiWorldProtocol.Messaging.Definitions.Messages
{
    [MWMessageType(MWMessageType.JoinConfirmMessage)]
    public class MWJoinConfirmMessage : MWMessage
    {
        public MWJoinConfirmMessage()
        {
            MessageType = MWMessageType.JoinConfirmMessage;
        }
    }

    public class MWJoinConfirmMessageDefinition : MWMessageDefinition<MWJoinConfirmMessage>
    {
        public MWJoinConfirmMessageDefinition() : base(MWMessageType.JoinConfirmMessage)
        {
        }
    }
}
