using GravyNetworking.Client;
using GravyNetworking.Client.Utilities;

namespace ClientApplication
{
    public sealed class PacketHandler
    {
        private delegate void PacketHandlerFunc(BinaryStream stream);

        private PacketHandlerFunc[] packetHandlers;

        public PacketHandler()
        {
            packetHandlers = new PacketHandlerFunc[(int)PacketType.Count];
            packetHandlers[(int)PacketType.Chat] = OnChatMessage;
        }

        public void HandlePacket(byte[] data, int length, byte channel)
        {
            BinaryStream stream = new BinaryStream(data, length);

            byte packetType = stream.ReadByte();

            if(packetType < packetHandlers.Length)
                packetHandlers[packetType](stream);
        }

        private void OnChatMessage(BinaryStream stream)
        {
            ChatMessage chat = new ChatMessage(stream);
            NetworkLog.WriteLine(chat.Text);
        }
    }
}