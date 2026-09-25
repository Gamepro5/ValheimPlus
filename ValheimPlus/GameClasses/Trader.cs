using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    public static class TraderExtensions
    {
        public static bool SellsItem(this Trader trader, string itemName)
            => trader.m_items.Any(item => item.m_prefab.name == itemName);

        /// <summary>
        /// A trade item by its localisation token OR its prefab name, or null when the trader does
        /// not sell it.
        ///
        /// Both keys are accepted because the two lookups in this file disagreed: SellsItem matches
        /// m_prefab.name while this matched m_shared.m_name, so a caller passing the wrong one of
        /// the pair silently found nothing. Tokens begin with '$' and prefab names do not, so there
        /// is no ambiguity in accepting either.
        ///
        /// Null-safe throughout: a trade entry with no prefab or no item data must not take the
        /// whole lookup down.
        /// </summary>
        public static Trader.TradeItem GetTradeItem(this Trader trader, string itemName)
            => trader?.m_items?.FirstOrDefault(item =>
                   item?.m_prefab != null
                   && (item.m_prefab.m_itemData?.m_shared?.m_name == itemName
                       || item.m_prefab.name == itemName));
    }

    [HarmonyPatch(typeof(Trader), nameof(Trader.Start))]
    public static class Trader_Start_Patch
    {
        [UsedImplicitly]
        public static void Postfix(Trader __instance)
        {
            switch (__instance.m_name)
            {
                case "$npc_haldor":
                    AddHaldorItems(__instance);
                    break;
                case "$npc_hildir":
                    AddHildirItems(__instance);
                    break;
                case "$npc_bogwitch":
                    AddBogWitchItems(__instance);
                    break;
            }
        }

        private static void AddHaldorItems(Trader haldor)
        {
            if (!Configuration.Current.Egg.IsEnabled) return;

            var egg = haldor.GetTradeItem("$item_chicken_egg") ?? haldor.GetTradeItem("ChickenEgg");

            // Nothing to configure if he does not stock it. This used to dereference the result
            // straight away, and GetTradeItem is a FirstOrDefault: when the lookup missed, the
            // NullReferenceException came out of a Trader.Start POSTFIX, which is a miserable place
            // for one. Enabling the Egg section was enough to trigger it.
            if (egg == null)
            {
                ValheimPlusPlugin.Logger.LogWarning(
                    "[Egg] is enabled, but this trader does not stock the chicken egg, so " +
                    "soldByDefault and sellPrice have nothing to apply to. Skipping.");
                return;
            }

            egg.m_requiredGlobalKey = Configuration.Current.Egg.soldByDefault ? "" : "defeated_goblinking";
            egg.m_price = Configuration.Current.Egg.sellPrice;
        }

        private static void AddHildirItems(Trader hildir)
        {
        }

        private static void AddBogWitchItems(Trader bogWitch)
        {
        }
    }
}
