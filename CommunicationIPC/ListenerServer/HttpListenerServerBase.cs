﻿using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using CommunicationIPC.Models;
using System;
using System.Diagnostics;

namespace CommunicationIPC.ListenerServer
{
    internal abstract class HttpListenerServerBase
    {
        /// <summary>
        /// Received the message from others AP
        /// </summary>
        internal event Action<string> ReceivedNotification;

        /// <summary>
        /// InitServer server and get server state
        /// </summary>
        /// <returns>Server state</returns>
        internal abstract ConnectionState InitServer();

        /// <summary>
        /// Handle the receive data
        /// </summary>
        /// <param name="context">HttpListenerContext</param>
        /// <param name="recevieData">Receive data</param>
        protected abstract void HandleReceiveData(HttpListenerContext context, ConnectionModel recevieData);

        /// <summary>
        /// Current Server of requestPort.
        /// </summary>
        protected int? CurrentPort { get; set; }

        /// <summary>
        /// The other server of ports.
        /// </summary>
        protected List<int> ClientServerPorts { get; set; }

        /// <summary>
        /// Check primaryPort
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        protected bool IsPortUsing(int port)
        {
            IPEndPoint[] tcpListenersArray = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();

            return tcpListenersArray.Any(x => x.Port == port);
        }

        /// <summary>
        /// Convet the generic type to json string
        /// </summary>
        /// <typeparam name="T">Generic Type</typeparam>
        /// <param name="message">Generic Type of message</param>
        /// <returns>Json string</returns>
        protected static string ConvertToJson<T>(T message)
        {
            var parameterString = JsonSerializer.Serialize(message);
            return parameterString;
        }

        /// <summary>
        /// Send the response for htttplistener
        /// </summary>
        /// <param name="context">HttpListenerContext</param>
        /// <param name="json">json string</param>
        protected void SendResponse(HttpListenerContext context, string json)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(json);
            HttpListenerResponse response = context.Response;
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Request the server to response data
        /// </summary>
        /// <param name="requestPort">Server requestPort</param>
        /// <param name="action">ConnectionActions</param>
        /// <param name="jsonMessage">Message json string</param>
        protected HttpWebRequest RequestServerResponse(int requestPort, ConnectionActions action, string jsonMessage)
        {
            // Request all ports form primary server -> Step 1
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(string.Format("http://127.0.0.1:{0}/", requestPort));
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.Timeout = 2000;

            ConnectionModel requestModel = new ConnectionModel()
            {
                Action = action,
                Sender = CurrentPort == null ? requestPort : CurrentPort.Value,
                Message = jsonMessage,
            };

            var requestJson = ConvertToJson(requestModel);

            using StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream());
            streamWriter.Write(requestJson);

            return httpWebRequest;
        }

        /// <summary>
        /// Receive server request of response
        /// </summary>
        /// <param name="httpWebRequest">HttpWebRequest</param>
        /// <returns>ConnectionModel</returns>
        protected ConnectionModel ReceiveServerResponse(HttpWebRequest httpWebRequest)
        {
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using var streamReader = new StreamReader(httpResponse.GetResponseStream());
            return JsonSerializer.Deserialize<ConnectionModel>(streamReader.ReadToEnd());
        }

        /// <summary>
        /// Handle request new requestPort connected, and trigger the ReceivedNotification event
        /// </summary>
        /// <param name="context"></param>
        /// <param name="recevieData"></param>
        protected void HandleRequestNewPortConnected(HttpListenerContext context, ConnectionModel recevieData)
        {
            ReceivedNotification?.Invoke(string.Format("Connected Ports = {0}, Sender = {1}", recevieData.Message, recevieData.Sender));
            var requestModel = new ConnectionModel()
            {
                Action = ConnectionActions.RequestNewPortConnected,
                Sender = CurrentPort.Value,
                Message = ConvertToJson(ClientServerPorts)
            };

            var json = ConvertToJson(requestModel);
            SendResponse(context, json);
        }


        /// <summary>
        /// Create server
        /// </summary>
        internal void CreateServer(int port)
        {
            CurrentPort = port;
            ClientServerPorts = new List<int>() { port };

            Task.Run(() =>
            {
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add(string.Format("http://127.0.0.1:{0}/", CurrentPort));
                listener.Start();             
                while (listener.IsListening)
                {
                    try
                    {
                        HttpListenerContext context = listener.GetContext();
                        var request = context.Request;
                        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                        var recevieData = JsonSerializer.Deserialize<ConnectionModel>(reader.ReadToEnd());
                        HandleReceiveData(context, recevieData);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }
            });
        }

        /// <summary>
        /// Get current port
        /// </summary>
        /// <returns></returns>
        internal int GetCurrentPort() 
        {
            return CurrentPort.Value;
        }

        internal List<int> GetAllPorts() 
        {
            return ClientServerPorts;
        }
    }
}