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
        public List<string> ItemList { get; } = new List<string>();

        private ConfigEntry<ListType> EquipmentListTypeEntry { get; set; }
        public ListType EquipmentListType { get; set; }
        private ConfigEntry<string> EquipmentListEntry { get; set; }
        public List<string> EquipmentList { get; } = new List<string>();

        private ConfigEntry<ListType> KeyItemListTypeEntry { get; set; }
        public ListType KeyItemListType { get; set; }
        private ConfigEntry<string> KeyItemListEntry { get; set; }
        public List<string> KeyItemList { get; } = new List<string>();

        public ItemDisplayConfigSection(ConfigFile file, string sectionName, bool alwaysBound = false)
        {
            this.file = file;
            this.alwaysBound = alwaysBound;
            SectionName = RemoveInvalidCharacters(sectionName);

            sectionEnabledDef = new ConfigDefinition(SectionName, nameof(SectionEnabled));
            itemListTypeDef = new ConfigDefinition(SectionName, nameof(ItemListType));
            equipmentListTypeDef = new ConfigDefinition(SectionName, nameof(EquipmentListType));
            keyItemListTypeDef = new ConfigDefinition(SectionName, nameof(KeyItemListType));
            itemListDef = new ConfigDefinition(SectionName, nameof(ItemList));
            equipmentListDef = new ConfigDefinition(SectionName, nameof(EquipmentList));
            keyItemListDef = new ConfigDefinition(SectionName, nameof(KeyItemList));

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
                ItemList.Count > 0 ||
                EquipmentList.Count > 0 ||
                KeyItemList.Count > 0;

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

            SectionEnabledEntry = file.Bind(SectionName, nameof(SectionEnabled), alwaysBound, SectionEnabledDesc);
            ItemListTypeEntry = file.Bind(SectionName, nameof(ItemListType), ListType.Blacklist, ItemListTypeDesc);
            EquipmentListTypeEntry = file.Bind(SectionName, nameof(EquipmentListType), ListType.Blacklist, EquipmentListTypeDesc);
            KeyItemListTypeEntry = file.Bind(SectionName, nameof(KeyItemListType), ListType.Blacklist, KeyItemListTypeDesc);
            ItemListEntry = file.Bind(SectionName, nameof(ItemList), "", ItemListDesc);
            EquipmentListEntry = file.Bind(SectionName, nameof(EquipmentList), "", EquipmentListDesc);
            KeyItemListEntry = file.Bind(SectionName, nameof(KeyItemList), "", KeyItemListDesc);

            if (parseValues)
            {
                SectionEnabled = SectionEnabledEntry.Value;
                ItemListType = ItemListTypeEntry.Value;
                EquipmentListType = EquipmentListTypeEntry.Value;
                KeyItemListType = KeyItemListTypeEntry.Value;

                try
                {
                    ItemList.AddRange(ItemListEntry.Value
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
                    EquipmentList.AddRange(EquipmentListEntry.Value
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
                    KeyItemList.AddRange(KeyItemListEntry.Value
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
                ItemListEntry.Value = string.Join(", ", ItemList);
                EquipmentListEntry.Value = string.Join(", ", EquipmentList);
                KeyItemListEntry.Value = string.Join(", ", KeyItemList);
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
            return ItemList;
        }

        private void ItemListItemAdded(string value, int index)
        {
            ItemList.Insert(index, value);
            if (AfterFieldChanged())
            {
                ItemListEntry.Value = string.Join(", ", ItemList);
            }
        }

        private void ItemListItemRemoved(int index)
        {
            ItemList.RemoveAt(index);
            if (AfterFieldChanged())
            {
                ItemListEntry.Value = string.Join(", ", ItemList);
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
            return EquipmentList;
        }

        private void EquipmentListItemAdded(string value, int index)
        {
            EquipmentList.Insert(index, value);
            if (AfterFieldChanged())
            {
                EquipmentListEntry.Value = string.Join(", ", EquipmentList);
            }
        }

        private void EquipmentListItemRemoved(int index)
        {
            EquipmentList.RemoveAt(index);
            if (AfterFieldChanged())
            {
                EquipmentListEntry.Value = string.Join(", ", EquipmentList);
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
            return KeyItemList;
        }

        private void KeyItemListItemAdded(string value, int index)
        {
            KeyItemList.Insert(index, value);
            if (AfterFieldChanged())
            {
                KeyItemListEntry.Value = string.Join(", ", KeyItemList);
            }
        }

        private void KeyItemListItemRemoved(int index)
        {
            KeyItemList.RemoveAt(index);
            if (AfterFieldChanged())
            {
                KeyItemListEntry.Value = string.Join(", ", ItemList);
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
