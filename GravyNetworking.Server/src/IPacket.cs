using GravyNetworking.Server.Utilities;

namespace GravyNetworking.Server
{
    public interface IPacket
    {
        int Serialize(BinaryStream stream);
        void Deserialize(BinaryStream stream);
    }
}