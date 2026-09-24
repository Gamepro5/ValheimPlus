using System;
using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class InventoryConfiguration : BaseConfig
    {
        private const string Section = "Inventory";

        private ConfigEntry<bool> inventoryFillTopToBottomEntry;
        private ConfigEntry<bool> mergeWithExistingStacksEntry;
        private ConfigEntry<int> playerInventoryRowsEntry;
        private ConfigEntry<int> extraPlayerInventoryRowsEntry;
        private ConfigEntry<int> woodChestColumnsEntry;
        private ConfigEntry<int> woodChestRowsEntry;
        private ConfigEntry<int> personalChestColumnsEntry;
        private ConfigEntry<int> personalChestRowsEntry;
        private ConfigEntry<int> ironChestColumnsEntry;
        private ConfigEntry<int> ironChestRowsEntry;
        private ConfigEntry<int> blackmetalChestColumnsEntry;
        private ConfigEntry<int> blackmetalChestRowsEntry;
        private ConfigEntry<int> cartInventoryColumnsEntry;
        private ConfigEntry<int> cartInventoryRowsEntry;
        private ConfigEntry<int> karveInventoryColumnsEntry;
        private ConfigEntry<int> karveInventoryRowsEntry;
        private ConfigEntry<int> longboatInventoryColumnsEntry;
        private ConfigEntry<int> longboatInventoryRowsEntry;

        public bool inventoryFillTopToBottom => inventoryFillTopToBottomEntry.Value;
        public bool mergeWithExistingStacks => mergeWithExistingStacksEntry.Value;
        /// <summary>
        /// Rows added on top of however many the game gives you, rather than a floor to raise
        /// them to. Needed because the game's own rows can be earned incrementally - the trader
        /// sells an increment, which `playerInventoryRows` as a floor silently absorbs until the
        /// purchases exceed it.
        /// </summary>
        public int extraPlayerInventoryRows
        {
            get => Math.Min(5, Math.Max(0, extraPlayerInventoryRowsEntry.Value));
            internal set => extraPlayerInventoryRowsEntry.Value = value;
        }

        /// <summary>True when this section should size the player inventory at all.</summary>
        public bool changesPlayerInventoryRows => playerInventoryRows > 4 || extraPlayerInventoryRows > 0;

        public int playerInventoryRows
        {
            get => Math.Min(9, Math.Max(4, playerInventoryRowsEntry.Value));
            internal set => playerInventoryRowsEntry.Value = value;
        }
        public int woodChestColumns => woodChestColumnsEntry.Value;
        public int woodChestRows => woodChestRowsEntry.Value;
        public int personalChestColumns => personalChestColumnsEntry.Value;
        public int personalChestRows => personalChestRowsEntry.Value;
        public int ironChestColumns => ironChestColumnsEntry.Value;
        public int ironChestRows => ironChestRowsEntry.Value;
        public int blackmetalChestColumns => blackmetalChestColumnsEntry.Value;
        public int blackmetalChestRows => blackmetalChestRowsEntry.Value;
        public int cartInventoryColumns => cartInventoryColumnsEntry.Value;
        public int cartInventoryRows => cartInventoryRowsEntry.Value;
        public int karveInventoryColumns => karveInventoryColumnsEntry.Value;
        public int karveInventoryRows => karveInventoryRowsEntry.Value;
        public int longboatInventoryColumns => longboatInventoryColumnsEntry.Value;
        public int longboatInventoryRows => longboatInventoryRowsEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            inventoryFillTopToBottomEntry = Bind(config, Section, "inventoryFillTopToBottom", false,
                "By default tools and weapons go into inventories top to bottom and other materials bottom to top.\nSet to true to make all items go into the inventory top to bottom.");
            mergeWithExistingStacksEntry = Bind(config, Section, "mergeWithExistingStacks", false,
                "By default items go to their original position when picking up your tombstone.\nSet to true to make all stacks try to merge with an existing stack first.");
            playerInventoryRowsEntry = Bind(config, Section, "playerInventoryRows", 4, 4, 9,
                "Minimum number of player inventory rows. Your inventory uses this value or the rows the game gives you, whichever is larger. Lowering it never takes away rows the game has added.");
            extraPlayerInventoryRowsEntry = Bind(config, Section, "extraPlayerInventoryRows", 0, 0, 5,
                "Rows ADDED to however many the game gives you, rather than a floor like playerInventoryRows.\nUse this when the game can grant rows itself: the trader sells an increment, and a floor absorbs those purchases invisibly until they exceed it, so buying appears to do nothing. Added on top, a purchase always shows up.\nThe total is still capped at 9 rows, which is the game's own limit.");
            woodChestColumnsEntry = Bind(config, Section, "woodChestColumns", 5, 3, 8,
                "Wood chest number of columns\n(default 5, 3 min, 8 max)");
            woodChestRowsEntry = Bind(config, Section, "woodChestRows", 2, 2, 10,
                "Wood chest number of rows (more than 4 rows will add a scrollbar).\n(default 2, min 2, 10 max)");
            personalChestColumnsEntry = Bind(config, Section, "personalChestColumns", 3, 3, 8,
                "Personal chest number of columns.\n(default 3, 3 min, 8 max)");
            personalChestRowsEntry = Bind(config, Section, "personalChestRows", 2, 2, 20,
                "Personal chest number of rows\n(default 2, 2 min, 20 max)");
            ironChestColumnsEntry = Bind(config, Section, "ironChestColumns", 6, 3, 8,
                "Iron chest number of columns.\n(default 6, min 3, max 8)");
            ironChestRowsEntry = Bind(config, Section, "ironChestRows", 4, 3, 20,
                "Iron chest number of rows (more than 4 rows will add a scrollbar)\n(default 4, min 3, max 20)");
            blackmetalChestColumnsEntry = Bind(config, Section, "blackmetalChestColumns", 8, 3, 8,
                "Blackmetal chests already have 8 columns by default but now you can lower it\n(default 8, min 3, max 8)");
            blackmetalChestRowsEntry = Bind(config, Section, "blackmetalChestRows", 4, 3, 20,
                "Blackmetal number of rows (more than 4 rows will add a scrollbar)\n(default 4, min 3, max 20)");
            cartInventoryColumnsEntry = Bind(config, Section, "cartInventoryColumns", 8, 6, 8,
                "Cart (Wagon) inventory number of columns\n(default 8, min 6, max 8)");
            cartInventoryRowsEntry = Bind(config, Section, "cartInventoryRows", 3, 3, 30,
                "Cart (Wagon) inventory number of rows (more than 4 rows will add a scrollbar)\n(default 3, min 3, max 30)");
            karveInventoryColumnsEntry = Bind(config, Section, "karveInventoryColumns", 2, 2, 8,
                "Karve (small boat) inventory number of columns\n(default 2, min 2, max 8)");
            karveInventoryRowsEntry = Bind(config, Section, "karveInventoryRows", 2, 2, 30,
                "Karve (small boat) inventory number of rows (more than 4 rows will add a scrollbar)\n(default 2, min 2, max 30)");
            longboatInventoryColumnsEntry = Bind(config, Section, "longboatInventoryColumns", 8, 6, 8,
                "Longboat (large boat) inventory number of columns\n(default 8, min 6, max 8)");
            longboatInventoryRowsEntry = Bind(config, Section, "longboatInventoryRows", 3, 3, 30,
                "Longboat (large boat) inventory number of rows (more than 4 rows will add a scrollbar)\n(default 3, min 3, max 30)");
        }
    }
}
