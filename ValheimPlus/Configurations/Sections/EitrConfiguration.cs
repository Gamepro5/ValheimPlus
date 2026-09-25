using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    /// <summary>
    /// Eitr regeneration, mirroring what [Stamina] does for stamina.
    ///
    /// A separate section from [EitrUsage] on purpose, following the existing split: [Stamina]
    /// holds regeneration and drains while [StaminaUsage] holds the per-weapon costs, so eitr
    /// regeneration belongs beside the former rather than inside a section named for usage.
    /// </summary>
    public class EitrConfiguration : BaseConfig
    {
        private const string Section = "Eitr";

        private ConfigEntry<float> eitrRegenEntry;
        private ConfigEntry<float> eitrRegenDelayEntry;

        public float eitrRegen => eitrRegenEntry.Value;
        public float eitrRegenDelay => eitrRegenDelayEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section. This section contains modifiers. Modifiers are increases and reduction in percent declared by 50, or -50.");
            eitrRegenEntry = Bind(config, Section, "eitrRegen", 0f,
                "Changes how fast eitr regenerates by %.\nThe value 100 makes it regenerate twice as fast, -50 makes it half as fast.");
            eitrRegenDelayEntry = Bind(config, Section, "eitrRegenDelay", 0f,
                "Changes the delay before eitr starts regenerating after being spent, by %.\nThe value -50 halves the wait.");
        }
    }
}
