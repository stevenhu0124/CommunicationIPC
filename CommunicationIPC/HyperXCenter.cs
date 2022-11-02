using CommunicationIPC.ListenerServer;
using System;
using System.Collections.Generic;

namespace CommunicationIPC
{
    /// <summary>
    /// Simulation of multiple APs using the same DLL
    /// </summary>
    public class HyperXCenter
    {
        private HttpListenerServerBase server;

        /// <summary>
        /// Received the message from others AP
        /// </summary>
        public event Action<string> ReceivedNotification;

        public void InitServer()
        {
            server = new PrimaryServer();
            var primaryInfo = server.InitServer();

            if (!primaryInfo.IsServerRunning)
            {
                server.CreateServer(primaryInfo.ServerPort);
                server.ReceivedNotification += Server_ReceivedNotification;
                var primaryServer = ((PrimaryServer)server);
                primaryServer.CheckSecondaryServerAlive();
            }
            else
            {
                int primaryPort = primaryInfo.ServerPort;

                server = new SecondaryServer();
                var secondaryInfo = server.InitServer();
                if (!secondaryInfo.IsServerRunning)
                {
                    server.CreateServer(secondaryInfo.ServerPort);
                    server.ReceivedNotification += Server_ReceivedNotification;
                    var secondaryServer = ((SecondaryServer)server);
                    secondaryServer.RequestPortsFromPrimary(primaryPort);
                }
            }
        }

        public int GetCurrentPort() 
        {
            return server.GetCurrentPort();
        }

        public List<int> GetAllPorts() 
        {
            return server.GetAllPorts();
        }

        private void Server_ReceivedNotification(string message)
        {
            ReceivedNotification?.Invoke(message);
        }
    }
}
