using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace GasApi
{
    public class BlockBehaviorSparkGas : BlockBehavior
    {
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            base.OnBlockBroken(world, pos, byPlayer, ref handling);
            
            if (world.Side != EnumAppSide.Server || byPlayer == null || !GasConfig.Loaded.GasesEnabled || !GasConfig.Loaded.Explosions || world.Rand.NextDouble() > GasConfig.Loaded.PickaxeExplosionChance) return;

            GasSystem gasHandler = world.Api.ModLoader.GetModSystem<GasSystem>();

            if (gasHandler.ShouldExplode(pos))
            {
                (world as IServerWorldAccessor)?.CreateExplosion(pos, EnumBlastType.RockBlast, 3, 3);
            }
            else
            {
                BlockPos tmpPos = pos.Copy();
                foreach (BlockFacing face in BlockFacing.ALLFACES)
                {
                    tmpPos.Set(pos);
                    tmpPos.Add(face);

                    if (gasHandler.ShouldExplode(tmpPos))
                    {
                        (world as IServerWorldAccessor)?.CreateExplosion(pos, EnumBlastType.RockBlast, 3, 3);
                        break;
                    }
                }
            }
        }

        public BlockBehaviorSparkGas(Block block) : base(block)
        {
        }
    }
}
