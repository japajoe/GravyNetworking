
using System;
using System.Threading;
using GravyNetworking.Client.Utilities;
using ENet;

namespace GravyNetworking.Client
{
    public delegate void ConnectedEvent();
    public delegate void DisconnectedEvent();
    public delegate void PacketReceivedEvent(byte[] data, int length, byte channel);

    public sealed class NetworkClient
    {
        private struct ConnectionInfo
        {
        }

        private struct PacketInfo
        {
            public ENet.Packet packet;
            public byte channel;

            public PacketInfo(ENet.Packet packet, byte channel)
            {
                this.packet = packet;
                this.channel = 0;
            }
        }

        private string IP;
        private ushort port;
        private uint maxChannels;
        private uint maxPacketSize;
        private volatile bool isRunning;
        private byte[] incomingBuffer;
        private byte[] outgoingBuffer;
        private Thread networkThread;
        private ChecksumCallback checksumCallback;
        private RingBuffer<ConnectionInfo> connectionQueue;
        private RingBuffer<ConnectionInfo> disconnectionQueue;
        private RingBuffer<PacketInfo> incomingPacketQueue;
        private RingBuffer<PacketInfo> outgoingPacketQueue;

        public event ConnectedEvent Connected;
        public event DisconnectedEvent Disconnected;
        public event PacketReceivedEvent PacketReceived;

        public bool IsRunning
        {
            get => isRunning;
        }

        public NetworkClient(string IP, ushort port, uint maxChannels, uint maxPacketSize = 1024)
        {
            this.IP = IP;
            this.port = port;
            this.maxChannels = maxChannels;
            this.maxPacketSize = maxPacketSize;

            if(this.maxChannels < 1)
                this.maxChannels = 1;

            incomingBuffer = new byte[4096];
            outgoingBuffer = new byte[4096];

            connectionQueue = new RingBuffer<ConnectionInfo>(32);
            disconnectionQueue = new RingBuffer<ConnectionInfo>(32);
            incomingPacketQueue = new RingBuffer<PacketInfo>(4096);
            outgoingPacketQueue = new RingBuffer<PacketInfo>(4096);

            checksumCallback = new ChecksumCallback(ENet.Library.CRC64);

            Network.SetNetworkClient(this);
        }

        public void Start()
        {
            if(isRunning)
                return;

            //Drain queues in case any leftover data is still present from a previous thread
            connectionQueue.Drain();
            disconnectionQueue.Drain();
            incomingPacketQueue.Drain();
            outgoingPacketQueue.Drain();
            
            isRunning = true;

            networkThread = new Thread(NetworkThread);
            networkThread.Start();
        }

        public void Stop()
        {
            if(!isRunning)
                return;
            
            NetworkLog.WriteLine("Attempting to stop client.");

            isRunning = false;

            if(networkThread != null)
            {
                if(networkThread.IsAlive)
                    networkThread.Join();
            }
        }

        public void Update()
        {
            if(connectionQueue.Count > 0)
            {
                while(connectionQueue.Count > 0)
                {
                    if(connectionQueue.TryDequeue(out _))
                    {
                        Connected?.Invoke();
                    }
                }
            }

            if(disconnectionQueue.Count > 0)
            {
                while(disconnectionQueue.Count > 0)
                {
                    if(disconnectionQueue.TryDequeue(out _))
                    {
                        Disconnected?.Invoke();
                    }
                }
            }

            if(incomingPacketQueue.Count > 0)
            {
                while(incomingPacketQueue.Count > 0)
                {
                    if(incomingPacketQueue.TryDequeue(out PacketInfo packetInfo))
                    {
                        //If packet exceeds maximum size, just deallocate it
                        if(packetInfo.packet.Length > maxPacketSize)
                        {
                            packetInfo.packet.Dispose();
                        }
                        else
                        {
                            Array.Fill<byte>(incomingBuffer, 0);
                            packetInfo.packet.CopyTo(incomingBuffer);
                            PacketReceived?.Invoke(incomingBuffer, packetInfo.packet.Length, packetInfo.channel);
                            packetInfo.packet.Dispose();
                        }
                    }
                }
            }
        }

        public void Send(byte[] data, int length, byte channelId, ENet.PacketFlags flags)
        {
            ENet.Packet packet = default;
            packet.Create(data, 0, length, flags);
            PacketInfo info = new PacketInfo(packet, channelId);
            outgoingPacketQueue.Enqueue(info);
        }

        public void Send(IPacket packet, byte channelId, ENet.PacketFlags flags)
        {
            Array.Fill<byte>(outgoingBuffer, 0);
            BinaryStream stream = new BinaryStream(outgoingBuffer);
            int length = packet.Serialize(stream);
            ENet.Packet enetPacket = default;
            enetPacket.Create(outgoingBuffer, 0, length, flags);
            PacketInfo info = new PacketInfo(enetPacket, channelId);
            outgoingPacketQueue.Enqueue(info);            
        }

        private void NetworkThread()
        {
            NetworkLog.WriteLine("Attemping to start client.");

            if(!ENet.Library.Initialize())
            {
                isRunning = false;
                NetworkLog.WriteLine("Could not initialize ENet.");
                return;
            }

            using (Host client = new Host())
            {
                Address address = new Address();

                address.SetHost(IP);
                address.Port = port;

                try
                {
                    client.Create();
                }
                catch(Exception ex)
                {
                    NetworkLog.WriteLine("Could not create client instance: " + ex.Message);
                    Library.Deinitialize();
                    isRunning = false;
                    return;                    
                }

                client.SetChecksumCallback(checksumCallback);

                Peer peer = client.Connect(address, (int)maxChannels);

                NetworkLog.WriteLine("Client started connecting to: " + IP + ":" + port + ".");

                Event netEvent;

                while (isRunning)
                {
                    if(outgoingPacketQueue.Count > 0)
                    {
                        while(outgoingPacketQueue.Count > 0)
                        {
                            if(outgoingPacketQueue.TryDequeue(out PacketInfo packetInfo))
                            {
                                //If packet size exceeds maximum packet size, don't send but just deallocate
                                if(packetInfo.packet.Length > maxPacketSize)
                                {
                                    packetInfo.packet.Dispose();
                                }
                                else
                                {
                                    peer.Send(packetInfo.channel, ref packetInfo.packet);
                                }
                            }
                        }
                    }

                    bool polled = false;

                    while (!polled)
                    {
                        if (client.CheckEvents(out netEvent) <= 0)
                        {
                            if (client.Service(15, out netEvent) <= 0)
                                break;

                            polled = true;
                        }

                        switch (netEvent.Type)
                        {
                            case EventType.None:
                                break;
                            case EventType.Connect:
                                connectionQueue.Enqueue(new ConnectionInfo());
                                break;
                            case EventType.Disconnect:
                            case EventType.Timeout:
                                disconnectionQueue.Enqueue(new ConnectionInfo());
                                break;
                            case EventType.Receive:
                                incomingPacketQueue.Enqueue(new PacketInfo(netEvent.Packet, netEvent.ChannelID));
                                break;
                        }
                    }
                }

                client.Flush();

                //Disconnect gracefully
                peer.DisconnectNow(0);
            }

            ENet.Library.Deinitialize();

            NetworkLog.WriteLine("Client stopped.");
        }
    }
}