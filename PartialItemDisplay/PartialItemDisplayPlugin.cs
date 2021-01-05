using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace PartialItemDisplay
{
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    [BepInDependency("com.KingEnderBrine.InLobbyConfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.KingEnderBrine.PartialItemDisplay", "Partial Item Display", "1.0.2")]
    public class PartialItemDisplayPlugin : BaseUnityPlugin
    {
        internal static PartialItemDisplayPlugin Instance { get; private set; }
        internal static ManualLogSource InstanceLogger { get => Instance?.Logger; }

        private static ConfigEntry<bool> Enabled { get; set; }
        private static ItemDisplayConfigSection DefaultSection { get; set; }
        private static Dictionary<int, ItemDisplayConfigSection> CharacterSections { get; set; }

        private void Awake()
        {
            Instance = this;

            IL.RoR2.CharacterModel.UpdateItemDisplay += UpdateItemDisplayIL;
            IL.RoR2.CharacterModel.SetEquipmentDisplay += SetEquipmentDisplayIL;
        }

        private void Start()
        {
            SetupConfig();
        }

        private void Destroy()
        {
            Instance = null;

            IL.RoR2.CharacterModel.UpdateItemDisplay -= UpdateItemDisplayIL;
            IL.RoR2.CharacterModel.SetEquipmentDisplay -= SetEquipmentDisplayIL;
        }

        private static void UpdateItemDisplayIL(ILContext il)
        {
            var c = new ILCursor(il);

            ILLabel elseLabel = null;
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(1),
                x => x.MatchLdloc(0),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchLdcI4(0),
                x => x.MatchBle(out elseLabel)
                );

            c.Emit(OpCodes.Ldloc_0);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(CharacterModel).GetField("body"));
            c.Emit(OpCodes.Call, typeof(PartialItemDisplayPlugin).GetMethod(nameof(IgnoreItemDisplay), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
            c.Emit(OpCodes.Brtrue, elseLabel);
        }

        private static void SetEquipmentDisplayIL(ILContext il)
        {
            var c = new ILCursor(il);

            ILLabel retLabel = null;

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(out _),
                x => x.MatchLdfld(out _),
                x => x.MatchCallOrCallvirt(out _),
                x => x.MatchBrfalse(out retLabel)
                );

            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(CharacterModel).GetField("body"));
            c.Emit(OpCodes.Call, typeof(PartialItemDisplayPlugin).GetMethod(nameof(IgnoreEquipmentDisplay), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
            c.Emit(OpCodes.Brtrue, retLabel);
        }

        private static bool IgnoreEquipmentDisplay(EquipmentIndex index, CharacterBody body)
        {
            if (!Enabled.Value || !body)
            {
                return false;
            }
            CharacterSections.TryGetValue(body.bodyIndex, out var characterSection);
            if (characterSection != null && characterSection.SectionEnabled.Value)
            {
                return ProcessEquipmentDisplay(index, characterSection);
            }
            if (DefaultSection.SectionEnabled.Value)
            {
                return ProcessEquipmentDisplay(index, DefaultSection);
            }
            return false;
        }

        private static bool IgnoreItemDisplay(ItemIndex itemIndex, CharacterBody body)
        {
            if (!Enabled.Value || !body)
            {
                return false;
            }
            CharacterSections.TryGetValue(body.bodyIndex, out var characterSection);
            if (characterSection != null && characterSection.SectionEnabled.Value)
            {
                return ProcessItemDisplay(itemIndex, characterSection);
            }
            if (DefaultSection.SectionEnabled.Value)
            {
                return ProcessItemDisplay(itemIndex, DefaultSection);
            }
            return false;
        }

        private static bool ProcessEquipmentDisplay(EquipmentIndex index, ItemDisplayConfigSection section)
        {
            var equipmentDef = EquipmentCatalog.GetEquipmentDef(index);
            if (equipmentDef == null)
            {
                return false;
            }

            switch (section.EquipmentListType.Value)
            {
                case ListType.Blacklist:
                    return section.Equipments.Contains(equipmentDef.name);
                case ListType.Whitelist:
                    return !section.Equipments.Contains(equipmentDef.name);
            }

            return false;
        }

        private static bool ProcessItemDisplay(ItemIndex index, ItemDisplayConfigSection section)
        {
            var itemDef = ItemCatalog.GetItemDef(index);
            if (itemDef == null)
            {
                return false;
            }
            switch (section.ItemListType.Value)
            {
                case ListType.Blacklist:
                    return section.Items.Contains(itemDef.name);
                case ListType.Whitelist:
                    return !section.Items.Contains(itemDef.name);
            }

            return false;
        }

        private void SetupConfig()
        {
            Enabled = Config.Bind("Main", "Enabled", true, "Is this mod enabled");
            DefaultSection = new ItemDisplayConfigSection(Config, "Default", true);
            CharacterSections = SurvivorCatalog
                .allSurvivorDefs
                .ToDictionary(
                    def => SurvivorCatalog.GetBodyIndexFromSurvivorIndex(def.survivorIndex),
                    def => new ItemDisplayConfigSection(
                        Config,
                        Language.english.GetLocalizedStringByToken(def.displayNameToken)));

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.InLobbyConfig"))
            {
                SetupInLobbyConfig();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void SetupInLobbyConfig()
        {
            var configEntry = new InLobbyConfig.ModConfigEntry
            {
                DisplayName = "Partial Item Display",
                EnableField = InLobbyConfig.Fields.ConfigFieldUtilities.CreateFromBepInExConfigEntry(Enabled) as InLobbyConfig.Fields.BooleanConfigField,
            };
            DefaultSection.ApplyToInLobbyConfig(configEntry);
            foreach (var characterSection in CharacterSections.Values)
            {
                characterSection.ApplyToInLobbyConfig(configEntry);
            }
            InLobbyConfig.ModConfigCatalog.Add(configEntry);
        }
    }
}