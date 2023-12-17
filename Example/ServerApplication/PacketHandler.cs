using GravyNetworking.Server;
using GravyNetworking.Server.Utilities;

namespace ServerApplication
{
    public sealed class PacketHandler
    {
        private delegate void PacketHandlerFunc(int clientId, BinaryStream stream);

        private PacketHandlerFunc[] packetHandlers;

        public PacketHandler()
        {
            packetHandlers = new PacketHandlerFunc[(int)PacketType.Count];
            packetHandlers[(int)PacketType.Chat] = OnChatMessage;
        }

        public void HandlePacket(int clientId, byte[] data, int length, byte channel)
        {
            BinaryStream stream = new BinaryStream(data, length);

            byte packetType = stream.ReadByte();

            if(packetType < packetHandlers.Length)
                packetHandlers[packetType](clientId, stream);
        }

        private void OnChatMessage(int clientId, BinaryStream stream)
        {
            ChatMessage chat = new ChatMessage(stream);
            NetworkLog.WriteLine(chat.Text);
            
            //Broadcast the chat message to everyone, including the sender
            chat.SenderId = clientId;
            Network.Broadcast(chat, 0, ENet.PacketFlags.Reliable);
        }
    }
}