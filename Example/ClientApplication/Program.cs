using System;
using System.Threading;
using GravyNetworking.Client;

namespace ClientApplication
{
    class Program
    {
        private static NetworkClient client;
        private static PacketHandler packetHandler;

        static void Main(string[] args)
        {
            Console.CancelKeyPress += OnCancelKeyPress;

            NetworkConfig config = new NetworkConfig(7777, "127.0.0.1", 2, 4096, 1024, 4096, 4096);
            
            client = new NetworkClient(config);
            client.Connected += OnConnected;
            client.Disconnected += OnDisconnected;
            client.PacketReceived += OnPacketReceived;

            packetHandler = new PacketHandler();

            client.Start();

            while(true)
            {
                //Important to update the client because messages from the network thread need to make it to the main thread
                client.Update();
                Thread.Sleep(10);
            }
        }

        private static void OnConnected()
        {
            NetworkLog.WriteLine("Connected to server");

            ChatMessage chat = new ChatMessage(0, "Hello server from client");
            Network.Send(chat, 0, ENet.PacketFlags.Reliable);
        }

        private static void OnDisconnected()
        {
            NetworkLog.WriteLine("Disconnected from server");
        }

        private static void OnPacketReceived(byte[] data, int length, byte channel)
        {
            NetworkLog.WriteLine("Received a packet with " + length + " bytes of data");
            packetHandler.HandlePacket(data, length, channel);
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            client?.Stop();
        }
    }
}
