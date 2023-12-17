using GravyNetworking.Client.Utilities;

namespace GravyNetworking.Client
{
    public interface IPacket
    {
        int Serialize(BinaryStream stream);
        void Deserialize(BinaryStream stream);
    }
}