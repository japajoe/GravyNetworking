namespace GravyNetworking.Client
{
    public struct NetworkConfig
    {
        public ushort port;
        public string hostAddress;
        public uint maxChannels;
        public uint maxPacketSize;
        public uint bufferSize;
        public uint incomingQueueCapacity;
        public uint outgoingQueueCapacity;

        /// <summary>
        /// Initializes a NetworkConfig structure
        /// </summary>
        /// <param name="port">The port that the server listens on</param>
        /// <param name="maxClients">The maximum number of clients the server handles</param>
        /// <param name="maxChannels">The maximum number of network channels</param>
        /// <param name="bufferSize">The size of the incoming and outgoing buffer in number of bytes</param>
        /// <param name="maxPacketSize">The maximum size of a packet in number of bytes</param>
        /// <param name="incomingQueueCapacity">The capacity of the incoming packet queue in number of items</param>
        /// <param name="outgoingQueueCapacity">The capacity of the outgoing packet queue in number of items</param>
        /// <param name="bindAddress">The address the server should bind to</param>
        /// <param name="bindAllInterfaces">If true the bind address isn't used but instead the server binds any address</param>
        public NetworkConfig(ushort port, string hostAddress, uint maxChannels, uint bufferSize, uint maxPacketSize, uint incomingQueueCapacity, uint outgoingQueueCapacity)
        {
            this.port = port;
            this.hostAddress = hostAddress;
            this.maxChannels = maxChannels;
            this.bufferSize = bufferSize;
            this.maxPacketSize = maxPacketSize;
            this.incomingQueueCapacity = incomingQueueCapacity;
            this.outgoingQueueCapacity = outgoingQueueCapacity;
        }

        /// <summary>
        /// Creates a NetworkConfig structure with some default values
        /// </summary>
        /// <param name="port">The port that the server listens on</param>
        /// <param name="maxClients">The maximum number of clients the server handles</param>
        /// <param name="maxChannels">The maximum number of network channels</param>
        /// <param name="bufferSize">The size of the incoming and outgoing buffer in number of bytes</param>
        /// <param name="maxPacketSize">The maximum size of a packet in number of bytes</param>
        /// <param name="incomingQueueCapacity">The capacity of the incoming packet queue in number of items</param>
        /// <param name="outgoingQueueCapacity">The capacity of the outgoing packet queue in number of items</param>
        /// <param name="bindAddress">The address the server should bind to</param>
        /// <param name="bindAllInterfaces">If true the bind address isn't used but instead the server binds any address</param>
        public static NetworkConfig CreateDefault(ushort port, string hostAddress, uint maxChannels)
        {
            NetworkConfig config = default;
            config.port = port;
            config.hostAddress = hostAddress;
            config.maxChannels = maxChannels;
            config.bufferSize = 4096;
            config.maxPacketSize = 1024;
            config.incomingQueueCapacity = 4096;
            config.outgoingQueueCapacity = 4096;
            return config;
        }
    }
}