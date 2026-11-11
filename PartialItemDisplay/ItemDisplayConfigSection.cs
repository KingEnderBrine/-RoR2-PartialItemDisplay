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
        private const string SectionEnabledDesc = "Should rules in this section be applied";
        private const string ItemListTypeDesc = "Blacklist - show everything except selected items. Whitelist - show only selected items";
        private const string EquipmentListTypeDesc = "Blacklist - show everything except selected items. Whitelist - show only selected items";
        private const string KeyItemListTypeDesc = "Blacklist - show everything except selected items. Whitelist - show only selected items";
        private const string ItemListDesc = "Selected items for this section";
        private const string EquipmentListDesc = "Selected equipment for this section";
        private const string KeyItemListDesc = "Selected key items for this section";

        private readonly ConfigFile file;
        private readonly bool alwaysBound;
        private bool bound = false;

        private readonly ConfigDefinition sectionEnabledDef;
        private readonly ConfigDefinition itemListTypeDef;
        private readonly ConfigDefinition equipmentListTypeDef;
        private readonly ConfigDefinition keyItemListTypeDef;
        private readonly ConfigDefinition itemListDef;
        private readonly ConfigDefinition equipmentListDef;
        private readonly ConfigDefinition keyItemListDef;

        public string SectionName { get; private set; }
        private ConfigEntry<bool> SectionEnabledEntry { get; set; }
        public bool SectionEnabled { get; set; }
        
        private ConfigEntry<ListType> ItemListTypeEntry { get; set; }
        public ListType ItemListType { get; set; }
        private ConfigEntry<string> ItemListEntry { get; set; }
        public List<string> Items { get; } = new List<string>();

        private ConfigEntry<ListType> EquipmentListTypeEntry { get; set; }
        public ListType EquipmentListType { get; set; }
        private ConfigEntry<string> EquipmentListEntry { get; set; }
        public List<string> Equipments { get; } = new List<string>();

        private ConfigEntry<ListType> KeyItemListTypeEntry { get; set; }
        public ListType KeyItemListType { get; set; }
        private ConfigEntry<string> KeyItemListEntry { get; set; }
        public List<string> KeyItems { get; } = new List<string>();

        public ItemDisplayConfigSection(ConfigFile file, string sectionName, bool alwaysBound = false)
        {
            this.file = file;
            this.alwaysBound = alwaysBound;
            SectionName = RemoveInvalidCharacters(sectionName);

            sectionEnabledDef = new ConfigDefinition(SectionName, nameof(SectionEnabledEntry));
            itemListTypeDef = new ConfigDefinition(SectionName, nameof(ItemListTypeEntry));
            equipmentListTypeDef = new ConfigDefinition(SectionName, nameof(EquipmentListTypeEntry));
            keyItemListTypeDef = new ConfigDefinition(SectionName, nameof(KeyItemListTypeEntry));
            itemListDef = new ConfigDefinition(SectionName, nameof(ItemListEntry));
            equipmentListDef = new ConfigDefinition(SectionName, nameof(EquipmentListEntry));
            keyItemListDef = new ConfigDefinition(SectionName, nameof(KeyItemListEntry));

            Bind(true);

            EnsureCorrectBind();
        }

        private bool EnsureCorrectBind()
        {
            var shouldBind = alwaysBound ||
                SectionEnabled ||
                ItemListType != ListType.Blacklist ||
                EquipmentListType != ListType.Blacklist ||
                KeyItemListType != ListType.Blacklist ||
                Items.Count > 0 ||
                Equipments.Count > 0 ||
                KeyItems.Count > 0;

            if (shouldBind)
            {
                if (!bound)
                {
                    Bind(false);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (bound)
                {
                    Unbind();
                }
            }

            return false;
        }

        public void Unbind()
        {
            if (!bound)
            {
                return;
            }

            bound = false;

            file.Remove(sectionEnabledDef);
            file.Remove(itemListTypeDef);
            file.Remove(equipmentListTypeDef);
            file.Remove(keyItemListTypeDef);
            file.Remove(itemListDef);
            file.Remove(equipmentListDef);
            file.Remove(keyItemListDef);

            SectionEnabledEntry = null;
            ItemListTypeEntry = null;
            EquipmentListTypeEntry = null;
            KeyItemListTypeEntry = null;
            ItemListEntry = null;
            EquipmentListEntry = null;
            KeyItemListEntry = null;
        }

        public void Bind(bool parseValues)
        {
            if (bound)
            {
                return;
            }

            bound = true;

            SectionEnabledEntry = file.Bind(SectionName, nameof(SectionEnabledEntry), alwaysBound, SectionEnabledDesc);
            ItemListTypeEntry = file.Bind(SectionName, nameof(ItemListTypeEntry), ListType.Blacklist, ItemListTypeDesc);
            EquipmentListTypeEntry = file.Bind(SectionName, nameof(EquipmentListTypeEntry), ListType.Blacklist, EquipmentListTypeDesc);
            KeyItemListTypeEntry = file.Bind(SectionName, nameof(KeyItemListTypeEntry), ListType.Blacklist, KeyItemListTypeDesc);
            ItemListEntry = file.Bind(SectionName, nameof(ItemListEntry), "", ItemListDesc);
            EquipmentListEntry = file.Bind(SectionName, nameof(EquipmentListEntry), "", EquipmentListDesc);
            KeyItemListEntry = file.Bind(SectionName, nameof(KeyItemListEntry), "", KeyItemListDesc);

            if (parseValues)
            {
                SectionEnabled = SectionEnabledEntry.Value;
                ItemListType = ItemListTypeEntry.Value;
                EquipmentListType = EquipmentListTypeEntry.Value;
                KeyItemListType = KeyItemListTypeEntry.Value;

                try
                {
                    Items.AddRange(ItemListEntry.Value
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
                    Equipments.AddRange(EquipmentListEntry.Value
                        .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(el => el.Trim())
                        .Distinct());
                }
                catch (Exception e)
                {
                    PartialItemDisplayPlugin.InstanceLogger.LogWarning("Failed to parse `EquipmentList` config");
                    PartialItemDisplayPlugin.InstanceLogger.LogError(e);
                }

                try
                {
                    KeyItems.AddRange(KeyItemListEntry.Value
                        .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(el => el.Trim())
                        .Distinct());
                }
                catch (Exception e)
                {
                    PartialItemDisplayPlugin.InstanceLogger.LogWarning("Failed to parse `KeyItemList` config");
                    PartialItemDisplayPlugin.InstanceLogger.LogError(e);
                }
            }
            else
            {
                SectionEnabledEntry.Value = SectionEnabled;
                ItemListTypeEntry.Value = ItemListType;
                EquipmentListTypeEntry.Value = EquipmentListType;
                KeyItemListTypeEntry.Value = KeyItemListType;
                ItemListEntry.Value = string.Join(", ", Items);
                EquipmentListEntry.Value = string.Join(", ", Equipments);
                KeyItemListEntry.Value = string.Join(", ", KeyItems);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void ApplyToInLobbyConfig(InLobbyConfig.ModConfigEntry configEntry)
        {
            var fields = new List<InLobbyConfig.Fields.IConfigField>
            {
                new InLobbyConfig.Fields.EnumConfigField<ListType>(itemListTypeDef.Key, ItemListTypeDesc, GetItemListType, ChangeItemListType),
                new InLobbyConfig.Fields.SelectListField<string>(itemListDef.Key, ItemListDesc, GetItemList, ItemListItemAdded, ItemListItemRemoved, GetItemOptions),
                new InLobbyConfig.Fields.EnumConfigField<ListType>(equipmentListTypeDef.Key, EquipmentListTypeDesc, GetEquipmentListType, ChangeEquipmentListType),
                new InLobbyConfig.Fields.SelectListField<string>(equipmentListDef.Key, EquipmentListDesc, GetEquipmentList, EquipmentListItemAdded, EquipmentListItemRemoved, GetEquipmentOptions),
                new InLobbyConfig.Fields.EnumConfigField<ListType>(keyItemListTypeDef.Key, KeyItemListTypeDesc, GetKeyItemListType, ChangeKeyItemListType),
                new InLobbyConfig.Fields.SelectListField<string>(keyItemListDef.Key, KeyItemListDesc, GetKeyItemList, KeyItemListItemAdded, KeyItemListItemRemoved, GetKeyItemOptions),
            };
            configEntry.SectionFields[SectionName] = fields;
            configEntry.SectionEnableFields[SectionName] = new InLobbyConfig.Fields.BooleanConfigField(sectionEnabledDef.Key, SectionEnabledDesc, GetEnabled, ChangeEnabled);
        }

        private bool AfterFieldChanged()
        {
            InLobbyConfigIntegration.NeedsSave = true;
            return EnsureCorrectBind();
        }

        private void ChangeItemListType(ListType type)
        {
            ItemListType = type;
            if (AfterFieldChanged())
            {
                ItemListTypeEntry.Value = ItemListType;
            }
        }

        private ListType GetItemListType()
        {
            return ItemListType;
        }

        private void ChangeEquipmentListType(ListType type)
        {
            EquipmentListType = type;
            if (AfterFieldChanged())
            {
                EquipmentListTypeEntry.Value = EquipmentListType;
            }
        }

        private ListType GetEquipmentListType()
        {
            return EquipmentListType;
        }

        private void ChangeKeyItemListType(ListType type)
        {
            KeyItemListType = type;
            if (AfterFieldChanged())
            {
                KeyItemListTypeEntry.Value = KeyItemListType;
            }
        }

        private ListType GetKeyItemListType()
        {
            return KeyItemListType;
        }

        private void ChangeEnabled(bool value)
        {
            SectionEnabled = value;
            if (AfterFieldChanged())
            {
                SectionEnabledEntry.Value = SectionEnabled;
            }
        }

        private bool GetEnabled()
        {
            return SectionEnabled;
        }

        private List<string> GetItemList()
        {
            return Items;
        }

        private void ItemListItemAdded(string value, int index)
        {
            Items.Insert(index, value);
            if (AfterFieldChanged())
            {
                ItemListEntry.Value = string.Join(", ", Items);
            }
        }

        private void ItemListItemRemoved(int index)
        {
            Items.RemoveAt(index);
            if (AfterFieldChanged())
            {
                ItemListEntry.Value = string.Join(", ", Items);
            }
        }

        private Dictionary<string, string> GetItemOptions()
        {
            return ItemCatalog.allItemDefs
                .Where(el =>
                    {
                        if (el.hidden)
                        {
                            return false;
                        }
                        var tier = ItemTierCatalog.GetItemTierDef(el.tier);
                        return tier && tier.isDroppable;
                    })
                .ToDictionary(el => el.name, el => Language.GetString(el.nameToken));
        }

        private List<string> GetEquipmentList()
        {
            return Equipments;
        }

        private void EquipmentListItemAdded(string value, int index)
        {
            Equipments.Insert(index, value);
            if (AfterFieldChanged())
            {
                EquipmentListEntry.Value = string.Join(", ", Equipments);
            }
        }

        private void EquipmentListItemRemoved(int index)
        {
            Equipments.RemoveAt(index);
            if (AfterFieldChanged())
            {
                EquipmentListEntry.Value = string.Join(", ", Equipments);
            }
        }

        private Dictionary<string, string> GetEquipmentOptions()
        {
            return EquipmentCatalog.equipmentDefs
                .Where(el => el.canDrop || el.isBoss || el.dropOnDeathChance > 0)
                .ToDictionary(
                    el => el.name, 
                    el =>
                    {
                        var localized = Language.GetString(el.nameToken);
                        if (string.IsNullOrWhiteSpace(localized))
                        {
                            return el.name;
                        }
                        return localized;
                    });
        }

        private List<string> GetKeyItemList()
        {
            return KeyItems;
        }

        private void KeyItemListItemAdded(string value, int index)
        {
            KeyItems.Insert(index, value);
            if (AfterFieldChanged())
            {
                KeyItemListEntry.Value = string.Join(", ", KeyItems);
            }
        }

        private void KeyItemListItemRemoved(int index)
        {
            KeyItems.RemoveAt(index);
            if (AfterFieldChanged())
            {
                KeyItemListEntry.Value = string.Join(", ", Items);
            }
        }

        private Dictionary<string, string> GetKeyItemOptions()
        {
            return KeyItemCatalog.allKeyItemDefs
                .Where(el => !el.hidden)
                .ToDictionary(el => el.name, el => Language.GetString(el.nameToken));
        }

        private string RemoveInvalidCharacters(string sectionName)
        {
            return Regex.Replace(sectionName, @"[=\n\t""'\\[\]]", "").Trim();
        }
    }
}
