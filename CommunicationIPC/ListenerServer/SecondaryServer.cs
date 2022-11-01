using CommunicationIPC.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace CommunicationIPC.ListenerServer
{
    internal sealed class SecondaryServer : HttpListenerServerBase
    {
        private readonly Tuple<int, int> SECONDARY_PORTS_RANGE = Tuple.Create(8880, 8890);

        /// <summary>
        /// InitServer server and get server state
        /// </summary>
        /// <returns>Server state</returns>
        internal override ConnectionState InitServer()
        {
            for (int secondaryPort = SECONDARY_PORTS_RANGE.Item1; secondaryPort < SECONDARY_PORTS_RANGE.Item2; secondaryPort++)
            {
                if (!IsPortUsing(secondaryPort))
                {
                    return new ConnectionState() { IsServerRunning = false, ServerPort = secondaryPort };
                }
            }

            throw new Exception("The DLL on this computer has reached its maximum usage limit");
        }

        /// <summary>
        /// Handle the receive data
        /// </summary>
        /// <param name="context">HttpListenerContext</param>
        /// <param name="recevieData">Receive data</param>
        protected override void HandleReceiveData(HttpListenerContext context, ConnectionModel recevieData)
        {
            switch (recevieData.Action)
            {
                case ConnectionActions.RequestNewPortConnected:
                    HandleRequestNewPortConnected(context, recevieData);
                    break;
                default:
                    break;
            }

        }


        /// <summary>
        /// Request all server port's from primary server
        /// </summary>
        public void RequestPortsFromPrimary(int primaryPort)
        {
            try
            {
                var httpWebRequest = RequestServerResponse(primaryPort, ConnectionActions.RequestPrimaryServerPorts, string.Empty);
                var recevieData = ReceiveServerResponse(httpWebRequest);

                if (recevieData.Action == ConnectionActions.RequestPrimaryServerPorts)
                {
                    ClientServerPorts = JsonSerializer.Deserialize<List<int>>(recevieData.Message);
                }
                else
                {
                    throw new Exception("invalid response");
                }
            }
            catch(Exception ex)
            {
                Debug.Write(ex.ToString());
            }
        }
    }
}
