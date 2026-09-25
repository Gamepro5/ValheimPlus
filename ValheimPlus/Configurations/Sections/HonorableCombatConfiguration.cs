using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    /// <summary>
    /// Duelling without consequences: a killing blow from another player leaves you alive at a
    /// sliver of health and switches your PvP off, instead of killing you.
    /// </summary>
    public class HonorableCombatConfiguration : BaseConfig
    {
        private const string Section = "HonorableCombat";

        private ConfigEntry<float> minimumHealthEntry;
        private ConfigEntry<bool> clearDamageOverTimeEntry;

        public float minimumHealth => minimumHealthEntry.Value;
        public bool clearDamageOverTime => clearDamageOverTimeEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.\nA blow from another player that would kill you instead leaves you alive and switches your PvP off. Nothing is lost, because you never actually die: no tombstone, no dropped gear, no skill loss, and your food and buffs keep running.\nOnly applies between two players who both have PvP on. A creature, a fall or a drowning still kills you normally.");
            minimumHealthEntry = Bind(config, Section, "minimumHealth", 1f, 1f, 100f,
                "Health you are left with after yielding. The killing blow is reduced to leave exactly this much, so you have to eat or rest to recover.");
            clearDamageOverTimeEntry = Bind(config, Section, "clearDamageOverTime", true,
                "Clear burning and poison when you yield.\nWithout this, being saved on a sliver of health only to burn to death a second later loses the gear anyway. Only damaging effects are removed - food, rested and other buffs are left alone.");
        }
    }
}
