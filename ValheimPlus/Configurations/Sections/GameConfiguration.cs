using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class GameConfiguration : BaseConfig
    {
        private const string Section = "Game";

        private ConfigEntry<float> gameDifficultyDamageScaleEntry;
        private ConfigEntry<float> gameDifficultyHealthScaleEntry;
        private ConfigEntry<int> extraPlayerCountNearbyEntry;
        private ConfigEntry<int> setFixedPlayerCountToEntry;
        private ConfigEntry<int> difficultyScaleRangeEntry;
        private ConfigEntry<bool> disablePortalsEntry;
        private ConfigEntry<bool> disableConsoleEntry;
        private ConfigEntry<bool> bigPortalNamesEntry;
        private ConfigEntry<bool> disableFogEntry;

        public float gameDifficultyDamageScale => gameDifficultyDamageScaleEntry.Value;
        public float gameDifficultyHealthScale => gameDifficultyHealthScaleEntry.Value;
        public int extraPlayerCountNearby => extraPlayerCountNearbyEntry.Value;
        public int setFixedPlayerCountTo => setFixedPlayerCountToEntry.Value;
        public int difficultyScaleRange => difficultyScaleRangeEntry.Value;
        public bool disablePortals => disablePortalsEntry.Value;
        public bool disableConsole => disableConsoleEntry.Value;
        public bool bigPortalNames => bigPortalNamesEntry.Value;
        public bool disableFog => disableFogEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            gameDifficultyDamageScaleEntry = Bind(config, Section, "gameDifficultyDamageScale", 4f,
                "The games damage multiplier per person nearby in difficultyScaleRange(m) radius.\nDefault is 4% monster damage increase per player in radius.");
            gameDifficultyHealthScaleEntry = Bind(config, Section, "gameDifficultyHealthScale", 30f,
                "The games health multiplier per person nearby in difficultyScaleRange(m) radius.\nDefault is 30% monster health increase per player in radius.");
            extraPlayerCountNearbyEntry = Bind(config, Section, "extraPlayerCountNearby", 0,
                "Adds additional players to the difficulty calculation in multiplayer unrelated to the actual amount.\nThis option is disabled if its set to 0.");
            setFixedPlayerCountToEntry = Bind(config, Section, "setFixedPlayerCountTo", 0,
                "Sets the nearby player count always to this value + extraPlayerCountNearby.\nThis option is disabled if its set to 0.");
            difficultyScaleRangeEntry = Bind(config, Section, "difficultyScaleRange", 100, 1, 20000,
                "The range in meters at which other players count towards nearby players for the difficulty scale.");
            disablePortalsEntry = Bind(config, Section, "disablePortals", false,
                "If you set this to true, all portals will be disabled.");
            disableConsoleEntry = Bind(config, Section, "disableConsole", false,
                "If you set this to true the console will be force disabled in-game.");
            bigPortalNamesEntry = Bind(config, Section, "bigPortalNames", false,
                "If you set this to true, portal names will be displayed in big text in center of screen.");
            disableFogEntry = Bind(config, Section, "disableFog", false,
                "Remove dense fog from the game.");
        }
    }
}
