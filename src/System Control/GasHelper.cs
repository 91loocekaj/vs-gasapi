using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Server;

namespace GasApi
{
    public class GasHelper : ModSystem
    {
        private ICoreAPI api;

        public override void Start(ICoreAPI papi)
        {
            base.Start(api);

            api = papi;

            try
            {
                IAsset asset = api.Assets.Get("gasapi:config/gases.json");
                LiteGasDict = asset.ToObject<Dictionary<string, GasInfoLite>>();
                if (LiteGasDict == null) LiteGasDict = new Dictionary<string, GasInfoLite>();
            }
            catch
            {
                LiteGasDict = new Dictionary<string, GasInfoLite>();
            }
        }

        static Dictionary<string, GasInfoLite> LiteGasDict;

        public Dictionary<int, Dictionary<string, float>> GetGasesForChunk(BlockPos pos)
        {
            byte[] data;

            IWorldChunk chunk = api.World.BlockAccessor.GetChunkAtBlockPos(pos);
            if (chunk == null) return null;

            data = chunk.GetModdata("gases");

            Dictionary<int, Dictionary<string, float>> gasesOfChunk = null;

            if (data != null)
            {
                try
                {
                    gasesOfChunk = SerializerUtil.Deserialize<Dictionary<int, Dictionary<string, float>>>(data);
                }
                catch (Exception)
                {
                    gasesOfChunk = null;
                }
            }

            return gasesOfChunk;
        }

        public Dictionary<int, Dictionary<string, float>> GetGasesForChunk(IWorldChunk chunk)
        {
            byte[] data;

            if (chunk == null) return null;

            data = chunk.GetModdata("gases");

            Dictionary<int, Dictionary<string, float>> gasesOfChunk = null;

            if (data != null)
            {
                try
                {
                    gasesOfChunk = SerializerUtil.Deserialize<Dictionary<int, Dictionary<string, float>>>(data);
                }
                catch (Exception)
                {
                    gasesOfChunk = null;
                }
            }

            return gasesOfChunk;
        }

        public Dictionary<string, float> GetGases(BlockPos pos)
        {
            Dictionary<int, Dictionary<string, float>> gasesOfChunk = GetGasesForChunk(pos);
            if (gasesOfChunk == null) return null;

            int index3d = toLocalIndex(pos);
            if (!gasesOfChunk.ContainsKey(index3d)) return null;

            return gasesOfChunk[index3d];
        }

        public float GetGas(BlockPos pos, string name)
        {
            Dictionary<string, float> gasesHere = GetGases(pos);

            if (gasesHere == null || !gasesHere.ContainsKey(name)) return 0;

            return gasesHere[name];
        }

        public void SendGasSpread(BlockPos pos, Dictionary<string, float> gases, bool mainThread = false)
        {
            System.Diagnostics.Debug.WriteLine(api.Side);
            (api as ICoreServerAPI)?.Event.PushEvent(mainThread ? "spreadGasMain" : "spreadGas", SerializeGasTreeData(pos, gases));
        }

        public TreeAttribute SerializeGasTreeData(BlockPos pos, Dictionary<string, float> gases)
        {
            if (pos == null ) return null;

            TreeAttribute tree = new TreeAttribute();
            
            tree.SetBlockPos("pos", pos);

            if (gases != null && gases.Count > 0)
            {
                TreeAttribute sGases = new TreeAttribute();

                foreach (var gas in gases)
                {
                    sGases.SetFloat(gas.Key, gas.Value);
                }

                tree.SetAttribute("gases", sGases);
            }

            return tree;
        }

        public static Dictionary<string, float> DeserializeGasTreeData(IAttribute data, out BlockPos pos)
        {
            TreeAttribute tree = data as TreeAttribute;
            pos = tree?.GetBlockPos("pos");
            ITreeAttribute gases = tree?.GetTreeAttribute("gases");

            if (pos == null) return null;
            Dictionary<string, float> dGases = null;

            if (gases != null)
            {
                dGases = new Dictionary<string, float>();

                foreach (var gas in gases)
                {
                    float? value = gases.TryGetFloat(gas.Key);
                    if (value == null) continue;
                    
                    dGases.Add(gas.Key, (float)value);
                }
            }

            return dGases;
        }

        public static void MergeGasIntoDict(string gasName, float gasValue, ref Dictionary<string, float> dest)
        {
            if (gasName == null || gasValue == 0 || dest == null) return;

            if (!dest.ContainsKey(gasName))
            {
                dest.Add(gasName, gasValue);
            }
            else
            {
                dest[gasName] += gasValue;
            }
        }

        public static void MergeGasDicts(Dictionary<string, float> source, ref Dictionary<string, float> dest)
        {
            if (source == null || dest == null) return;

            foreach (var gas in source)
            {
                if (gas.Key == "RADIUS")
                {
                    if (!dest.ContainsKey(gas.Key)) dest.Add(gas.Key, gas.Value);
                    else if (dest[gas.Key] < gas.Value) dest[gas.Key] = gas.Value;
                }
                else MergeGasIntoDict(gas.Key, gas.Value, ref dest);
            }
        }

        public float GetAirAmount(BlockPos pos)
        {
            Dictionary<string, float> gasesHere = GetGases(pos);

            if (gasesHere == null) return 1;

            float conc = 0;

            foreach (var gas in gasesHere)
            {
                if (LiteGasDict.ContainsKey(gas.Key))
                {
                    if (LiteGasDict[gas.Key] != null) conc += gas.Value * LiteGasDict[gas.Key].QualityMult; else conc += gas.Value;
                }
            }

            if (conc >= 2) return -1;
            if (conc < 0) return 1;

            return 1 - conc;
        }

        public float GetAcidity(BlockPos pos)
        {
            Dictionary<string, float> gasesHere = GetGases(pos);

            if (gasesHere == null) return 0;

            float conc = 0;

            foreach (var gas in gasesHere)
            {
                if (LiteGasDict.ContainsKey(gas.Key))
                {
                    if (LiteGasDict[gas.Key] != null && LiteGasDict[gas.Key].Acidic) conc += gas.Value;
                    if (conc >= 1) return 1;
                }
            }

            return conc;
        }

        public bool IsVolatile(BlockPos pos)
        {
            Dictionary<string, float> gasesHere = GetGases(pos);

            if (gasesHere == null) return false;

            foreach (var gas in gasesHere)
            {
                if (LiteGasDict.ContainsKey(gas.Key))
                {
                    if (LiteGasDict[gas.Key].FlammableAmount > 0 && gas.Value >= LiteGasDict[gas.Key].FlammableAmount) return true;
                }
            }

            return false;
        }

        public bool ShouldExplode(BlockPos pos)
        {
            Dictionary<string, float> gasesHere = GetGases(pos);

            if (gasesHere == null) return false;

            foreach (var gas in gasesHere)
            {
                if (LiteGasDict.ContainsKey(gas.Key))
                {
                    if (LiteGasDict[gas.Key].ExplosionAmount <= gas.Value) return true;
                }
            }

            return false;
        }

        public bool IsToxic(string name, float amount)
        {
            if (!LiteGasDict.ContainsKey(name)) return true;

            return amount > LiteGasDict[name].ToxicAt;
        }

        public Dictionary<string, float> CollectGases(BlockPos pos, int radius, string[] gasFilter)
        {
            IBlockAccessor blockAccessor = api.World.BlockAccessor;
            if (pos.Y < 1 || pos.Y > blockAccessor.MapSizeY) return null;

            Dictionary<string, float> result = new Dictionary<string, float>();
            Queue<Vec3i> checkQueue = new Queue<Vec3i>();
            Dictionary<Vec3i, IWorldChunk> chunks = new Dictionary<Vec3i, IWorldChunk>();
            Dictionary<Vec3i, Dictionary<int, Dictionary<string, float>>> gasChunks = new Dictionary<Vec3i, Dictionary<int, Dictionary<string, float>>>();
            HashSet<BlockPos> markedPositions = new HashSet<BlockPos>();
            Dictionary<int, Block> blocks = new Dictionary<int, Block>();
            Cuboidi bounds = new Cuboidi(pos.X - radius, pos.Y - radius, pos.Z - radius, pos.X + radius, pos.Y + radius, pos.Z + radius);
            BlockPos curPos = pos.Copy();
            BlockFacing[] faces = BlockFacing.ALLFACES;

            for (int x = bounds.MinX / blockAccessor.ChunkSize; x <= bounds.MaxX / blockAccessor.ChunkSize; x++)
            {
                for (int y = bounds.MinY / blockAccessor.ChunkSize; y <= bounds.MaxY / blockAccessor.ChunkSize; y++)
                {
                    for (int z = bounds.MinZ / blockAccessor.ChunkSize; z <= bounds.MaxZ / blockAccessor.ChunkSize; z++)
                    {
                        IWorldChunk chunk = blockAccessor.GetChunk(x, y, z);

                        Vec3i currentChunkPos = new Vec3i(x, y, z);
                        chunks.Add(currentChunkPos, chunk);
                        gasChunks.Add(currentChunkPos, GetGasesForChunk(chunk));

                    }
                }
            }
            if (chunks.Count < 1) return result;

            Vec3i originChunkVec = new Vec3i(pos.X / blockAccessor.ChunkSize, pos.Y / blockAccessor.ChunkSize, pos.Z / blockAccessor.ChunkSize);
            if (chunks[originChunkVec] == null) return null;
            checkQueue.Enqueue(pos.ToVec3i());
            markedPositions.Add(pos.Copy());
            Block starter = blockAccessor.GetBlock(pos);
            blocks.Add(starter.BlockId, starter);
            if (gasChunks[originChunkVec] != null && gasChunks[originChunkVec].ContainsKey(toLocalIndex(pos)))
            {
                Dictionary<string, float> gasesHere = gasChunks[originChunkVec][toLocalIndex(pos)];
                if (gasFilter == null) MergeGasDicts(gasesHere, ref result);
                else
                {
                    foreach (var gas in gasesHere)
                    {
                        if (gasFilter.Contains(gas.Key)) MergeGasIntoDict(gas.Key, gas.Value, ref result);
                    }
                }
            }

            while (checkQueue.Count > 0)
            {
                Vec3i bpos = checkQueue.Dequeue();
                Vec3i parentChunkVec = new Vec3i(bpos.X / blockAccessor.ChunkSize, bpos.Y / blockAccessor.ChunkSize, bpos.Z / blockAccessor.ChunkSize);

                Block parent = null;
                IWorldChunk parentChunk = chunks[parentChunkVec];
                if (!blocks.ContainsKey(parentChunk.Unpack_AndReadBlock(toLocalIndex(bpos.AsBlockPos)))) continue;

                parent = blocks[parentChunk.Unpack_AndReadBlock(toLocalIndex(bpos.AsBlockPos))];

                foreach (BlockFacing facing in faces)
                {
                    if (SolidCheck(parent, facing)) continue;
                    curPos.Set(bpos.X + facing.Normali.X, bpos.Y + facing.Normali.Y, bpos.Z + facing.Normali.Z);
                    if (!bounds.Contains(curPos) || markedPositions.Contains(curPos)) continue;
                    if (curPos.Y < 0 || curPos.Y > blockAccessor.MapSizeY) continue;

                    Vec3i curChunkVec = new Vec3i(curPos.X / blockAccessor.ChunkSize, curPos.Y / blockAccessor.ChunkSize, curPos.Z / blockAccessor.ChunkSize);
                    int chunkBid = toLocalIndex(curPos);
                    Block atPos = null;
                    IWorldChunk chunk = chunks[curChunkVec];

                    if (chunk == null) continue;

                    int blockId = chunk.Unpack_AndReadBlock(toLocalIndex(curPos));

                    if (!blocks.TryGetValue(blockId, out atPos)) atPos = blocks[blockId] = blockAccessor.GetBlock(blockId);

                    if (SolidCheck(atPos, facing.Opposite)) continue;
                    
                    if (gasChunks[curChunkVec] != null && gasChunks[curChunkVec].ContainsKey(chunkBid))
                    {
                        Dictionary<string, float> gasesHere = gasChunks[curChunkVec][chunkBid];
                        if (gasFilter == null) MergeGasDicts(gasesHere, ref result);
                        else
                        {
                            foreach (var gas in gasesHere)
                            {
                                if (gasFilter.Contains(gas.Key)) MergeGasIntoDict(gas.Key, gas.Value, ref result);
                            }
                        }
                    }

                    markedPositions.Add(curPos.Copy());
                    checkQueue.Enqueue(curPos.ToVec3i());
                }
            }

            Dictionary<string, float> reverse = new Dictionary<string, float>();

            foreach (var gas in result) reverse.Add(gas.Key, -gas.Value);
            reverse.Add("IGNORELIQUIDS", -100);
            reverse.Add("RADIUS", radius);
            SendGasSpread(pos, reverse);

            return result;
        }

        public bool SolidCheck(Block block, BlockFacing face)
        {
            if (block.Attributes?.KeyExists("gassysSolidSides") == true)
            {
                return block.Attributes["gassysSolidSides"].IsTrue(face.Code);
            }

            return block.SideSolid[face.Index];
        }

        int toLocalIndex(BlockPos pos)
        {
            return MapUtil.Index3d(pos.X % 32, pos.Y % 32, pos.Z % 32, 32, 32);
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class GasInfoLite
        {
            [JsonProperty]
            public bool Light;

            [JsonProperty]
            public float VentilateSpeed = 0;

            [JsonProperty]
            public bool Pollutant;

            [JsonProperty]
            public bool Distribute;

            [JsonProperty]
            public float ExplosionAmount = 2;

            [JsonProperty]
            public float SuffocateAmount = 1;

            [JsonProperty]
            public float FlammableAmount = 2;

            [JsonProperty]
            public bool PlantAbsorb;

            [JsonProperty]
            public bool Acidic;

            [JsonProperty]
            public Dictionary<string, float> Effects;

            [JsonProperty]
            public string BurnInto;

            [JsonProperty]
            public float ToxicAt = 0f;

            public float QualityMult
            {
                get
                {
                    if (SuffocateAmount == 0) return 1;

                    return 1 / SuffocateAmount;
                }
            }
        }
    }
}
