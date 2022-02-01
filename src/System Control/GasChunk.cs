using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace GasApi
{
    public class GasChunk
    {
        public IWorldChunk Chunk;
        public Dictionary<int, Dictionary<string, float>> Gases;

        public int X;
        public int Y;
        public int Z;

        bool shouldSave;

        public GasChunk(IWorldChunk newChunk, Dictionary<int, Dictionary<string, float>> newGases, int x, int y, int z)
        {
            Chunk = newChunk;
            Gases = newGases;
            X = x;
            Y = y;
            Z = z;
        }

        public bool Compare(BlockPos pos, int chunksize)
        {
            return pos.X / chunksize == X && pos.Y / chunksize == Y && pos.Z / chunksize == Z;
        }

        public void SaveChunk(IServerNetworkChannel serverChannel)
        {
            if (!shouldSave) return;
            
            byte[] data = SerializerUtil.Serialize(Gases);

            Chunk.SetModdata("gases", data);
            // Todo: Send only to players that have this chunk in their loaded range
            serverChannel.BroadcastPacket(new ChunkGasData() { chunkX = X, chunkY = Y, chunkZ = Z, Data = data });
        }

        public void TakeGas(ref Dictionary<string, float> taker, int point)
        {
            if (Gases == null || !Gases.ContainsKey(point)) return;
            
            Dictionary<string, float> takeFrom = Gases[point];
            if (takeFrom == null || takeFrom.Count < 1) return;

            foreach (var gas in takeFrom)
            {
                if (taker.ContainsKey(gas.Key))
                {
                    taker[gas.Key] += gas.Value;
                }
                else taker[gas.Key] = gas.Value;
            }

            Gases[point] = null;
            shouldSave = true;
            
        }

        public void SetGas(string gasName, float amount, int point)
        {
            if (Gases == null) return;
            
            Dictionary<string, float> gasAtPoint;
            Gases.TryGetValue(point, out gasAtPoint);
            if (gasAtPoint == null) gasAtPoint = new Dictionary<string, float>();

            if (!gasAtPoint.ContainsKey(gasName)) gasAtPoint.Add(gasName, GameMath.Clamp(amount, 0, 1)); else gasAtPoint[gasName] = GameMath.Clamp(gasAtPoint[gasName] + amount, 0, 1);
            if (Gases.ContainsKey(point)) Gases[point] = gasAtPoint; else Gases.Add(point, gasAtPoint);
            
            shouldSave = true;
            
        }
    }
}
