using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;
using Vintagestory.API.Server;

namespace GasApi
{
    public class BlockEntityBehaviorBurningProduces : BlockEntityBehavior
    {
        GasSystem gasHandler;
        public Dictionary<string, float> produceGas;
        BlockPos blockPos
        {
            get { return Blockentity.Pos; }
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            gasHandler = api.ModLoader.GetModSystem<GasSystem>();
            Blockentity.RegisterGameTickListener(ProduceCO, 5000);
            produceGas = properties["produceGas"].AsObject(new Dictionary<string, float>());
            if (produceGas == null || produceGas.Count < 1)
            {
                produceGas = new Dictionary<string, float>();
                produceGas.Add("carbonmonoxide", 0.2f);
                produceGas.Add("carbondioxide", 0.5f);
            }
        }

        public void ProduceCO(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;

            if (IsBurning())
            {
                if (GasConfig.Loaded.Explosions && gasHandler.ShouldExplode(blockPos))
                {
                    (Api.World as IServerWorldAccessor).CreateExplosion(blockPos, EnumBlastType.RockBlast, 3, 3);
                }
                else if (GasConfig.Loaded.Smoke)
                {
                   gasHandler.QueueGasExchange(new Dictionary<string, float>(produceGas), blockPos);
                }
            }
        }

        private bool IsBurning()
        {
            return (Blockentity as BlockEntityFirepit)?.IsBurning == true ||
                (Blockentity as BlockEntityForge)?.IsBurning == true ||
                (Blockentity as BlockEntityBloomery)?.IsBurning == true ||
                (Blockentity as BlockEntityCoalPile)?.IsBurning == true ||
                (Blockentity as BlockEntityTorch)?.Block.LightHsv?[2] > 0 ||
                (Blockentity as BlockEntityTorchHolder)?.Block.LightHsv?[2] > 0 ||
                (Blockentity as BlockEntityPitKiln)?.Lit == true ||
                (Blockentity as BlockEntityCharcoalPit)?.Lit == true ||
                (Blockentity as BlockEntityBoiler)?.IsBurning == true ||
                Blockentity.Block.Code.Path == "fire";
        }

        public BlockEntityBehaviorBurningProduces(BlockEntity blockentity) : base(blockentity)
        {
        }
    }
}
