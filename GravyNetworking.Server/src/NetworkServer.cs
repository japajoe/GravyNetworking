using System;
using System.Threading;
using GravyNetworking.Server.Utilities;
using ENet;

namespace GravyNetworking.Server
{
    public delegate void ConnectedEvent(int clientId, string IP);
    public delegate void DisconnectedEvent(int clientId);
    public delegate void PacketReceivedEvent(int clientId, byte[] data, int length, byte channel);

    public sealed class NetworkServer
    {
        private struct ConnectionInfo
        {
            public int peerId;
            public string IP;

            public ConnectionInfo(int peerId, string IP)
            {
                this.peerId = peerId;
                this.IP = IP;
            }
        }

        private struct PacketInfo
        {
            public enum SendMode
            {
                None,
                Broadcast,
                BroadcastSelective,
                SendToPeer
            }

            public int peerId;
            public ENet.Packet packet;
            public PacketInfo.SendMode sendMode;
            public int[] clients;
            public byte channel;

            public PacketInfo(int peerId, ENet.Packet packet, PacketInfo.SendMode sendMode, byte channel, int[] clients = null)
            {
                this.peerId = peerId;
                this.packet = packet;
                this.sendMode = sendMode;
                this.channel = 0;
                this.clients = clients;
            }
        }

        private ushort port;
        private string bindAddress = "0.0.0.0";
        private ushort maxClients;
        private uint maxPacketSize;
        private uint maxChannels;
        private bool isFruityDevice = false; //If running on Mac OS probably set to true
        private bool bindAllInterfaces = false;
        private byte[] incomingBuffer;
        private byte[] outgoingBuffer;
        private Thread networkThread;
        private volatile bool isRunning;
        private ENet.Peer[] peers;
        private ChecksumCallback checksumCallback;
        private RingBuffer<ConnectionInfo> connectionQueue;
        private RingBuffer<ConnectionInfo> disconnectionQueue;
        private RingBuffer<PacketInfo> incomingPacketQueue;
        private RingBuffer<PacketInfo> outgoingPacketQueue;

        public event ConnectedEvent ClientConnected;
        public event DisconnectedEvent ClientDisconnected;
        public event PacketReceivedEvent PacketReceived;

        public bool IsRunning
        {
            get => isRunning;
        }

        public NetworkServer(ushort port, ushort maxClients, uint maxChannels, uint maxPacketSize = 1024)
        {
            this.port = port;
            this.maxClients = maxClients;
            this.maxChannels = maxChannels;
            this.maxPacketSize = maxPacketSize;

            if(this.maxChannels < 1)
                this.maxChannels = 1;

            peers = new ENet.Peer[maxClients];
            incomingBuffer = new byte[4096];
            outgoingBuffer = new byte[4096];

            connectionQueue = new RingBuffer<ConnectionInfo>(1024);
            disconnectionQueue = new RingBuffer<ConnectionInfo>(1024);
            incomingPacketQueue = new RingBuffer<PacketInfo>(4096);
            outgoingPacketQueue = new RingBuffer<PacketInfo>(4096);
            
            checksumCallback = new ChecksumCallback(ENet.Library.CRC64);

            Network.SetNetworkServer(this);
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
            
            NetworkLog.WriteLine("Attempting to stop server.");

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
                    if(connectionQueue.TryDequeue(out ConnectionInfo connectionInfo))
                    {
                        ClientConnected?.Invoke(connectionInfo.peerId, connectionInfo.IP);
                    }
                }
            }

            if(disconnectionQueue.Count > 0)
            {
                while(disconnectionQueue.Count > 0)
                {
                    if(disconnectionQueue.TryDequeue(out ConnectionInfo connectionInfo))
                    {
                        ClientDisconnected?.Invoke(connectionInfo.peerId);
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
                            PacketReceived?.Invoke(packetInfo.peerId, incomingBuffer, packetInfo.packet.Length, packetInfo.channel);
                            packetInfo.packet.Dispose();
                        }
                    }
                }
            }
        }

        public void Send(int peerId, byte[] data, int length, byte channelId, ENet.PacketFlags flags)
        {
            ENet.Packet packet = default;
            packet.Create(data, 0, length, flags);
            PacketInfo info = new PacketInfo(peerId, packet, PacketInfo.SendMode.SendToPeer, channelId);
            outgoingPacketQueue.Enqueue(info);
        }

        public void Send(int peerId, IPacket packet, byte channelId, ENet.PacketFlags flags)
        {
            Array.Fill<byte>(outgoingBuffer, 0);
            BinaryStream stream = new BinaryStream(outgoingBuffer);
            int length = packet.Serialize(stream);
            Send(peerId, outgoingBuffer, length, channelId, flags);
        }

        public void Broadcast(byte[] data, int length, byte channelId, ENet.PacketFlags flags)
        {
            ENet.Packet packet = default;
            packet.Create(data, 0, length, flags);
            PacketInfo info = new PacketInfo(0, packet, PacketInfo.SendMode.Broadcast, channelId);
            outgoingPacketQueue.Enqueue(info);
        }

        public void Broadcast(IPacket packet, byte channelId, ENet.PacketFlags flags)
        {
            Array.Fill<byte>(outgoingBuffer, 0);
            BinaryStream stream = new BinaryStream(outgoingBuffer);
            int length = packet.Serialize(stream);
            Broadcast(outgoingBuffer, length, channelId, flags);
        }
        
        public void BroadcastSelective(int[] clientIds, byte[] data, int length, byte channelId, ENet.PacketFlags flags)
        {
            ENet.Packet packet = default;
            packet.Create(data, 0, length, flags);
            PacketInfo info = new PacketInfo(0, packet, PacketInfo.SendMode.BroadcastSelective, channelId, clientIds);
            outgoingPacketQueue.Enqueue(info);
        }

        public void BroadcastSelective(int[] clientIds, IPacket packet, byte channelId, ENet.PacketFlags flags)
        {
            Array.Fill<byte>(outgoingBuffer, 0);
            BinaryStream stream = new BinaryStream(outgoingBuffer);
            int length = packet.Serialize(stream);
            BroadcastSelective(clientIds, outgoingBuffer, length, channelId, flags);
        }

        private void NetworkThread()
        {
            NetworkLog.WriteLine("Attemping to start server.");

            if(!ENet.Library.Initialize())
            {
                isRunning = false;
                NetworkLog.WriteLine("Could not initialize ENet.");
                return;
            }

            using (Host server = new Host())
            {
                Address address = new Address();

                if (isFruityDevice)
                {
                    if (!bindAllInterfaces)
                        address.SetHost(bindAddress);
                }
                else
                {
                    if (bindAllInterfaces)
                        address.SetIP("::0"); // Binds any address
                    else
                        address.SetHost(bindAddress);
                }

                address.Port = port;
                
                try
                {
                    server.Create(address, maxClients, (int)maxChannels);
                }
                catch(Exception ex)
                {
                    NetworkLog.WriteLine("Could not create server instance: " + ex.Message);
                    ENet.Library.Deinitialize();
                    isRunning = false;
                    return;
                }

                server.SetChecksumCallback(checksumCallback);

                NetworkLog.WriteLine("Server start listening on port: " + port + ". Max clients: " + maxClients + ".");

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
                                    switch(packetInfo.sendMode)
                                    {
                                        case PacketInfo.SendMode.Broadcast:
                                            server.Broadcast(packetInfo.channel, ref packetInfo.packet);
                                            break;
                                        case PacketInfo.SendMode.BroadcastSelective:
                                            if(packetInfo.clients != null)
                                            {
                                                Peer[] targetPeers = new Peer[packetInfo.clients.Length];
                                                int numPeers = 0;

                                                for(int i = 0; i < packetInfo.clients.Length; i++)
                                                {
                                                    if(packetInfo.clients[i] < peers.Length)
                                                    {
                                                        targetPeers[numPeers] = peers[packetInfo.clients[i]];
                                                        numPeers++;
                                                    }
                                                }

                                                if(numPeers > 0)
                                                {
                                                    server.Broadcast(packetInfo.channel, ref packetInfo.packet, targetPeers);
                                                }
                                                else
                                                {
                                                    packetInfo.packet.Dispose();
                                                }

                                            }
                                            break;
                                        case PacketInfo.SendMode.SendToPeer:
                                            peers[packetInfo.peerId].Send(packetInfo.channel, ref packetInfo.packet);
                                            break;
                                        default:
                                            break;
                                    }
                                }

                            }
                        }
                    }

                    bool polled = false;

                    while (!polled)
                    {
                        if (server.CheckEvents(out netEvent) <= 0)
                        {
                            if (server.Service(15, out netEvent) <= 0)
                                break;

                            polled = true;
                        }

                        switch (netEvent.Type)
                        {
                            case EventType.None:
                                break;
                            case EventType.Connect:
                                peers[netEvent.Peer.ID] = netEvent.Peer;
                                connectionQueue.Enqueue(new ConnectionInfo((int)netEvent.Peer.ID, netEvent.Peer.IP));
                                break;
                            case EventType.Disconnect:
                            case EventType.Timeout:
                                peers[netEvent.Peer.ID] = default;
                                disconnectionQueue.Enqueue(new ConnectionInfo((int)netEvent.Peer.ID, netEvent.Peer.IP));
                                break;
                            case EventType.Receive:
                                incomingPacketQueue.Enqueue(new PacketInfo((int)netEvent.Peer.ID, netEvent.Packet, PacketInfo.SendMode.None, netEvent.ChannelID));
                                break;
                        }
                    }
                }

                server.Flush();
                
                //Disconnect everyone explicitly because we are nice people
                for(int i = 0; i < peers.Length; i++)
                {
                    if(peers[i].IsSet)
                    {
                        peers[i].DisconnectNow(0);
                    }
                }

            }

            ENet.Library.Deinitialize();

            NetworkLog.WriteLine("Server stopped.");
        }
    }
}