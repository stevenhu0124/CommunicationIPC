namespace CommunicationIPC.Models
{
    internal enum ConnectionActions
    {
        RequestPrimaryServerAlive,
        RequestPrimaryServerPorts,
        RequestNewPortConnected,
    }

    internal class ConnectionModel
    {
        public string Message { get; set; }

        public ConnectionActions Action { get; set; }

        public int Sender { get; set; }
    }
}
