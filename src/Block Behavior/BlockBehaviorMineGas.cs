using Vintagestory.API.Common;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;

namespace GasApi
{
    public class BlockBehaviorMineGas : BlockBehavior
    {
        public Dictionary<string, float> produceGas;
        public bool onRemove = false;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            produceGas = properties["produceGas"].AsObject(new Dictionary<string, float>());
            onRemove = properties.IsTrue("onRemove");
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            base.OnBlockBroken(world, pos, byPlayer, ref handling);

            if (onRemove || !GasConfig.Loaded.GasesEnabled || world.Side != EnumAppSide.Server || produceGas == null || produceGas.Count < 1) return;

            world.Api.ModLoader.GetModSystem<GasSystem>()?.QueueGasExchange(new Dictionary<string, float>(produceGas), pos);
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            base.OnBlockRemoved(world, pos, ref handling);

            if (!onRemove || !GasConfig.Loaded.GasesEnabled || world.Side != EnumAppSide.Server || produceGas == null || produceGas.Count < 1) return;

            world.Api.ModLoader.GetModSystem<GasSystem>()?.QueueGasExchange(new Dictionary<string, float>(produceGas), pos);
        }

        public BlockBehaviorMineGas(Block block) : base(block)
        {
        }
    }
}
