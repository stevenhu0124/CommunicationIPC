namespace CommunicationIPC.Models
{
    public enum ConnectionActions
    {
        RequestPrimaryServerPorts,
        RequestNewPortConnected,
    }

    public class ConnectionModel
    {
        public string Message { get; set; }

        public ConnectionActions Action { get; set; }

        public int Sender { get; set; }
    }
}
