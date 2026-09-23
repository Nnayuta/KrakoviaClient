// Cliente/Editor/VendorExporter.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class VendorExporter
{
    // Estruturas de dados para o JSON do servidor (espelham o que o servidor espera)
    private class ServerVendorItem
    {
        public string ItemID { get; set; }
        public int BuyPrice { get; set; }
    }

    private class ServerVendorData
    {
        public string NpcTypeId { get; set; }
        public List<ServerVendorItem> Items { get; set; }
    }

    // O "Wrapper" que contém a lista principal, como no seu JSON de exemplo
    private class VendorListWrapper { public List<ServerVendorData> Vendors; }
    private const float VENDOR_BUY_PRICE_MARGIN = 2.5f;

    [MenuItem("Tools/RPG/Export Vendors to JSON")]
    public static void ExportVendors()
    {
        var serverVendors = new List<ServerVendorData>();
        string[] guids = AssetDatabase.FindAssets("t:VendorData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            VendorData vendorDef = AssetDatabase.LoadAssetAtPath<VendorData>(path);

            // Validação crucial: Pula se o vendor não tiver um NPC associado ou ID
            if (vendorDef == null || vendorDef.VendorNpc == null || string.IsNullOrEmpty(vendorDef.VendorNpc.npcTypeId))
            {
                Debug.LogWarning($"[VendorExporter] O asset de vendedor em '{path}' foi ignorado por não ter um NPC válido associado.");
                continue;
            }

            var serverData = new ServerVendorData
            {
                // Pega o ID do NpcData linkado
                NpcTypeId = vendorDef.VendorNpc.npcTypeId,

                // Converte a lista de itens para o formato do servidor
                Items = vendorDef.Items
                    .Where(vendorItem => vendorItem.Item != null && !string.IsNullOrEmpty(vendorItem.Item.itemID))
                    .Select(vendorItem =>
                    {
                        // 1. Calcula o preço de VENDA base do item.
                        int sellPrice = StatAllocator.CalculateSellPrice(vendorItem.Item);

                        // 2. Calcula o preço de COMPRA do jogador, aplicando a margem.
                        int buyPrice = Mathf.RoundToInt(sellPrice * VENDOR_BUY_PRICE_MARGIN);

                        // 3. Cria o objeto para o JSON com o preço calculado.
                        return new ServerVendorItem
                        {
                            ItemID = vendorItem.Item.itemID,
                            BuyPrice = Mathf.Max(sellPrice + 1, buyPrice) // Garante que o preço de compra seja sempre maior que o de venda.
                        };
                    }).ToList()
            };

            serverVendors.Add(serverData);
        }

        var wrapper = new VendorListWrapper { Vendors = serverVendors };
        var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };

        string json = JsonConvert.SerializeObject(wrapper, settings);
        string exportPath = Path.Combine(Application.dataPath, "..", "..", "Krakovia Server", "Krakovia Server", "ServerData", "vendors.json");
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
        File.WriteAllText(exportPath, json);

        Debug.Log($"<color=orange>[VendorExporter]</color> {serverVendors.Count} Vendedores exportados para: {exportPath}");
        EditorUtility.RevealInFinder(exportPath);
    }
}