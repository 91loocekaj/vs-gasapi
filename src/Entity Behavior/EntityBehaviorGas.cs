using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace GasApi
{
    public class EntityBehaviorGas : EntityBehavior
    {
        float timeKeeper;
        GasSystem gasHandler;

        ItemStack LeftHand
        {
            get { return (entity as EntityAgent)?.LeftHandItemSlot?.Itemstack; }
        }

        ItemStack RightHand
        {
            get { return (entity as EntityAgent)?.RightHandItemSlot?.Itemstack; }
        }

        ItemStack SelfStack
        {
            get { return (entity as EntityItem)?.Itemstack; }
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            gasHandler = entity.Api.ModLoader.GetModSystem<GasSystem>();
        }

        public override void OnGameTick(float deltaTime)
        {
            if (timeKeeper < 3)
            {
                timeKeeper += deltaTime;
                return;
            }

            timeKeeper = 0;

            BlockPos entityPos = entity.SidedPos.AsBlockPos;

            if (GasConfig.Loaded.Explosions && (entity.IsOnFire || HasFire()))
            {
                if (gasHandler.ShouldExplode(entityPos))
                {
                    (entity.World as IServerWorldAccessor).CreateExplosion(entity.ServerPos.AsBlockPos, EnumBlastType.RockBlast, 3, 3);
                }
                else if (gasHandler.IsVolatile(entityPos)) entity.Ignite();
            }

            if (GasConfig.Loaded.Acid && entity.FeetInLiquid)
            {
                float acidAmount = gasHandler.GetAcidity(entity.ServerPos.AsBlockPos);
                EntityPlayer player;

                if (acidAmount > 0.35f && (player = entity as EntityPlayer) != null)
                {
                    IInventory inv = player.Player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);

                    for (int a = 12; a < 15; a++)
                    {
                        if (inv?[a]?.Itemstack?.Collectible.Durability > 1) inv[a].Itemstack.Collectible.DamageItem(entity.World, entity, inv[a], 31);
                    }
                }

                if (acidAmount > 0.7) entity.ReceiveDamage(new DamageSource() { Type = EnumDamageType.Poison }, 1);
            }
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            if (GasConfig.Loaded.Smoke && damageSourceForDeath?.Type == EnumDamageType.Fire)
            {
                GasSystem gasHandler = entity.Api.ModLoader.GetModSystem<GasSystem>();

                Dictionary<string, float> gases = new Dictionary<string, float>();
                gases.Add("co2", 0.05f * gasHandler.GasSpreadBlockRadius);
                gases.Add("co", 0.005f * gasHandler.GasSpreadBlockRadius);

                gasHandler.QueueGasExchange(gases, entity.ServerPos.AsBlockPos);
            }
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            base.OnEntityReceiveDamage(damageSource, ref damage);

            //Ignite in an explosion if in flammable gas

            if (GasConfig.Loaded.FlammableGas && damageSource?.Source == EnumDamageSource.Explosion && gasHandler.IsVolatile(entity.ServerPos.AsBlockPos))
            {
                entity.Ignite();
            }
        }

        public bool HasFire()
        {
            if (LeftHand != null && LeftHand.Collectible is BlockTorch && LeftHand.Block.LightHsv != null) return true;

            if (RightHand != null && RightHand.Collectible is BlockTorch && RightHand.Block.LightHsv != null) return true;

            if (SelfStack != null && SelfStack.Collectible is BlockTorch && SelfStack.Block.LightHsv != null) return true;

            return false;
        }

        public EntityBehaviorGas(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "gasinteract";
        }
    }
}
