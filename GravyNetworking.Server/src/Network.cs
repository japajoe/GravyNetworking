namespace GravyNetworking.Server
{
    public static class Network
    {
        private static NetworkServer server;

        public static void SetNetworkServer(NetworkServer networkServer)
        {
            server = networkServer;
        }

        public static void Send(int peerId, byte[] data, int length, byte channelId, ENet.PacketFlags flags)
        {
            server?.Send(peerId, data, length, channelId, flags);
        }

        public static void Send(int peerId, IPacket packet, byte channelId, ENet.PacketFlags flags)
        {
            server?.Send(peerId, packet, channelId, flags);
        }

        public static void Broadcast(byte[] data, int length, byte channelId, ENet.PacketFlags flags)
        {
            server?.Broadcast(data, length, channelId, flags);
        }

        public static void Broadcast(IPacket packet, byte channelId, ENet.PacketFlags flags)
        {
            server?.Broadcast(packet, channelId, flags);
        }

        public static void BroadcastSelective(int[] clientIds, byte[] data, int length, byte channelId, ENet.PacketFlags flags)
        {
            server?.BroadcastSelective(clientIds, data, length, channelId, flags);
        }

        public static void BroadcastSelective(int[] clientIds, IPacket packet, byte channelId, ENet.PacketFlags flags)
        {
            server?.BroadcastSelective(clientIds, packet, channelId, flags);
        }
    }
}