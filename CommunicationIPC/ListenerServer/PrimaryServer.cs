using CommunicationIPC.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace CommunicationIPC.ListenerServer
{
    internal sealed class PrimaryServer : HttpListenerServerBase
    {
        private readonly Tuple<int, int> PRIMARY_PORTS_RANGE = Tuple.Create(9990, 10000);
      
        /// <summary>
        /// GetServerConnectionState server and get server state
        /// </summary>
        /// <returns>Server state</returns>
        internal override ConnectionState GetServerConnectionState()
        {
            for (int primaryPort = PRIMARY_PORTS_RANGE.Item1; primaryPort < PRIMARY_PORTS_RANGE.Item2; primaryPort++)
            {
                if (IsPortUsing(primaryPort))
                {
                    if (RequestServerAlive(primaryPort))
                    {
                        return new ConnectionState() { IsServerRunning = true, ServerPort = primaryPort };
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    return new ConnectionState() { IsServerRunning = false, ServerPort = primaryPort };
                }
            }

            throw new Exception("The DLL on this computer has reached its maximum usage limit");
        }

        /// <summary>
        /// To check is secondary server is alive and update the new server list
        /// </summary>
        internal void SendPortsToSecondaryServers()
        {
            for (int secondaryPort = SecondaryServer.SECONDARY_PORTS_RANGE.Item1; secondaryPort < SecondaryServer.SECONDARY_PORTS_RANGE.Item2; secondaryPort++)
            {
                if (IsPortUsing(secondaryPort) && RequestServerAlive(secondaryPort))
                {
                    if (!ClientServerPorts.Contains(secondaryPort))
                    {
                        ClientServerPorts.Add(secondaryPort);
                    }
                }
            }

            NotifyNewServerConnected(CurrentPort.Value);
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
                case ConnectionActions.RequestPrimaryServerConnected:
                    CheckServersAlvie(recevieData.Sender);
                    ConnectionModel response = new ConnectionModel()
                    {
                        Action = recevieData.Action,
                        Sender = CurrentPort.Value,
                        Message = ConvertToJson(ClientServerPorts)
                    };

                    string responseJson = ConvertToJson(response);
                    SendResponse(context, responseJson);

                    NotifyNewServerConnected(recevieData.Sender);
                    break;
                default:
                    break;
            }
        }

        private void CheckServersAlvie(int senderPort)
        {
            if (!ClientServerPorts.Contains(senderPort))
            {
                ClientServerPorts.Add(senderPort);
            }

            List<int> unavailablePorts = new List<int>();
            foreach (var port in ClientServerPorts.Where(x => x != CurrentPort && x != senderPort))
            {
                if (!IsPortUsing(port))
                {
                    unavailablePorts.Add(port);
                }
            }

            foreach (var port in unavailablePorts)
            {
                ClientServerPorts.Remove(port);
            }
        }

        /// <summary>
        /// Notify all servers with new server connected
        /// </summary>
        /// <param name="senderPort">Sender port</param>
        private void NotifyNewServerConnected(int senderPort)
        {
            foreach (var port in ClientServerPorts.Where(x => x != CurrentPort && x != senderPort))
            {
                try
                {
                    var httpWebRequest = RequestServerResponse(port, ConnectionActions.SendSecondaryNewConnected, ConvertToJson(ClientServerPorts));
                    var recevieData = ReceiveServerResponse(httpWebRequest);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            if (senderPort != CurrentPort)
            {
                var message = string.Join(",", ClientServerPorts);
                TriggerNewServerConnected(string.Format("Connected Ports = [{0}], Trigger by Sender = {1} -> NotifyNewServerConnected", message, senderPort));
            }
        }
    }
}
