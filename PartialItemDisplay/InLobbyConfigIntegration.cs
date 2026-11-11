using BepInEx.Bootstrap;
using InLobbyConfig;
using InLobbyConfig.Fields;
using System;
using System.Runtime.CompilerServices;

namespace PartialItemDisplay
{
    public static class InLobbyConfigIntegration
    {
        public const string GUID = "com.KingEnderBrine.InLobbyConfig";
        private static bool Enabled => Chainloader.PluginInfos.ContainsKey(GUID);
        private static object ModConfig { get; set; }

        public static bool NeedsSave { get; set; }
        
        public static void OnStart()
        {
            if (Enabled)
            {
                OnStartInternal();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnStartInternal()
        {
            PartialItemDisplayPlugin.Enabled.SettingChanged += (s, e) => { NeedsSave = true; };
            var modConfig = new ModConfigEntry
            {
                DisplayName = "Partial Item Display",
                EnableField = ConfigFieldUtilities.CreateFromBepInExConfigEntry(PartialItemDisplayPlugin.Enabled) as BooleanConfigField,
            };
            PartialItemDisplayPlugin.DefaultSection.ApplyToInLobbyConfig(modConfig);
            foreach (var characterSection in PartialItemDisplayPlugin.CharacterSections.Values)
            {
                characterSection.ApplyToInLobbyConfig(modConfig);
            }

            ModConfigCatalog.Add(modConfig);
            ModConfig = modConfig;

            ConfigFieldUtilities.OnConfigWindowOpen += SaveConfig;
            ConfigFieldUtilities.OnConfigWindowClosed += SaveConfig;
        }

        private static void SaveConfig()
        {
            if (NeedsSave)
            {
                PartialItemDisplayPlugin.Instance.Config.Save();
                NeedsSave = false;
            }
        }

        public static void OnDestroy()
        {
            if (Enabled)
            {
                OnDestroyInternal();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnDestroyInternal()
        {
            ModConfigCatalog.Remove(ModConfig as ModConfigEntry);
        }
    }
}
