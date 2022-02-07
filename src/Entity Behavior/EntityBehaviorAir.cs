using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API;
using Vintagestory.API.Common.Entities;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;
using System;
using Vintagestory.API.Config;
using System.Linq;

namespace GasApi
{
    public class EntityBehaviorAir : EntityBehavior
    {
        ITreeAttribute airTree;
        GasSystem atmosphere;

        public bool waterBreather = false;
        float damageOn = 0;
        double timer;
        float effectsTimer;
        float scubaTimer;

        BlockPos HeadBlock
        {
            get { return (entity as EntityAgent)?.SidedPos.AsBlockPos.Add(0, (float)entity.Properties.EyeHeight, 0) ?? entity.SidedPos.AsBlockPos; }
        }

        public float Air
        {
            get { return airTree.GetFloat("currentair"); }
            set { airTree.SetFloat("currentair", GameMath.Clamp(value, 0, MaxAir)); entity.WatchedAttributes.MarkPathDirty("air"); }
        }

        public float MaxAir
        {
            get { return airTree.GetFloat("maxair"); }
            set { airTree.SetFloat("maxair", value); entity.WatchedAttributes.MarkPathDirty("air"); }
        }

        public float BaseMaxAir
        {
            get { return airTree.GetFloat("basemaxair"); }
            set
            {
                airTree.SetFloat("basemaxair", value);
                entity.WatchedAttributes.MarkPathDirty("air");
            }
        }

        public string[] InSystem
        {
            get { return (airTree["effects"] as StringArrayAttribute)?.value; }
            set { airTree["effects"] = new StringArrayAttribute(value ?? new string[0]); entity.WatchedAttributes.MarkPathDirty("condsTree"); }
        }

        public Dictionary<string, float> MaxAirModifiers = new Dictionary<string, float>();

        public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
        {
            atmosphere = entity.Api.ModLoader.GetModSystem<GasSystem>();
            timer = entity.World.Calendar.TotalHours;
            waterBreather = typeAttributes.IsTrue("waterBreather");
            airTree = entity.WatchedAttributes.GetTreeAttribute("air");

            if (airTree == null)
            {
                entity.WatchedAttributes.SetAttribute("air", airTree = new TreeAttribute());

                Air = typeAttributes["currentair"].AsFloat(15);
                BaseMaxAir = typeAttributes["maxair"].AsFloat(15);
                InSystem = new string[0];
                UpdateMaxAir();
                return;
            }

            Air = airTree.GetFloat("currentair");
            BaseMaxAir = airTree.GetFloat("basemaxair");

            if (BaseMaxAir == 0) BaseMaxAir = typeAttributes["maxair"].AsFloat(15);


            UpdateMaxAir();
        }

        public void UpdateMaxAir()
        {
            float totalMaxAir = BaseMaxAir;
            foreach (var val in MaxAirModifiers) totalMaxAir += val.Value;

            totalMaxAir += entity.Stats.GetBlended("maxairExtraPoints") - 1;

            bool wasFullAir = Air >= MaxAir;

            MaxAir = totalMaxAir;

            if (wasFullAir) Air = MaxAir;
        }

        public override void OnGameTick(float deltaTime)
        {
            if (!entity.Alive) return;

            UpdateMaxAir();

            if (GasConfig.Loaded.ToxicEffects) HandleEffects(deltaTime);

            //Exhaling
            if (GasConfig.Loaded.Exhaling && entity.World.Calendar.TotalHours - timer >= 10)
            {
                timer = entity.World.Calendar.TotalHours;

                Dictionary<string, float> exhaust = new Dictionary<string, float>();
                exhaust.Add("carbondioxide", 1f);

                atmosphere.QueueGasExchange(exhaust, entity.SidedPos.AsBlockPos);
            }

            //Handle air intake
            if (HasScubaSet())
            {
                Air += deltaTime * entity.Stats.GetBlended("airRecovery");
                scubaTimer += deltaTime;

                if (scubaTimer >= 1)
                {
                    DamageTank(1);
                    scubaTimer--;
                }
            }
            else
            {
                if (EntityUnderwater())
                {
                    //In water
                    if (waterBreather)
                    {
                        if (Air < MaxAir) { Air += deltaTime * entity.Stats.GetBlended("airRecovery"); }
                    }
                    else
                    {
                        if (Air > 0) Air -= deltaTime * entity.Stats.GetBlended("airLoss");
                    }
                }
                else
                {
                    //On land
                    if (waterBreather)
                    {
                        if (Air > 0) Air -= deltaTime * entity.Stats.GetBlended("airLoss");
                    }
                    else
                    {
                        Air += AirQuality(deltaTime);
                    }
                }
            }


            //Suffocation
            if (Air <= 0)
            {
                damageOn += deltaTime;

                if (damageOn >= 1)
                {
                    entity.ReceiveDamage(new DamageSource() { Type = EnumDamageType.Suffocation, Source = EnumDamageSource.Drown }, 1f);
                    damageOn = 0;
                }
            }
        }

        public bool EntityUnderwater()
        {
            if (!entity.Swimming) return false;

            Vec3d head = entity.SidedPos.XYZ.AddCopy(0, entity.CollisionBox.Height, 0);
            Block liquid = entity.World.BlockAccessor.GetBlock(head.AsBlockPos);

            return liquid.IsLiquid() && (entity.World.BlockAccessor.GetBlock(head.AsBlockPos.Add(0, 1, 0)).IsLiquid() || head.Y - (0.25 * entity.CollisionBox.Height) < head.AsBlockPos.Y + ((liquid.LiquidLevel + 1) / 8));
        }

        public float AirQuality(float air)
        {
            BlockPos head = HeadBlock;
            Block gas = entity.World.BlockAccessor.GetBlock(head);
            float mult = atmosphere.GetAirAmount(head);
            bool solid = true;

            foreach (bool face in gas.SideSolid)
            {
                solid &= face;
            }

            if (solid) return -1f * entity.Stats.GetBlended("airLoss") * air;

            return air * mult * (mult > 0 ? entity.Stats.GetBlended("airRecovery") : entity.Stats.GetBlended("airLoss"));
        }

        private void RemoveEffect(string name)
        {
            if (GasSystem.GasDictionary != null && GasSystem.GasDictionary.ContainsKey(name))
            {
                if (GasSystem.GasDictionary[name].Effects != null && GasSystem.GasDictionary[name].Effects.Count > 0)
                {
                    foreach (var effect in GasSystem.GasDictionary[name].Effects)
                    {
                        entity.Stats.Remove(effect.Key, "gaseffect-" + name);
                    }
                }
            }
        }

        private void SetEffect(string name, float conc)
        {
            if (GasSystem.GasDictionary != null && GasSystem.GasDictionary.ContainsKey(name))
            {
                if (GasSystem.GasDictionary[name].Effects != null && GasSystem.GasDictionary[name].Effects.Count > 0)
                {
                    foreach (var effect in GasSystem.GasDictionary[name].Effects)
                    {
                        //System.Diagnostics.Debug.WriteLine(effect.Value * conc);
                        entity.Stats.Set(effect.Key, "gaseffect-" + name, effect.Value * conc, true);
                    }
                }
            }
        }

        private void HandleEffects(float time)
        {
            effectsTimer += time;

            if (effectsTimer >= 3)
            {
                effectsTimer = 0;

                Dictionary<string, float> gasesHere = atmosphere.GetGases(HeadBlock);
                bool usedMask = false;


                if (gasesHere == null || gasesHere.Count < 1)
                {
                    if (InSystem != null && InSystem.Length > 0)
                    {
                        for (int i = 0; i < InSystem.Length; i++) RemoveEffect(InSystem[i]);
                        InSystem = new string[0];
                    }
                }
                else
                {
                    List<string> newSystem = new List<string>();

                    foreach (var gas in gasesHere)
                    {
                        if (atmosphere.IsToxic(gas.Key, gas.Value))
                            if (!BlockedByGasMaskOrScuba(gas.Key, ref usedMask))
                            {
                                SetEffect(gas.Key, gas.Value);
                                newSystem.Add(gas.Key);
                            }
                    }

                    if (InSystem != null && InSystem.Length > 0)
                    {
                        for (int g = 0; g < InSystem.Length; g++)
                        {
                            if (!newSystem.Contains(InSystem[g])) RemoveEffect(InSystem[g]);
                        }
                    }

                    if (usedMask) DamageMask(3);
                    InSystem = newSystem.ToArray();
                    entity.WatchedAttributes.MarkPathDirty("stats");
                }
            }
        }

        private bool BlockedByGasMaskOrScuba(string name, ref bool gasmask)
        {
            if (!(entity is EntityPlayer)) return false;

            IPlayerInventoryManager inv = (entity as EntityPlayer)?.Player.InventoryManager;
            ItemStack mask = inv.GetOwnInventory(GlobalConstants.characterInvClassName)?[(int)EnumCharacterDressType.Face].Itemstack;

            if (mask == null || mask.Collectible.GetDurability(mask) <= 0 || mask.ItemAttributes == null) return false;

            if (GasConfig.Loaded.AllowScuba && mask.ItemAttributes.IsTrue("gassysScubaMask"))
            {
                IInventory backpacks = inv.GetOwnInventory(GlobalConstants.backpackInvClassName);
                if (backpacks != null && backpacks.Count >= 6)
                {
                    ItemStack gastank = backpacks[5].Itemstack;
                    if (gastank != null && gastank.ItemAttributes != null && gastank.ItemAttributes.IsTrue("gassysScubaTank") && gastank.Collectible.GetDurability(gastank) > 0)
                    {
                        return true;
                    }
                }
            }

            if (GasConfig.Loaded.AllowMasks)
            {
                string[] gasProt = mask.ItemAttributes["gassysGasMaskProtection"].AsArray<string>();

                if (gasProt == null || !gasProt.Contains(name)) return false;

                gasmask = true;
                return true;
            }

            return false;
        }

        private bool HasScubaSet()
        {
            if (!GasConfig.Loaded.AllowScuba || !(entity is EntityPlayer)) return false;

            IPlayerInventoryManager inv = (entity as EntityPlayer)?.Player.InventoryManager;
            ItemStack mask = inv.GetOwnInventory(GlobalConstants.characterInvClassName)?[(int)EnumCharacterDressType.Face].Itemstack;

            if (mask == null || mask.Collectible.GetDurability(mask) <= 0 || mask.ItemAttributes == null) return false;

            if (mask.ItemAttributes.IsTrue("gassysScubaMask"))
            {
                IInventory backpacks = inv.GetOwnInventory(GlobalConstants.backpackInvClassName);
                if (backpacks != null && backpacks.Count >= 6)
                {
                    ItemStack gastank = backpacks[5].Itemstack;
                    if (gastank != null && gastank.ItemAttributes != null && gastank.ItemAttributes.IsTrue("gassysScubaTank") && gastank.Collectible.GetDurability(gastank) > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void DamageMask(int dam)
        {
            ItemSlot maskSlot = (entity as EntityPlayer)?.Player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName)?[(int)EnumCharacterDressType.Face];
            ItemStack mask = maskSlot?.Itemstack;

            if (mask == null) return;

            mask.Collectible.DamageItem(entity.World, entity, maskSlot, dam);
        }

        private void DamageTank(int dam)
        {
            IInventory tankInv = (entity as EntityPlayer)?.Player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            if (tankInv == null || tankInv.Count < 6) return;
            ItemSlot tankSlot = tankInv[5];
            ItemStack tank = tankSlot?.Itemstack;

            if (tank == null) return;

            tank.Collectible.DamageItem(entity.World, entity, tankSlot, dam);
        }

        public EntityBehaviorAir(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "air";
        }
    }
}
