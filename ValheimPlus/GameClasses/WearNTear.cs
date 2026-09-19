using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ValheimPlus.Configurations;
using ValheimPlus.Utility;
using System.Diagnostics;
using JetBrains.Annotations;

namespace ValheimPlus.GameClasses
{
    /// <summary>
    /// Disable weather damage
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "UpdateWear")]
    public static class WearNTear_UpdateWear_Patch
    {
        [UsedImplicitly]
        private static void Prefix(float time, ref float ___m_rainTimer)
        {
            if (Configuration.Current.Building.IsEnabled && Configuration.Current.Building.noWeatherDamage)
            {
                // if the rain timer is perpetually set to the current time in prefix,
                // then the timer will never become large enough to trigger the weather effect.
                ___m_rainTimer = time;
            }
        }
    }

    /// <summary>
    /// Removes the heavy snow wear damage contribution in <c>WearNTear.UpdateWear</c>.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.UpdateWear))]
    public static class WearNTear_UpdateWear_HeavySnowDamage_Transpiler
    {
        [UsedImplicitly]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            try
            {
                // num += Game.instance.m_snowDamage;
                return new CodeMatcher(il)
                    .MatchExactlyOnce(
                        new CodeMatch(i => i.IsLdloc()),
                        new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Game), nameof(Game.instance))),
                        new CodeMatch(i => i.LoadsField(AccessTools.Field(typeof(Game), nameof(Game.m_snowDamage)))),
                        new CodeMatch(OpCodes.Add))
                    .Advance(3)
                    .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WearNTear_UpdateWear_HeavySnowDamage_Transpiler), nameof(Filter))))
                    .InstructionEnumeration();
            }
            catch (Exception e)
            {
                PatchLog.Failed(
                    nameof(WearNTear_UpdateWear_HeavySnowDamage_Transpiler),
                    "The `noHeavySnowDamage` setting will not work; heavy snow will keep damaging structures.",
                    e);
                return il;
            }
        }

        private static float Filter(float snowDamage)
        {
            var config = Configuration.Current.Building;
            return config.IsEnabled && config.noHeavySnowDamage ? 0f : snowDamage;
        }
    }

    /// <summary>
    /// Removes the heavy snow damage visual effect in <c>WearNTear.UpdateWear</c>, so that structures
    /// no longer show damage puffs once <c>noHeavySnowDamage</c> stops the damage itself.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.UpdateWear))]
    public static class WearNTear_UpdateWear_HeavySnowDamageEffect_Transpiler
    {
        private static readonly EffectList NoEffects = new();

        [UsedImplicitly]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            try
            {
                // Game.instance.m_snowDamageEffect.Create(...);
                return new CodeMatcher(il)
                    .MatchExactlyOnce(
                        new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Game), nameof(Game.instance))),
                        new CodeMatch(i => i.LoadsField(AccessTools.Field(typeof(Game), nameof(Game.m_snowDamageEffect)))))
                    .Advance(2)
                    .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WearNTear_UpdateWear_HeavySnowDamageEffect_Transpiler), nameof(Filter))))
                    .InstructionEnumeration();
            }
            catch (Exception e)
            {
                PatchLog.Failed(
                    nameof(WearNTear_UpdateWear_HeavySnowDamageEffect_Transpiler),
                    "The `noHeavySnowDamage` setting will still stop the damage, but structures will keep "
                    + "showing the heavy snow damage effect.",
                    e);
                return il;
            }
        }

        // An empty effect list spawns nothing, so the vanilla effect timer keeps its cadence untouched.
        private static EffectList Filter(EffectList snowDamageEffect)
        {
            var config = Configuration.Current.Building;
            return config.IsEnabled && config.noHeavySnowDamage ? NoEffects : snowDamageEffect;
        }
    }

    /// <summary>
    /// Removes the lava wear damage contribution in <c>WearNTear.UpdateWear</c>.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.UpdateWear))]
    public static class WearNTear_UpdateWear_LavaDamage_Transpiler
    {
        [UsedImplicitly]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            try
            {
                // float num3 = (flag ? 30f : 70f) * m_lavaValue;
                // Zeroing that product also zeroes the `num += num3 * resist` that is its only consumer.
                return new CodeMatcher(il)
                    .MatchExactlyOnce(
                        new CodeMatch(i => i.LoadsField(AccessTools.Field(typeof(WearNTear), nameof(WearNTear.m_lavaValue)))),
                        new CodeMatch(OpCodes.Mul),
                        new CodeMatch(i => i.IsStloc()))
                    .Advance(2)
                    .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WearNTear_UpdateWear_LavaDamage_Transpiler), nameof(Filter))))
                    .InstructionEnumeration();
            }
            catch (Exception e)
            {
                PatchLog.Failed(
                    nameof(WearNTear_UpdateWear_LavaDamage_Transpiler),
                    "The `noLavaDamage` setting will not work; lava will keep damaging structures.",
                    e);
                return il;
            }
        }

        private static float Filter(float lavaDamage)
        {
            var config = Configuration.Current.Building;
            return config.IsEnabled && config.noLavaDamage ? 0f : lavaDamage;
        }
    }

    /// <summary>
    /// Removes the lava contribution to the Ashlands damage shader, so that structures no longer look
    /// scorched once <c>noLavaDamage</c> stops the damage itself.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.UpdateAshlandsMaterialValues))]
    public static class WearNTear_UpdateAshlandsMaterialValues_LavaDamage_Transpiler
    {
        [UsedImplicitly]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            try
            {
                // SetAshlandsMaterialValue(Mathf.Max(m_lavaTimer, Mathf.Max(m_ashDamageTime, m_burnDamageTime)));
                // Only this read of m_lavaTimer is filtered; the field still drives the vanilla lava damage timing.
                return new CodeMatcher(il)
                    .MatchExactlyOnce(
                        new CodeMatch(i => i.LoadsField(AccessTools.Field(typeof(WearNTear), "m_lavaTimer"))),
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(i => i.LoadsField(AccessTools.Field(typeof(WearNTear), "m_ashDamageTime"))))
                    .Advance(1)
                    .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WearNTear_UpdateAshlandsMaterialValues_LavaDamage_Transpiler), nameof(Filter))))
                    .InstructionEnumeration();
            }
            catch (Exception e)
            {
                PatchLog.Failed(
                    nameof(WearNTear_UpdateAshlandsMaterialValues_LavaDamage_Transpiler),
                    "The `noLavaDamage` setting will still stop the damage, but structures will keep "
                    + "showing the lava damage material effect.",
                    e);
                return il;
            }
        }

        private static float Filter(float lavaTimer)
        {
            var config = Configuration.Current.Building;
            return config.IsEnabled && config.noLavaDamage ? 0f : lavaTimer;
        }
    }

    internal static class WearNTearCodeMatcherExtensions
    {
        /// <summary>
        /// Positions the matcher on the only occurrence of <paramref name="matches"/>, and throws when
        /// the sequence is missing or appears more than once.
        /// </summary>
        internal static CodeMatcher MatchExactlyOnce(this CodeMatcher matcher, params CodeMatch[] matches)
        {
            matcher.MatchStartForward(matches).ThrowIfNotMatch("No match for the expected instructions.");

            var duplicate = matcher.Clone().Advance(1).MatchStartForward(matches);
            if (duplicate.IsValid)
                throw new InvalidOperationException("More than one match for the expected instructions.");

            return matcher;
        }
    }



    /// <summary>
    /// Removes the integrity check for having a connected piece to the ground.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "HaveSupport")]
    public static class WearNTear_HaveSupport_Patch
    {
        private static void Postfix(ref bool __result)
        {
            if (Configuration.Current.StructuralIntegrity.IsEnabled && Configuration.Current.StructuralIntegrity.disableStructuralIntegrity)
            {
                __result = true;
            }
        }
    }

    /// <summary>
    /// Disable damage to player structures
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "ApplyDamage")]
    public static class WearNTear_ApplyDamage_Patch
    {
        private static readonly HashSet<string> UpdateWearMethodNames = new()
        {
            "UpdateWear",
            "DMD<WearNTear::UpdateWear>",
        };
        
        private static bool Prefix(ref WearNTear __instance, ref float damage)
        {
            // Gets the name of the method calling the ApplyDamage method
            StackTrace stackTrace = new StackTrace();
            string callingMethod = stackTrace.GetFrame(2).GetMethod().Name;

            if (!(Configuration.Current.StructuralIntegrity.IsEnabled && __instance.m_piece && __instance.m_piece.IsPlacedByPlayer() && !UpdateWearMethodNames.Contains(callingMethod)))
                return true;

            if (__instance.m_piece.m_name.StartsWith("$ship"))
            {
                if (Configuration.Current.StructuralIntegrity.disableDamageToPlayerBoats ||
                    (Configuration.Current.StructuralIntegrity.disableWaterDamageToPlayerBoats &&
                     stackTrace.GetFrame(15).GetMethod().Name == "UpdateWaterForce")) return false;

                return true;
            }
            if (__instance.m_piece.m_name.StartsWith("$tool_cart"))
            {
                if (Configuration.Current.StructuralIntegrity.disableDamageToPlayerCarts ||
                    (Configuration.Current.StructuralIntegrity.disableWaterDamageToPlayerCarts &&
                     stackTrace.GetFrame(15).GetMethod().Name == "UpdateWaterForce")) return false;

                return true;
            }
            return !Configuration.Current.StructuralIntegrity.disableDamageToPlayerStructures;
        }
    }

    /// <summary>
    /// Disable structural integrity
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.GetMaterialProperties))]
    public static class WearNTear_GetMaterialProperties_Patch
    {
        private static readonly Dictionary<WearNTear.MaterialType, Func<float>> Multipliers = new()
        {
            [WearNTear.MaterialType.Wood] = () => Configuration.Current.StructuralIntegrity.wood,
            [WearNTear.MaterialType.Stone] = () => Configuration.Current.StructuralIntegrity.stone,
            [WearNTear.MaterialType.Iron] = () => Configuration.Current.StructuralIntegrity.iron,
            [WearNTear.MaterialType.HardWood] = () => Configuration.Current.StructuralIntegrity.hardWood,
            [WearNTear.MaterialType.Marble] = () => Configuration.Current.StructuralIntegrity.marble,
            [WearNTear.MaterialType.Ashstone] = () => Configuration.Current.StructuralIntegrity.ashstone,
            [WearNTear.MaterialType.Ancient] = () => Configuration.Current.StructuralIntegrity.ancient,
            [WearNTear.MaterialType.Ice] = () => Configuration.Current.StructuralIntegrity.ice,
            [WearNTear.MaterialType.Timberwood] = () => Configuration.Current.StructuralIntegrity.timberwood,
        };

        [UsedImplicitly]
        private static void Postfix(ref WearNTear __instance, ref float horizontalLoss, ref float verticalLoss)
        {
            if (!Configuration.Current.StructuralIntegrity.IsEnabled) return;
            if (Configuration.Current.StructuralIntegrity.disableStructuralIntegrity)
            {
                verticalLoss = 0f;
                horizontalLoss = 0f;
                return;
            }

            // Unknown material type, don't modify.
            if (!Multipliers.TryGetValue(__instance.m_materialType, out var multiplier)) return;
            
            // scale the loss number between its current number and 0 based on the user config.
            float clampedMultiplier = Helper.Clamp(multiplier(), 0 , 100);
            verticalLoss -= verticalLoss / 100 * clampedMultiplier;
            horizontalLoss -= horizontalLoss / 100 * clampedMultiplier;
        }
    }
}
