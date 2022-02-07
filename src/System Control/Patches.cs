using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using Vintagestory.Server;
using Vintagestory.ServerMods.NoObf;

namespace GasApi
{
    [HarmonyPatch(typeof(EntitySidedProperties))]
    public class BreatheOverride
    {
        [HarmonyPrepare]
        static bool Prepare(MethodBase original, Harmony harmony)
        {
            //From Melchoir
            if (original != null)
            {
                foreach (var patched in harmony.GetPatchedMethods())
                {
                    if (patched.Name == original.Name) return false;
                }
            }

            return true;
        }

        [HarmonyPatch("loadBehaviors")]
        [HarmonyPostfix]
        static void ChangeToAir(Entity entity, EntityProperties properties, EntitySidedProperties __instance, JsonObject[] ___BehaviorsAsJsonObj)
        {
            if (GasConfig.Loaded.BreathingEnabled)
            {
                if (!(entity is EntityPlayer) || GasConfig.Loaded.PlayerBreathingEnabled)
                {
                    for (int i = 0; i < __instance.Behaviors.Count; i++)
                    {
                        if (__instance.Behaviors[i] is EntityBehaviorBreathe)
                        {
                            EntityBehavior air = new EntityBehaviorAir(entity);
                            air.Initialize(properties, ___BehaviorsAsJsonObj[i]);

                            __instance.Behaviors[i] = air;
                            break;
                        }
                    }
                }
            }

            if (GasConfig.Loaded.GasesEnabled && entity.Api.Side == EnumAppSide.Server)
            {
                EntityBehavior gas = new EntityBehaviorGas(entity);
                gas.Initialize(properties, null);

                entity.AddBehavior(gas);
            }
        }
    }

    [HarmonyPatch(typeof(ServerMain))]
    public class ExplosiveAdditions
    {
        [HarmonyPrepare]
        static bool Prepare(MethodBase original, Harmony harmony)
        {
            //From Melchoir
            if (original != null)
            {
                foreach (var patched in harmony.GetPatchedMethods())
                {
                    if (patched.Name == original.Name) return false;
                }
            }

            return true;
        }

        [HarmonyPatch("CreateExplosion")]
        [HarmonyPrefix]
        static void GasSetup(BlockPos pos, EnumBlastType blastType, double destructionRadius, double injureRadius, ServerMain __instance)
        {
            //Register this explosion
            GasSystem gasHandler = __instance.Api.ModLoader.GetModSystem<GasSystem>();

            if (gasHandler == null) return;

            gasHandler.SetupExplosion(pos, (int)destructionRadius);
        }

        [HarmonyPatch("CreateExplosion")]
        [HarmonyPostfix]
        static void GasSpread(BlockPos pos, EnumBlastType blastType, double destructionRadius, double injureRadius, ServerMain __instance)
        {
            GasSystem gasHandler = __instance.Api.ModLoader.GetModSystem<GasSystem>();

            if (gasHandler == null) return;

            gasHandler.EnqueueExplosion(pos);
        }
    }

    [HarmonyPatch(typeof(BlockEntityContainer))]
    public class ContainerBonus
    {
        [HarmonyPrepare]
        static bool Prepare(MethodBase original, Harmony harmony)
        {
            //From Melchoir
            if (original != null)
            {
                foreach (var patched in harmony.GetPatchedMethods())
                {
                    if (patched.Name == original.Name) return false;
                }
            }

            return GasConfig.Loaded.ContainerBonus;
        }

        [HarmonyPatch("GetPerishRate")]
        [HarmonyPostfix]
        static void AirQuality(BlockEntityContainer __instance, ref float __result)
        {
            float airQuality = __instance.Api.ModLoader.GetModSystem<GasSystem>().GetAirAmount(__instance.Pos);

            if (airQuality >= 0) return;

            __result *= 1 - (Math.Abs(airQuality)/2);
        }
    }

    [HarmonyPatch(typeof(BlockBehaviorExchangeOnInteract))]
    public class TrapDoorHandler
    {
        [HarmonyPrepare]
        static bool Prepare(MethodBase original, Harmony harmony)
        {
            //From Melchoir
            if (original != null)
            {
                foreach (var patched in harmony.GetPatchedMethods())
                {
                    if (patched.Name == original.Name) return false;
                }
            }

            return true;
        }

        [HarmonyPatch("OnBlockInteractStart")]
        [HarmonyPostfix]
        static void TrapdoorGasCheck(IWorldAccessor world, BlockSelection blockSel, ref bool __result)
        {
            if (__result && world.Side == EnumAppSide.Server)
            {
                world.Api.ModLoader.GetModSystem<GasSystem>()?.QueueGasExchange(null, blockSel.Position);
            }
        }
    }

    [HarmonyPatch(typeof(BlockDoor))]
    public class DoorHandler
    {
        [HarmonyPrepare]
        static bool Prepare(MethodBase original, Harmony harmony)
        {
            //From Melchoir
            if (original != null)
            {
                foreach (var patched in harmony.GetPatchedMethods())
                {
                    if (patched.Name == original.Name) return false;
                }
            }

            return true;
        }

        [HarmonyPatch("Open")]
        [HarmonyPostfix]
        static void DoorGasCheck(IWorldAccessor world, BlockPos position)
        {
            if (world.Side == EnumAppSide.Server)
            {
                world.Api.ModLoader.GetModSystem<GasSystem>()?.QueueGasExchange(null, position);
            }
        }
    }

    [HarmonyPatch(typeof(EntityBehaviorHealth))]
    public class HeavyHitsStat
    {
        [HarmonyPrepare]
        static bool Prepare()
        {
            return true;
        }

        [HarmonyPatch("Initialize")]
        [HarmonyPostfix]
        static void DefenseBonus(EntityBehaviorHealth __instance)
        {
            __instance.onDamaged += (dmg, source) => { return dmg + (__instance.entity.Stats.GetBlended("heavyHits") - 1); };
        }
    }

    [HarmonyPatch(typeof(Entity))]
    public class EntityBurn
    {
        [HarmonyPrepare]
        static bool Prepare(MethodBase original, Harmony harmony)
        {
            //From Melchoir
            if (original != null)
            {
                foreach (var patched in harmony.GetPatchedMethods())
                {
                    if (patched.Name == original.Name) return false;
                }
            }

            return true;
        }

        [HarmonyPatch("ReceiveDamage")]
        [HarmonyPrefix]
        static void Burn(Entity __instance, DamageSource damageSource)
        {
            if (damageSource?.Source != EnumDamageSource.Explosion) return;
            if (GasConfig.Loaded.FlammableGas && __instance.Api.ModLoader.GetModSystem<GasSystem>().IsVolatile(__instance.ServerPos.AsBlockPos))
            {
                __instance.Ignite();
            }
        }
    }

}
