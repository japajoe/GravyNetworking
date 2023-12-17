using System;
using System.Threading;
using GravyNetworking.Server;

namespace ServerApplication
{
    class Program
    {
        private static NetworkServer server;
        private static PacketHandler packetHandler;

        static void Main(string[] args)
        {
            Console.CancelKeyPress += OnCancelKeyPress;

            NetworkConfig config = new NetworkConfig(7777, 100, 2, 4096, 1024, 4096, 4096, "0.0.0.0", false);
            
            server = new NetworkServer(config);
            server.ClientConnected += OnClientConnected;
            server.ClientDisconnected += OnClientDisconnected;
            server.PacketReceived += OnPacketReceived;

            packetHandler = new PacketHandler();

            server.Start();

            while(true)
            {
                //Important to update the server because messages from the network thread need to make it to the main thread
                server.Update();
                Thread.Sleep(10);
            }
        }

        private static void OnClientConnected(int clientId, string IP)
        {
            NetworkLog.WriteLine("A client as connected with ID " + clientId);
        }

        private static void OnClientDisconnected(int clientId)
        {
            NetworkLog.WriteLine("A client has disconnected with ID " + clientId);
        }

        private static void OnPacketReceived(int clientId, byte[] data, int length, byte channel)
        {
            NetworkLog.WriteLine("Received a packet from ID " + clientId + " with " + length + " bytes of data");
            packetHandler.HandlePacket(clientId, data, length, channel);
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            server?.Stop();
        }
    }
}
