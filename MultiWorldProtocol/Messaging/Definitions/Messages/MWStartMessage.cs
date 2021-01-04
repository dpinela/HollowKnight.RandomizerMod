using System;
using Modding;

namespace MultiWorldProtocol.Messaging.Definitions.Messages
{
    [MWMessageType(MWMessageType.StartMessage)]
    public class MWStartMessage : MWMessage
    {
        public MWStartMessage()
        {
            MessageType = MWMessageType.StartMessage;
        }
    }

    public class MWStartMessageDefinition : MWMessageDefinition<MWConnectMessage>
    {
        public MWStartMessageDefinition() : base(MWMessageType.StartMessage)
        {
        }
    }
}
