using System;
using MultiWorldProtocol.Messaging;
using MultiWorldProtocol.Messaging.Definitions;

using RandomizerLib;

namespace MultiWorldProtocol.Messaging.Definitions.Messages
{
    [MWMessageType(MWMessageType.ReadyMessage)]
    public class MWReadyMessage : MWMessage
    {
        public string Nickname { get; set; }
        public RandoSettings Settings { get; set; }

        public MWReadyMessage()
        {
            MessageType = MWMessageType.ReadyMessage;
        }
    }

    public class MWReadyMessageDefinition : MWMessageDefinition<MWReadyMessage>
    {
        public MWReadyMessageDefinition() : base(MWMessageType.ReadyMessage)
        {
            Properties.Add(new MWMessageProperty<string, MWReadyMessage>(nameof(MWReadyMessage.Nickname)));
            Properties.Add(new MWMessageProperty<RandoSettings, MWReadyMessage>(nameof(MWReadyMessage.Settings)));
        }
    }
}
