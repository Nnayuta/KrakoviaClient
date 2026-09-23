using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class ItemExporter
{
    [MenuItem("Tools/RPG/Export Items to JSON")]
    public static void ExportItems()
    {
        string[] guids = AssetDatabase.FindAssets("t:Item");
        List<object> serverItems = new List<object>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Item item = AssetDatabase.LoadAssetAtPath<Item>(path);

            if (item == null || string.IsNullOrEmpty(item.itemID)) continue;

            bool assetWasModified = false;

            if (item.autoGenerateStats)
            {
                item.Stats = StatAllocator.GenerateStats(item);
                assetWasModified = true;
            }

            int calculatedSellPrice = StatAllocator.CalculateSellPrice(item);

            if (item.sellPrice != calculatedSellPrice)
            {
                item.sellPrice = calculatedSellPrice;
                assetWasModified = true;
            }

            if (assetWasModified)
            {
                EditorUtility.SetDirty(item);
            }

            serverItems.Add(MapToServerData(item));
        }

        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };
        string json = JsonConvert.SerializeObject(serverItems, settings);

        string exportPath = Path.Combine(Application.dataPath, "..", "..", "Krakovia Server", "Krakovia Server", "ServerData", "items.json");
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
        File.WriteAllText(exportPath, json);

        Debug.Log($"<color=green>Exportação concluída!</color> {serverItems.Count} itens foram salvos em: {exportPath}");
        EditorUtility.RevealInFinder(exportPath);
    }

    private static void MapBaseItemData(Item source, ServerItemData destination)
    {
        destination.itemID = source.itemID;
        destination.itemName = source.itemName;
        destination.quality = source.quality;
        destination.maxStackSize = source.maxStackSize;
        destination.requiredLevel = source.requiredLevel;
        destination.sellPrice = source.sellPrice;
        destination.primaryStatFocus = source.primaryStatFocus;
        destination.Stats = source.Stats
                                 .Select(s => new BaseStatData { Stat = s.Stat, Value = s.Value })
                                 .ToList();
    }

    // NENHUMA MUDANÇA NECESSÁRIA AQUI
    private static void MapEquipmentData(EquipmentItem source, ServerEquipmentData destination)
    {
        destination.equipmentSlot = source.equipmentSlot;
    }

    private static object MapToServerData(Item item)
    {
        if (item is WeaponItem weapon)
        {
            var serverWeapon = new ServerWeaponData
            {
                weaponType = weapon.weaponType,
                handType = weapon.handType,
            };
            MapBaseItemData(weapon, serverWeapon);
            MapEquipmentData(weapon, serverWeapon);
            return serverWeapon;
        }

        if (item is ArmorItem armor)
        {
            var serverArmor = new ServerArmorData
            {
                armorType = armor.armorType,
            };
            MapBaseItemData(armor, serverArmor);
            MapEquipmentData(armor, serverArmor);
            return serverArmor;
        }

        // ----- INÍCIO DA MUDANÇA -----
        if (item is ConsumableItem consumable)
        {
            var serverConsumable = new ServerConsumableData
            {
                // Mapeia os novos campos de ganho instantâneo
                InstantHealthGain = consumable.instantHealthGain,
                InstantResourceGain = consumable.instantResourceGain,

                // Mapeia o ID do StatusEffect.
                // Usamos um operador ternário para garantir que, se nenhum efeito for
                // atribuído no Unity, o valor será nulo no JSON, evitando erros.
                StatusEffectID = consumable.effectToApply != null ? consumable.effectToApply.effectID : null
            };
            MapBaseItemData(consumable, serverConsumable);
            return serverConsumable;
        }
        // ----- FIM DA MUDANÇA -----

        if (item is JunkItem junk)
        {
            var serverJunk = new ServerJunkItemData();
            MapBaseItemData(junk, serverJunk);
            return serverJunk;
        }

        var serverGeneric = new ServerItemData();
        MapBaseItemData(item, serverGeneric);
        return serverGeneric;
    }
}