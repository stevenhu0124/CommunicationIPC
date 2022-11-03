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
        internal static readonly Tuple<int, int> SECONDARY_PORTS_RANGE = Tuple.Create(8880, 8890);

        /// <summary>
        /// GetServerConnectionState server and get server state
        /// </summary>
        /// <returns>Server state</returns>
        internal override ConnectionState GetServerConnectionState()
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
                case ConnectionActions.SendSecondaryNewConnected:
                    HandleRequestNewPortConnected(context, recevieData);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handle request new requestPort connected, and trigger the ReceivedNotification event
        /// </summary>
        /// <param name="context"></param>
        /// <param name="recevieData"></param>
        private void HandleRequestNewPortConnected(HttpListenerContext context, ConnectionModel recevieData)
        {
            ClientServerPorts = JsonSerializer.Deserialize<List<int>>(recevieData.Message);

            ConnectionModel response = new ConnectionModel() 
            {
                Sender = CurrentPort.Value,
                Action = recevieData.Action,
                Message = recevieData.Message,
            };

            var responseJson = ConvertToJson(response);
            SendResponse(context, responseJson);
            TriggerNewServerConnected(string.Format("Connected Ports = {0}, Recevice by Sender = {1} -> HandleRequestNewPortConnected", recevieData.Message, recevieData.Sender));
        }


        /// <summary>
        /// Request all server port's from primary server
        /// </summary>
        internal void RequestConnectionToPrimaryServer(int primaryPort)
        {
            try
            {
                var httpWebRequest = RequestServerResponse(primaryPort, ConnectionActions.RequestPrimaryServerConnected, string.Empty);
                var recevieData = ReceiveServerResponse(httpWebRequest);

                if (recevieData.Action == ConnectionActions.RequestPrimaryServerConnected)
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
