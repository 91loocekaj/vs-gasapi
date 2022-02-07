using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;
using Vintagestory.API.Server;

namespace GasApi
{
    public class BlockEntityBehaviorPlanterAbsorbs : BlockEntityBehavior
    {
        GasSystem gasHandler;
        public Dictionary<string, float> gasScrub;
        public float scrubAmount = 1;
        BlockPos blockPos
        {
            get { return Blockentity.Pos; }
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            gasHandler = api.ModLoader.GetModSystem<GasSystem>();
            Blockentity.RegisterGameTickListener(RemoveCO2, 5000);
            scrubAmount = properties["scrubAmount"].AsFloat(1);
            gasScrub = new Dictionary<string, float>();
            gasScrub.Add("THISISAPLANT", scrubAmount);
        }

        public void RemoveCO2(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;

            BlockEntityPlantContainer bpc = Blockentity as BlockEntityPlantContainer;
            if (bpc == null || Api.World.BlockAccessor.GetLightLevel(Blockentity.Pos, EnumLightLevelType.TimeOfDaySunLight) < 13) return;

            if (bpc.Inventory[0].Empty || bpc.Inventory[0].Itemstack.Block?.BlockMaterial != EnumBlockMaterial.Plant || bpc.Inventory[0].Itemstack.Collectible.Code.Path.StartsWith("mushroom")) return;

            gasHandler.QueueGasExchange(new Dictionary<string, float>(gasScrub), blockPos);
        }

        public BlockEntityBehaviorPlanterAbsorbs(BlockEntity blockentity) : base(blockentity)
        {
        }
    }
}
