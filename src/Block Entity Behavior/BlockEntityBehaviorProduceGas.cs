using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;
using Vintagestory.API.Server;

namespace GasApi
{
    public class BlockEntityBehaviorProduceGas : BlockEntityBehavior
    {
        GasSystem gasHandler;
        public Dictionary<string, float> produceGas;
        int updateTimeInMS;
        double updateTimeInHours;
        double lastTimeProduced;

        BlockPos blockPos
        {
            get { return Blockentity.Pos; }
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            gasHandler = api.ModLoader.GetModSystem<GasSystem>();
            produceGas = properties["produceGas"].AsObject(new Dictionary<string, float>());
            updateTimeInMS = properties["updateMS"].AsInt(10000);
            updateTimeInHours = properties["updateHours"].AsDouble();
            Blockentity.RegisterGameTickListener(ProduceGas, updateTimeInMS);
        }

        public virtual void ProduceGas(float dt)
        {
            if (Blockentity.Api.World.Calendar.TotalHours - lastTimeProduced < updateTimeInHours) return;
            if (Api.Side != EnumAppSide.Server || produceGas == null || produceGas.Count < 1) return;

            lastTimeProduced = Blockentity.Api.World.Calendar.TotalHours;

            gasHandler.QueueGasExchange(new Dictionary<string, float>(produceGas), blockPos);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetDouble("gassyslastProduced", lastTimeProduced);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            lastTimeProduced = tree.GetDouble("gassyslastProduced");
        }

        public BlockEntityBehaviorProduceGas(BlockEntity blockentity) : base(blockentity)
        {
        }
    }
}
