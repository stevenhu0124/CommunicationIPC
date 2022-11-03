using CommunicationIPC;
using System;

namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=========================== GetServerConnectionState Devices ===========================");
                HyperXCenter center = new HyperXCenter();
                center.ReceivedNotification += Center_ReceivedNotification;
                center.InitServer();

                var currentPort = center.GetCurrentPort();
                Console.WriteLine(string.Format("Current server of port: {0}", currentPort));

                var allPorts = center.GetAllPorts();
                Console.WriteLine(string.Format("AllPorts: {0}", string.Join(",", allPorts)));

                Console.ReadLine();

            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }

        private static void Center_ReceivedNotification(string msg)
        {
            Console.WriteLine("=========================== Receviced Connected event ===========================");
            Console.WriteLine(string.Format("{0}-{1}", DateTime.Now.ToString("yyyy/MM/dd/ HH:mm:ss"), msg));
        }

    }
}
