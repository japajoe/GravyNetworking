namespace GravyNetworking.Client
{
    public static class Network
    {
        private static NetworkClient client;

        public static void SetNetworkClient(NetworkClient networkClient)
        {
            client = networkClient;
        }

        public static void Send(byte[] data, int length, byte channelId, ENet.PacketFlags flags)
        {
            client?.Send(data, length, channelId, flags);
        }

        public static void Send(IPacket packet, byte channelId, ENet.PacketFlags flags)
        {
            client?.Send(packet, channelId, flags);
        }
    }
}