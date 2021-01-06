using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace PartialItemDisplay
{
    public class ItemDisplayConfigSection
    {
        public string SectionName { get; private set; }
        public ConfigEntry<bool> SectionEnabled { get; private set; }
        public ConfigEntry<ListType> ItemListType { get; private set; }
        public ConfigEntry<string> ItemList { get; private set; }
        public ConfigEntry<ListType> EquipmentListType { get; private set; }
        public ConfigEntry<string> EquipmentList { get; private set; }
        public List<string> Items { get; } = new List<string>();
        public List<string> Equipments { get; } = new List<string>();

        public ItemDisplayConfigSection(ConfigFile file, string sectionName, bool isEnabledByDefault = false)
        {
            SectionName = RemoveInvalidCharacters(sectionName);
            SectionEnabled = file.Bind(SectionName, nameof(SectionEnabled), isEnabledByDefault, "Should rules in this section be applied");
            ItemListType = file.Bind(SectionName, nameof(ItemListType), ListType.Blacklist, "Blacklist - show everything except selected items. Whitelist - show only selected items");
            EquipmentListType = file.Bind(SectionName, nameof(EquipmentListType), ListType.Blacklist, "Blacklist - show everything except selected items. Whitelist - show only selected items");
            ItemList = file.Bind(SectionName, nameof(ItemList), "", "Selected items for this section");
            EquipmentList = file.Bind(SectionName, nameof(EquipmentList), "", "Selected equipment for this section");

            try
            {
                Items.Clear();
                Items.AddRange(ItemList.Value
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(el => el.Trim())
                    .Distinct());
            }
            catch (Exception e)
            {
                PartialItemDisplayPlugin.InstanceLogger.LogWarning("Failed to parse `ItemList` config");
                PartialItemDisplayPlugin.InstanceLogger.LogError(e);
            }

            try
            {
                Equipments.Clear();
                Equipments.AddRange(EquipmentList.Value
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(el => el.Trim())
                    .Distinct());
            }
            catch (Exception e)
            {
                PartialItemDisplayPlugin.InstanceLogger.LogWarning("Failed to parse `EquipmentList` config");
                PartialItemDisplayPlugin.InstanceLogger.LogError(e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void ApplyToInLobbyConfig(InLobbyConfig.ModConfigEntry configEntry)
        {
            var fields = new List<InLobbyConfig.Fields.IConfigField>
            {
                InLobbyConfig.Fields.ConfigFieldUtilities.CreateFromBepInExConfigEntry(ItemListType),
                new InLobbyConfig.Fields.SelectListField<string>(ItemList.Definition.Key, ItemList.Description.Description, GetItemList, ItemListItemAdded, ItemListItemRemoved, GetItemOptions),
                InLobbyConfig.Fields.ConfigFieldUtilities.CreateFromBepInExConfigEntry(EquipmentListType),
                new InLobbyConfig.Fields.SelectListField<string>(EquipmentList.Definition.Key, EquipmentList.Description.Description, GetEquipmentList, EquipmentListItemAdded, EquipmentListItemRemoved, GetEquipmentOptions)
            };
            configEntry.SectionFields[SectionName] = fields;
            configEntry.SectionEnableFields[SectionName] = InLobbyConfig.Fields.ConfigFieldUtilities.CreateFromBepInExConfigEntry(SectionEnabled) as InLobbyConfig.Fields.BooleanConfigField;
        }

        private List<string> GetItemList()
        {
            return Items;
        }

        private void ItemListItemAdded(string value, int index)
        {
            Items.Insert(index, value);
            ItemList.Value = string.Join(", ", Items);
        }

        private void ItemListItemRemoved(int index)
        {
            Items.RemoveAt(index);
            ItemList.Value = string.Join(", ", Items);
        }

        private Dictionary<string, string> GetItemOptions()
        {
            return ItemCatalog.itemDefs.Where(el => !el.hidden && el.inDroppableTier).ToDictionary(el => el.name, el => Language.GetString(el.nameToken));
        }

        private List<string> GetEquipmentList()
        {
            return Equipments;
        }

        private void EquipmentListItemAdded(string value, int index)
        {
            Equipments.Insert(index, value);
            EquipmentList.Value = string.Join(", ", Equipments);
        }

        private void EquipmentListItemRemoved(int index)
        {
            Equipments.RemoveAt(index);
            EquipmentList.Value = string.Join(", ", Equipments);
        }

        private Dictionary<string, string> GetEquipmentOptions()
        {
            return EquipmentCatalog.equipmentDefs.Where(el => el.canDrop).ToDictionary(el => el.name, el => Language.GetString(el.nameToken));
        }

        private string RemoveInvalidCharacters(string sectionName)
        {
            return Regex.Replace(sectionName, @"[=\n\t""'\\[\]]", "");
        }
    }
}
