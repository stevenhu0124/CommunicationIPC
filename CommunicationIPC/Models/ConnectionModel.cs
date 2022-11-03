namespace CommunicationIPC.Models
{
    internal enum ConnectionActions
    {
        /// <summary>
        ///  Check primary or secondary have response 
        /// </summary>
        RequestServerAlive,

        /// <summary>
        /// Secondary -> Primary
        /// </summary>
        RequestPrimaryServerConnected,

        /// <summary>
        /// Primary -> Secondary
        /// </summary>
        SendSecondaryNewConnected,
    }

    internal class ConnectionModel
    {
        public string Message { get; set; }

        public ConnectionActions Action { get; set; }

        public int Sender { get; set; }
    }
}
