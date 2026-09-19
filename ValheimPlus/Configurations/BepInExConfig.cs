using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ValheimPlus.Configurations.Sections;

namespace ValheimPlus.Configurations
{
    /// <summary>
    /// Builds <see cref="Configuration.Current"/> out of BepInEx config entries, carrying over the
    /// values of an old valheim_plus.cfg where there is one. See <see cref="LegacyMode"/>.
    /// </summary>
    public static class BepInExConfig
    {
        /// <summary>Suffix given to the legacy ini once its values have been imported.</summary>
        private const string RetiredSuffix = ".migrated";

        internal const string EquipmentAndQuickSlotsGuid = "randyknapp.mods.equipmentandquickslots";
        internal const string ExtraSlotsGuid = "shudnal.ExtraSlots";

        /// <summary>The file every section's entries are bound to.</summary>
        public static ConfigFile Config { get; private set; }

        /// <summary>Every bound section, as declared on <see cref="Configuration"/>.</summary>
        private static readonly List<BaseConfig> Sections = new();

        /// <summary>Each setting's value when patches were last built. Empty at boot, so defaults stand in.</summary>
        private static readonly Dictionary<ConfigDefinition, object> AppliedValues = new();

        private static bool serverSyncRegistered;

        /// <summary>Whether a config package has already been taken this connection.</summary>
        private static bool syncedOnce;

        /// <summary>Whether a rebuild is already queued for the next frame.</summary>
        private static bool reapplyQueued;

        /// <summary>What a legacy valheim_plus.cfg on disk is being used for.</summary>
        private enum LegacyMode
        {
            /// <summary>No legacy file, or it could not be read.</summary>
            None,

            /// <summary>First run after upgrading: import it once, then set it aside.</summary>
            Migrate,

            /// <summary>Put back after a migration: it wins on every launch and is left in place.</summary>
            Override,
        }

        public static void Load(ConfigFile config)
        {
            Config = config;

            // Has to be decided before anything is bound, since BepInEx creates its file lazily.
            var mode = DetectLegacyMode(config);
            var legacy = mode == LegacyMode.None ? null : ReadLegacyIni(mode);
            if (legacy == null) mode = LegacyMode.None;

            // An import touches hundreds of entries, and each set would otherwise rewrite the whole file.
            config.SaveOnConfigSet = false;
            try
            {
                Configuration.Current = BindSections(config);
                if (legacy != null) ImportLegacyValues(config, legacy);
                config.Save();
            }
            finally
            {
                config.SaveOnConfigSet = true;
            }

            LogChangedSettings("differ from their default");

            if (mode == LegacyMode.Migrate) RetireLegacyIni(config);
            else if (mode == LegacyMode.Override) WarnLegacyOverride(config);

            SetUpServerSync();
            ConfigurationManagerWatcher.Install(config, Sections, mode == LegacyMode.Override);
        }

        /// <summary>
        /// Creates the ConfigSync at load, so the version check it owns is in place before anyone
        /// connects. Entries are registered later, see <see cref="RegisterForServerSync"/>.
        /// </summary>
        private static void SetUpServerSync()
        {
            ConfigSyncGlue.Initialize(ValheimPlusPlugin.ValheimPlusGuid, "Valheim Plus",
                ValheimPlusPlugin.NumericVersion, ValheimPlusPlugin.MinRequiredNumericVersion);

            // Only the return to our own values. The other direction is handled in ConfigApplied.
            ConfigSyncGlue.SourceOfTruthChanged += isSourceOfTruth =>
            {
                if (!isSourceOfTruth) return;

                Config.Save();
                ReapplyPatches("Config source changed");
            };

            // Rebuilding patches mid-world would swap code out from under a running game, so only
            // the first package of a connection is taken.
            ConfigSyncGlue.ConfigApplied += () =>
            {
                if (syncedOnce)
                {
                    ValheimPlusPlugin.Logger.LogInfo(
                        "Config arrived while in a world, so patches were left alone.");
                    return;
                }

                syncedOnce = true;
                ReapplyPatches("Received config from the server");
            };

            // Decides whether non-admins may change synced settings while connected.
            ConfigSyncGlue.RegisterLocking((ConfigEntry<bool>)Config[
                nameof(Configuration.Server), nameof(ServerConfiguration.serverSyncsConfig)]);
        }

        /// <summary>
        /// Rebuilds the patches from the current config values, on the next frame. Every caller
        /// reaches this from inside a patched method, and unpatching rewrites methods in place, so
        /// running it here would swap the code out from under a live stack frame. That survives on
        /// x64 but hangs the game on arm64, where a detour is several instructions and the
        /// instruction cache is not coherent with the write.
        /// </summary>
        internal static void ReapplyPatches(string reason)
        {
            if (reapplyQueued) return;
            reapplyQueued = true;

            ThreadingHelper.Instance.StartSyncInvoke(() =>
            {
                reapplyQueued = false;

                ValheimPlusPlugin.Logger.LogInfo($"{reason}, re-applying patches.");
                ResolveModConflicts();
                LogChangedSettings("changed since patches were last applied");
                ValheimPlusPlugin.UnpatchSelf();
                ValheimPlusPlugin.PatchAll();

                // That was the change, so a later window close need not repeat the work.
                ConfigurationManagerWatcher.MarkClean();
            });
        }

        /// <summary>
        /// Hands every section to ServerSync. Called from ZNet.Awake, the first point where
        /// ZNet.m_isServer is known and still before any peer logs in.
        /// </summary>
        public static void RegisterForServerSync()
        {
            // A server's patches already match its own config, so a push must not rebuild them.
            // Assigned before the guard below, since only this runs on every world entered.
            syncedOnce = ZNet.m_isServer;

            if (serverSyncRegistered) return;

            if (ZNet.m_isServer && !Configuration.Current.Server.serverSyncsConfig)
            {
                ValheimPlusPlugin.Logger.LogWarning(
                    "serverSyncsConfig is off, so this server will not push its config to clients.");
                return;
            }

            serverSyncRegistered = true;

            var registered = 0;

            foreach (var section in Sections) registered += section.RegisterForServerSync();

            ValheimPlusPlugin.Logger.LogInfo($"Registered {registered} settings for server sync.");
        }

        /// <summary>
        /// Decides what a legacy config file on disk means. A BepInEx config file already beside it
        /// means an earlier run migrated us, so the old file was put back deliberately.
        /// </summary>
        private static LegacyMode DetectLegacyMode(ConfigFile config)
        {
            if (!File.Exists(ConfigurationExtra.ConfigIniPath)) return LegacyMode.None;
            return File.Exists(config.ConfigFilePath) ? LegacyMode.Override : LegacyMode.Migrate;
        }

        /// <summary>Reads the legacy config, or returns null when it cannot be read.</summary>
        private static IniData ReadLegacyIni(LegacyMode mode)
        {
            try
            {
                if (mode == LegacyMode.Migrate)
                {
                    ValheimPlusPlugin.Logger.LogInfo(
                        $"Found a legacy config at '{ConfigurationExtra.ConfigIniPath}', importing its values.");
                }

                return new FileIniDataParser().ReadFile(ConfigurationExtra.ConfigIniPath);
            }
            catch (Exception e)
            {
                ValheimPlusPlugin.Logger.LogError(
                    $"Could not read the legacy config, so defaults will be used instead: {e}");
                return null;
            }
        }

        /// <summary>Logs the settings changed since patches were last built, to help read a user's log.</summary>
        private static void LogChangedSettings(string description)
        {
            var changed = new List<string>();
            foreach (var definition in Config.Keys)
            {
                var entry = Config[definition];
                var previous = AppliedValues.TryGetValue(definition, out var value) ? value : entry.DefaultValue;
                AppliedValues[definition] = entry.BoxedValue;

                if (!Equals(entry.BoxedValue, previous))
                    changed.Add($"  [{definition.Section}] {definition.Key}: {previous} -> {entry.BoxedValue}");
            }

            ValheimPlusPlugin.Logger.LogInfo($"{changed.Count} settings {description}:");
            foreach (var line in changed) ValheimPlusPlugin.Logger.LogInfo(line);
        }

        /// <summary>Resets settings known to break alongside another installed mod.</summary>
        internal static void ResolveModConflicts()
        {
            var configuredRows = Configuration.Current.Inventory.playerInventoryRows;
            if (Configuration.Current.Inventory.IsEnabled && configuredRows > 4)
            {
                if (Chainloader.PluginInfos.ContainsKey(EquipmentAndQuickSlotsGuid))
                {
                    ResolveModConflict(
                        $"[Inventory] playerInventoryRows is {configuredRows}, " +
                        "which conflicts with Equipment and Quick Slots. Use that mod's " +
                        "\"Extra Inventory Rows\" setting instead.",
                        "4",
                        () => Configuration.Current.Inventory.playerInventoryRows = 4);
                }

                if (Chainloader.PluginInfos.ContainsKey(ExtraSlotsGuid))
                {
                    ResolveModConflict(
                        $"[Inventory] playerInventoryRows is {configuredRows}, " +
                        "which conflicts with Extra Slots. Use that mod's " +
                        "\"Extra slots\" setting instead.",
                        "4",
                        () => Configuration.Current.Inventory.playerInventoryRows = 4);
                }
            }
        }

        /// <summary>Resets one conflicting setting with a warning, unless a server's values are in effect.</summary>
        private static void ResolveModConflict(string problem, string resetValue, Action reset)
        {
            // A server's value is its own to change, and writing it here would be reverted or pushed back up.
            if (ConfigSyncGlue.IsSourceOfTruth)
            {
                ValheimPlusPlugin.Logger.LogWarning(
                    $"{problem} It has been reset to {resetValue}. " +
                    $"Setting it to {resetValue} in your own config makes this warning go away.");
                reset();
            }
            else
            {
                ValheimPlusPlugin.Logger.LogDebug($"{problem} The server set this value, so it was left alone.");
            }
        }

        private static Configuration BindSections(ConfigFile config)
        {
            var configuration = new Configuration();
            Sections.Clear();

            foreach (var property in typeof(Configuration).GetProperties())
            {
                if (!typeof(BaseConfig).IsAssignableFrom(property.PropertyType)) continue;

                var section = (BaseConfig)Activator.CreateInstance(property.PropertyType);
                section.Bind(config);
                property.SetValue(configuration, section, null);
                Sections.Add(section);
            }

            return configuration;
        }

        /// <summary>Copies values out of the legacy ini into the entries just bound.</summary>
        private static void ImportLegacyValues(ConfigFile config, IniData legacy)
        {
            var imported = 0;
            var failed = new List<string>();

            foreach (var definition in config.Keys.ToList())
            {
                var section = legacy[definition.Section];
                if (section == null || !section.ContainsKey(definition.Key)) continue;

                var entry = config[definition];
                var value = ConvertIniValue(section, definition.Key, entry.SettingType, entry.DefaultValue);
                if (value == null)
                {
                    failed.Add($"{definition.Section}.{definition.Key}");
                    continue;
                }

                entry.BoxedValue = value;
                imported++;
            }

            ValheimPlusPlugin.Logger.LogInfo($"Imported {imported} settings from the legacy config.");
            if (failed.Count > 0)
            {
                ValheimPlusPlugin.Logger.LogWarning(
                    $"These settings kept their default because their type is not understood: " +
                    string.Join(", ", failed.ToArray()));
            }
        }

        /// <summary>
        /// Parses one ini value with the old readers, so tolerant spellings like "yes" still work.
        /// Returns null for a type we have no reader for.
        /// </summary>
        private static object ConvertIniValue(KeyDataCollection data, string key, Type type, object fallback)
        {
            if (type == typeof(bool)) return data.GetBool(key);
            if (type == typeof(int)) return data.GetInt(key, (int)fallback);
            if (type == typeof(float)) return data.GetFloat(key, (float)fallback);
            if (type == typeof(KeyCode)) return data.GetKeyCode(key, (KeyCode)fallback);
            if (type == typeof(string)) return data[key];

            if (type.IsEnum)
            {
                return type.IsDefined(typeof(FlagsAttribute), false)
                    ? data.GetFlags(key, fallback)
                    : data.GetEnumValue(key, fallback);
            }

            return null;
        }

        /// <summary>Says the legacy config is in charge and how to stop using it.</summary>
        private static void WarnLegacyOverride(ConfigFile config)
        {
            ValheimPlusPlugin.Logger.LogWarning(
                $"'{ConfigurationExtra.ConfigIniPath}' is present, so its values are overriding your " +
                "settings. This file is deprecated: a future release will read it only when " +
                "'useLegacyConfigFile' is enabled, and a later release will stop reading it entirely. " +
                $"The result has been written to '{config.ConfigFilePath}' - switch to generating that " +
                "file and delete the old one. Settings cannot be edited in-game while it exists.");
        }

        /// <summary>
        /// Renames the imported legacy config out of the way. Never deletes it, and never overwrites
        /// an existing backup, so a failure here just leaves an extra file.
        /// </summary>
        private static void RetireLegacyIni(ConfigFile config)
        {
            var retired = ConfigurationExtra.ConfigIniPath + RetiredSuffix;

            try
            {
                File.Move(ConfigurationExtra.ConfigIniPath, retired);
                ValheimPlusPlugin.Logger.LogWarning(
                    $"Settings now live in '{config.ConfigFilePath}'. " +
                    $"The old config was kept as '{retired}' and is no longer read.");
            }
            catch (Exception e)
            {
                ValheimPlusPlugin.Logger.LogWarning(
                    $"Settings now live in '{config.ConfigFilePath}', but '{ConfigurationExtra.ConfigIniPath}' " +
                    $"could not be renamed and is now unused: {e.Message}");
            }
        }
    }
}
