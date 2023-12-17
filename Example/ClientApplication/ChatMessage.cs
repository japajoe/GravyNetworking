using GravyNetworking.Client;
using GravyNetworking.Client.Utilities;

namespace ClientApplication
{
    public sealed class ChatMessage : IPacket
    {
        public int SenderId { get; set; }
        public string Text { get; set; }

        public ChatMessage(int senderId, string text)
        {
            this.SenderId = senderId;
            this.Text = text;
        }

        public ChatMessage(BinaryStream stream)
        {
            Deserialize(stream);
        }

        public void Deserialize(BinaryStream stream)
        {
            //We don't read the packet type, as the PacketHandler already read it from the stream
            this.SenderId = stream.ReadInt32();
            int textLengthInBytes = stream.ReadInt32();
            this.Text = stream.ReadString(textLengthInBytes);
        }

        public int Serialize(BinaryStream stream)
        {
            stream.Write((byte)PacketType.Chat);
            stream.Write(SenderId);
            stream.Write(BinaryConverter.GetByteCount(Text, TextEncoding.UTF8));
            stream.Write(Text);
            return stream.Length;
        }
    }
}