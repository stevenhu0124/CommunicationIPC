using CommunicationIPC;
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //HttpListener listener = new HttpListener();
            //listener.Prefixes.Add(string.Format("http://127.0.0.1:{0}/", 9990));
            //listener.Start();


            HyperXCenter center = new HyperXCenter();
            center.ReceivedNotification += Center_ReceivedNotification;
            var allPorts = center.InitDevices();
            Console.WriteLine("=========================== Init Devices ===========================");
            Console.WriteLine(string.Format("Current port: {0}", center.GetCurrentPort()));
            if (allPorts != null && allPorts.Count > 0)
            {
                Console.WriteLine(string.Format("AllPorts: {0}", string.Join(",", allPorts)));
            }
            Console.ReadLine();
        }

        private static void Center_ReceivedNotification(string msg)
        {
            Console.WriteLine("=========================== Receviced Connected event ===========================");
            Console.WriteLine(msg);
        }
    }
}
