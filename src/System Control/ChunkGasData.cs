using ProtoBuf;

namespace GasApi
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ChunkGasData
    {
        public byte[] Data;
        public int chunkX, chunkY, chunkZ;
    }
}
