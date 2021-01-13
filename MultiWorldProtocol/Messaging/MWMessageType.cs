namespace MultiWorldProtocol.Messaging
{
    public enum MWMessageType
    {
        InvalidMessage=0,
        SharedCore=1,
        ConnectMessage,
        ReconnectMessage,
        DisconnectMessage,
        JoinMessage,
        JoinConfirmMessage,
        LeaveMessage,
        ItemReceiveMessage,
        ItemReceiveConfirmMessage,
        ItemSendMessage,
        ItemSendConfirmMessage,
        NotifyMessage,
        NumReadyMessage,
        PingMessage,
        ReadyMessage,
        ResultMessage,
        SaveMessage,
        SetupMessage,
        StartMessage,
        UnreadyMessage
    }
}
