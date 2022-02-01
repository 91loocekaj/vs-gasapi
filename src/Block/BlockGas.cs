using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using System.Collections.Generic;

namespace GasApi
{
    public class BlockGas: Block
    {
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);

            if (world.Side != EnumAppSide.Server) return;
            
            Dictionary<string, float> tester = new Dictionary<string, float>();
            tester.Add(FirstCodePart(1), 1);

            world.Api.ModLoader.GetModSystem<GasSystem>().QueueGasExchange(tester, blockPos);
            world.BlockAccessor.SetBlock(0, blockPos);
        }

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldgenRandom)
        {
            Dictionary<string, float> tester = new Dictionary<string, float>();
            tester.Add(FirstCodePart(1), 1);

            api.ModLoader.GetModSystem<GasSystem>().SetGases(pos, tester);
            blockAccessor.SetBlock(0, pos);

            return true;
        }
    }
}
