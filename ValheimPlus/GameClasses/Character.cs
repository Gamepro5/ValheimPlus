using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;
using UnityEngine;
using ValheimPlus;
using ValheimPlus.Configurations;
using ValheimPlus.Utility;

namespace ValheimPlus.GameClasses
{
    /// <summary>
    /// Determines what happens when a tamed creature takes damage.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    public static class Character_Damage_Patch
    {
        public static void Prefix(ref Character __instance, ref HitData hit)
        {
            if (Configuration.Current.Tameable.IsEnabled)
            {
                // if immortal
                if (isMortality(TameableMortalityTypes.Immortal))
                {
                    // Network & Tameable component
                    ZDO zdo = __instance.m_nview.GetZDO();
                    Tameable tamed = __instance.GetComponent<Tameable>();

                    // Is tamed, has network, has valid hit data, tamed component is present.
                    if (!__instance.IsTamed() || zdo == null || hit == null || tamed == null)
                        return;

                    // Check if it should ignore the hit damage (includes stunned status check)
                    if (ShouldIgnoreDamage(__instance, hit, zdo))
                        hit = new HitData();
                }


            }
        }

        public static void Postfix(ref Character __instance, ref HitData hit)
        {
            if (Configuration.Current.Tameable.IsEnabled)
            {
                // if essential
                if (isMortality(TameableMortalityTypes.Essential))
                {
                    // Network & Tameable component
                    ZDO zdo = __instance.m_nview.GetZDO();
                    Tameable tamed = __instance.GetComponent<Tameable>();

                    // Is tamed, has network, has valid hit data, tamed component is present.
                    if (!__instance.IsTamed() || zdo == null || hit == null || tamed == null)
                        return;

                    // if killed on this hit
                    if (__instance.GetHealth() <= 5f)
                    {
                        // Allow players to kill the tamed creature with ownerDamageOverride
                        if(ShouldIgnoreDamage(__instance, hit, zdo)){
                            __instance.SetHealth(__instance.GetMaxHealth());
                            __instance.m_animator.SetBool("sleeping", true);
                            zdo.Set("sleeping", true);
                            zdo.Set("isRecoveringFromStun", true);
                        }
                    }
                }

            }
        }


        private static bool isMortality(TameableMortalityTypes type)
        {
            TameableMortalityTypes setting = (TameableMortalityTypes)Mathf.Clamp(Configuration.Current.Tameable.mortality, 0, 2);
            if (setting == type)
            {
                return true;
            }
            return false;
        }

        private static bool ShouldIgnoreDamage(Character __instance, HitData hit, ZDO zdo)
        {
            // The only valid attack from a player is with a butcher knife from a player.
            if (Configuration.Current.Tameable.ownerDamageOverride)
            {
                Character attacker = hit.GetAttacker();
                // Attacker is player
                if (attacker == __instance.GetComponent<Tameable>().GetPlayer(attacker.GetZDOID()))
                    return false;
            }

            return true;
        }



    }

    /// <summary>
    /// Allow tweaking of fall damage
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.UpdateGroundContact))]
    public static class Character_UpdateGroundContact_Transpiler
    {
        private static readonly MethodInfo method_calculateFallDamage = AccessTools.Method(typeof(Character_UpdateGroundContact_Transpiler), nameof(calculateFallDamage));

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Configuration.Current.Player.IsEnabled)
                return instructions;

            List<CodeInstruction> il = instructions.ToList();

            bool found = false;

            for (int i = 0; i < il.Count; i++)
            {
                if (!found && il[i].opcode != OpCodes.Newobj)
                {
                    continue;
                }
                else if (il[i].opcode == OpCodes.Newobj)
                {
                    found = true;
                    continue;
                }
                if (il[i].opcode == OpCodes.Ldloc_2)
                {
                    il[i].opcode = OpCodes.Ldloc_0;
                    il.Insert(i + 1, new CodeInstruction(OpCodes.Call, method_calculateFallDamage));
                }
                else continue;

                return il.AsEnumerable();
            }

            PatchLog.Failed(nameof(Character_UpdateGroundContact_Transpiler), "Fall damage will be unchanged.");
            return instructions;
        }

        private static float calculateFallDamage(float fallDistance)
        {
            if (fallDistance < 4f)
                return 0f;

            float linearFallDamage = ((fallDistance - 4f) / 16f) * 100f;
            float scaledFallDamage = Helper.applyModifierValue(linearFallDamage, Configuration.Current.Player.fallDamageScalePercent);
            float fallDamage = Math.Min(scaledFallDamage, Configuration.Current.Player.maxFallDamage);

            return fallDamage;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
    public static class Character_GetHoverText_Patch
    {
        [UsedImplicitly]
        public static void Postfix(Character __instance, ref string __result)
        {
            var growup = __instance.GetComponent<Growup>();
            if (growup) ProcreationHelpers.AddGrowupInformation(__instance, growup, ref __result);
        }
    }
    /// <summary>
    /// Honorable combat: a killing blow from another player is reduced so it cannot kill, and the
    /// player who would have died drops out of PvP.
    ///
    /// Nothing has to be saved and restored, because death never happens. Character.CheckDeath is
    /// simply "if (GetHealth() > 0) return; ... OnDeath()", so a blow that never reaches zero
    /// produces no tombstone, no dropped gear and no skill loss, and leaves food and buffs running.
    ///
    /// A PREFIX that shrinks the incoming hit, rather than a postfix that heals afterwards the way
    /// the tamed-creature "essential" mortality above does. A postfix runs after SetHealth and after
    /// the death check, so by then OnDeath has already fired; healing back up would leave a corpse
    /// and a tombstone behind. Catching it beforehand is the only way to keep the gear.
    ///
    /// Runs on the victim's own client. Damage is applied by the owner of the character being hit -
    /// the attacker's client sends RPC_Damage to it - so this is each player deciding not to die,
    /// which is also why it only means anything among people who trust each other.
    ///
    /// Switching the loser's PvP off is enough to end the fight: Player.SetPVP writes ZDOVars.s_pvp
    /// as well as the local field, so the flag reaches the other client, and Character.RPC_Damage
    /// refuses player damage against a character that is not PvP enabled.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    public static class Character_ApplyDamage_HonorableCombat_Patch
    {
        [UsedImplicitly]
        public static void Prefix(Character __instance, HitData hit)
        {
            var config = Configuration.Current.HonorableCombat;
            if (!config.IsEnabled || hit == null) return;

            // Only ever our own character: whoever owns a character decides its health.
            if (!(__instance is Player victim)) return;
            if (!Player.m_localPlayerExists || victim != Player.m_localPlayer) return;
            if (!victim.IsPVPEnabled()) return;

            // A creature, a fall, drowning, or a hit with no attributable attacker still kills.
            Character attacker = hit.GetAttacker();
            if (attacker == null || !attacker.IsPlayer() || attacker == __instance) return;
            if (!attacker.IsPVPEnabled()) return;

            float floor = Mathf.Max(1f, config.minimumHealth);
            float health = victim.GetHealth();
            float damage = hit.GetTotalDamage();

            // Survivable as it stands: leave it completely alone, damage numbers included.
            if (health - damage > floor) return;

            // Scale the blow to leave exactly the floor, rather than assigning a single damage
            // value, so it stays spread across its damage types and resistances still read right.
            float allowed = Mathf.Max(0f, health - floor);
            hit.ApplyModifier(damage > 0f ? allowed / damage : 0f);

            // Ends the fight. This reaches the attacker's client through the ZDO, and RPC_Damage
            // rejects player damage against a character whose PvP is off.
            victim.SetPVP(false);
        }
    }
}
