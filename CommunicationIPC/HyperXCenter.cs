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
            var primaryState = server.GetServerConnectionState();

            if (!primaryState.IsServerRunning)
            {
                server.CreateServer(primaryState.ServerPort);
                server.ReceivedNotification += Server_ReceivedNotification;
                ((PrimaryServer)server).SendPortsToSecondaryServers();
            }
            else
            {
                int primaryPort = primaryState.ServerPort;

                server = new SecondaryServer();
                var secondaryState = server.GetServerConnectionState();
                if (!secondaryState.IsServerRunning)
                {
                    server.CreateServer(secondaryState.ServerPort);
                    server.ReceivedNotification += Server_ReceivedNotification;
                    ((SecondaryServer)server).RequestConnectionToPrimaryServer(primaryPort);
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
