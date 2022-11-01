using CommunicationIPC.Models;
using System;
using System.Net;

namespace CommunicationIPC.ListenerServer
{
    internal sealed class PrimaryServer : HttpListenerServerBase
    {
        private readonly Tuple<int, int> PRIMARY_PORTS_RANGE = Tuple.Create(9990, 10000);
        /// <summary>
        /// InitServer server and get server state
        /// </summary>
        /// <returns>Server state</returns>
        internal override ConnectionState InitServer()
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
                    return new ConnectionState() { IsServerRunning = false, ServerPort = primaryPort};
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
                case ConnectionActions.RequestPrimaryServerAlive:
                    recevieData.Sender = CurrentPort.Value;
                    var json = ConvertToJson(recevieData);
                    SendResponse(context, json);

                    break;
                case ConnectionActions.RequestPrimaryServerPorts:

                    if (!ClientServerPorts.Contains(recevieData.Sender))
                    {
                        ClientServerPorts.Add(recevieData.Sender);
                    }

                    ConnectionModel requestModel = new ConnectionModel()
                    {
                        Action = ConnectionActions.RequestPrimaryServerPorts,
                        Sender = CurrentPort.Value,
                        Message = ConvertToJson(ClientServerPorts)
                    };

                    json = ConvertToJson(requestModel);
                    SendResponse(context, json);
                    break;
                case ConnectionActions.RequestNewPortConnected:

                    HandleRequestNewPortConnected(context, recevieData);                  

                    break;
                default:  
                    break;
            }
        }



        /// <summary>
        /// Require primary server to provide all ports
        /// </summary>
        private bool RequestServerAlive(int primaryPort)
        {
            try
            {
                // Request server alive -> Step 1
                var httpWebRequest = RequestServerResponse(primaryPort, ConnectionActions.RequestPrimaryServerAlive, string.Empty);

                // Receive Request server alive -> Step 3
                var recevieData = ReceiveServerResponse(httpWebRequest);
                if (recevieData.Action == ConnectionActions.RequestPrimaryServerAlive)
                { 
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
