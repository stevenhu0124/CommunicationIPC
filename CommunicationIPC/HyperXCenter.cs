using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommunicationIPC.Models;

namespace CommunicationIPC
{
    /// <summary>
    /// Simulation of multiple APs using the same DLL
    /// </summary>
    public class HyperXCenter
    {
        /// <summary>
        /// Received the message from others AP
        /// </summary>
        public event Action<string> ReceivedNotification;

        private readonly Tuple<int, int> PRIMARY_PORTS_RANGE = Tuple.Create(9990, 9999);
        private readonly Tuple<int, int> SECONDARY_PORTS_RANGE = Tuple.Create(8880, 8889);

        private List<int> allServerPorts = new List<int>();
        private int currentPort = -1;
        private readonly object locker = new object();

        public List<int> InitDevices()
        {
            for (int primaryPort = PRIMARY_PORTS_RANGE.Item1; primaryPort < PRIMARY_PORTS_RANGE.Item2; primaryPort++)
            {
                lock (locker)
                {
                    if (IsPortUsing(primaryPort))
                    {
                        int secondaryPort = CreateSecondaryPort();
                        if (secondaryPort != -1)
                        {
                            var allPorts = RequestPortsFromPrimary(primaryPort, secondaryPort);
                            if (allPorts != null)
                            {
                                allServerPorts = allPorts;
                                CreateServer(secondaryPort);
                                NotifyNewPortConnected(allServerPorts, secondaryPort);
                                return allServerPorts;
                            }
                        }
                        else
                        {
                            throw new Exception("The DLL on this computer has reached its maximum usage limit");
                        }
                    }
                    else
                    {
                        CreateServer(primaryPort);
                        return allServerPorts;
                    }
                }
            }

            throw new Exception("The DLL on this computer has reached its maximum usage limit");
        }

        public int GetCurrentPort()
        {
            return currentPort;
        }

        private void NotifyNewPortConnected(List<int> allServerPorts, int currentPort)
        {
            foreach (var port in allServerPorts.Where(x => x != currentPort))
            {
                try
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(string.Format("http://127.0.0.1:{0}/", port));
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    httpWebRequest.Timeout = 2000;

                    ConnectionModel requestModel = new ConnectionModel()
                    {
                        Action = ConnectionActions.RequestNewPortConnected,
                        Sender = currentPort,
                        Message = ConvertToJson(allServerPorts)
                    };

                    var json = ConvertToJson(requestModel);
                    using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(json);
                    }

                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using var streamReader = new StreamReader(httpResponse.GetResponseStream());
                    //var recevieData = JsonSerializer.Deserialize<ConnectionModel>(streamReader.ReadToEnd());

                    //if (recevieData.Action == ConnectionActions.RequestNewPortConnected)
                    //{
                    //    allServerPorts = JsonSerializer.Deserialize<List<int>>(recevieData.Message);
                    //}
                }
                catch
                {
                    allServerPorts.Remove(port);
                }
            }
        }

        private int CreateSecondaryPort()
        {
            for (int i = SECONDARY_PORTS_RANGE.Item1; i < SECONDARY_PORTS_RANGE.Item2; i++)
            {
                if (!IsPortUsing(i))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Require primary server to provide all ports
        /// </summary>
        private List<int> RequestPortsFromPrimary(int primaryPort, int currentPort)
        {
            try
            {
                // Request all ports form primary server -> Step 1
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(string.Format("http://127.0.0.1:{0}/", primaryPort));
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.Timeout = 2000;

                ConnectionModel requestModel = new ConnectionModel()
                {
                    Action = ConnectionActions.RequestPrimaryServerPorts,
                    Sender = currentPort,
                };

                var json = ConvertToJson(requestModel);

                using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(json);
                }

                // Recevie all port form primary server -> Step 4
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using var streamReader = new StreamReader(httpResponse.GetResponseStream());
                var recevieData = JsonSerializer.Deserialize<ConnectionModel>(streamReader.ReadToEnd());

                if (recevieData.Action == ConnectionActions.RequestPrimaryServerPorts)
                {
                    var connectedInfo = JsonSerializer.Deserialize<List<int>>(recevieData.Message);
                    return connectedInfo;
                }
                else
                {
                    throw new Exception("invalid response");
                }
            }
            catch
            {
                return null;
            }
        }

        private void CreateServer(int port)
        {
            currentPort = port;
            Task.Run(() =>
            {
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add(string.Format("http://127.0.0.1:{0}/", port));
                listener.Start();
                while (listener.IsListening)
                {
                    try
                    {
                        HttpListenerContext context = listener.GetContext();
                        var request = context.Request;
                        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                        var recevieData = JsonSerializer.Deserialize<ConnectionModel>(reader.ReadToEnd());
                        switch (recevieData.Action)
                        {
                            case ConnectionActions.RequestPrimaryServerPorts:

                                // Recevie the RequestPrimaryServerPorts from other server -> Step 2
                                if (!allServerPorts.Contains(currentPort))
                                {
                                    allServerPorts.Add(currentPort);
                                }

                                if (!allServerPorts.Contains(recevieData.Sender))
                                {
                                    allServerPorts.Add(recevieData.Sender);
                                }

                                // Send the RequestPrimaryServerPorts reposne to other server -> Step 3
                                ConnectionModel requestModel = new ConnectionModel()
                                {
                                    Action = ConnectionActions.RequestPrimaryServerPorts,
                                    Sender = currentPort,
                                    Message = ConvertToJson(allServerPorts)
                                };
                                var json = ConvertToJson(requestModel);
                                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(json);
                                HttpListenerResponse response = context.Response;
                                response.ContentLength64 = buffer.Length;
                                Stream output = response.OutputStream;
                                output.Write(buffer, 0, buffer.Length);
                                break;
                            case ConnectionActions.RequestNewPortConnected:

                                ReceivedNotification?.Invoke(string.Format("Connected Ports = {0}, Sender = {1}", recevieData.Message, recevieData.Sender));

                                requestModel = new ConnectionModel()
                                {
                                    Action = ConnectionActions.RequestNewPortConnected,
                                    Sender = currentPort,
                                    Message = ConvertToJson(allServerPorts)
                                };
                                json = ConvertToJson(requestModel);
                                buffer = System.Text.Encoding.UTF8.GetBytes(json);
                                response = context.Response;
                                response.ContentLength64 = buffer.Length;
                                output = response.OutputStream;
                                output.Write(buffer, 0, buffer.Length);

                                break;
                            default:
                                break;
                        }
                    }
                    catch
                    {
                    }
                }
            });
        }

        /// <summary>
        /// Check primaryPort
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        private bool IsPortUsing(int port)
        {
            IPEndPoint[] tcpListenersArray = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();

            return tcpListenersArray.Any(x => x.Port == port);
        }

        private string ConvertToJson<T>(T message)
        {
            var parameterString = JsonSerializer.Serialize(message);

            return parameterString;
        }
    }
}
