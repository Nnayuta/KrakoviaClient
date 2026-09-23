// Cliente/Editor/AbilityExporter.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class AbilityExporter
{
    private class AbilityListWrapper { public List<AbilityData> Abilities { get; set; } }

    [MenuItem("Tools/RPG/Export Abilities to JSON")]
    public static void ExportAbilities()
    {
        var serverAbilities = new List<AbilityData>();
        string[] guids = AssetDatabase.FindAssets("t:Ability");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Ability abilitySO = AssetDatabase.LoadAssetAtPath<Ability>(path);
            if (abilitySO == null) continue;

            var serverAbility = new AbilityData
            {
                // ... (propriedades base da habilidade, sem alterações)
                ID = abilitySO.ID,
                Name = abilitySO.abilityName,
                Intent = abilitySO.intent,
                Cooldown = abilitySO.cooldownTime,
                Range = abilitySO.range,
                ResourceCost = abilitySO.resourceCost,
                RequiresTarget = abilitySO.requiresTarget,
                WeaponRequirement = abilitySO.weaponRequirement,
                Priority = abilitySO.priority,
                Type = abilitySO.abilityType,
                CastTime = abilitySO.castTime,
                CanMoveWhileCasting = abilitySO.canMoveWhileCasting,
                ProjectileSpeed = abilitySO.projectileSpeed,
                TargetType = abilitySO.targetType,
                AoeRadius = abilitySO.AoeRadius,
                Effects = ConvertClientEffectsToServer(abilitySO.effects)
            };

            serverAbilities.Add(serverAbility);
        }

        var wrapper = new AbilityListWrapper { Abilities = serverAbilities };
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        string json = JsonConvert.SerializeObject(wrapper, settings);
        json = json.Replace(", Assembly-CSharp", "");

        string exportPath = Path.Combine(Application.dataPath, "..", "..", "Krakovia Server", "Krakovia Server", "ServerData", "abilities.json");
        File.WriteAllText(exportPath, json);

        Debug.Log($"<color=lime>[SUCESSO]</color> Exportação de Habilidades concluída! {serverAbilities.Count} habilidades salvas.");
    }

    // (NOVO) Método auxiliar para converter os efeitos, mantendo o código limpo
    private static List<ServerAbilityEffectData> ConvertClientEffectsToServer(List<AbilityEffect> clientEffects)
    {
        var serverEffects = new List<ServerAbilityEffectData>();
        if (clientEffects == null) return serverEffects;

        foreach (var effectSO in clientEffects)
        {
            if (effectSO == null) continue;

            // --- LÓGICA DE CONVERSÃO ---
            if (effectSO is DamageEffect damage)
            {
                serverEffects.Add(new ServerDamageEffectData
                {
                    BaseValue = damage.baseValue,
                    AttackPowerScaling = damage.attackPowerScaling,
                    SpellPowerScaling = damage.spellPowerScaling,
                    Intent = AbilityIntent.Harmful // Garante a intenção
                });
            }
            else if (effectSO is HealEffect heal)
            {
                serverEffects.Add(new ServerHealEffectData
                {
                    BaseValue = heal.baseValue,
                    SpellPowerScaling = heal.spellPowerScaling,
                    Intent = AbilityIntent.Helpful
                });
            }
            // (NOVO) Converte o efeito de Summon
            else if (effectSO is SummonNpcEffect summon)
            {
                if (summon.npcToSummon == null)
                {
                    Debug.LogError($"Efeito de Summon em uma habilidade não tem NPC definido!");
                    continue;
                }
                serverEffects.Add(new ServerSummonNpcEffectData
                {
                    NpcTypeId = summon.npcToSummon.TypeId,
                    Quantity = summon.quantity,
                    DurationSeconds = summon.durationSeconds,
                    SpawnRadius = summon.spawnRadius,
                    Intent = AbilityIntent.Helpful
                });
            }
            // (NOVO) Converte o efeito de Hazard (com recursão!)
            else if (effectSO is CreateHazardEffect hazard)
            {
                serverEffects.Add(new ServerCreateHazardEffectData
                {
                    DurationSeconds = hazard.durationSeconds,
                    Radius = hazard.radius,
                    TickRate = hazard.tickRate,
                    // Converte os efeitos de tick internos recursivamente
                    TickEffects = ConvertClientEffectsToServer(hazard.tickEffects),
                    Intent = AbilityIntent.Harmful
                });
            }
            // Adicione outros 'else if' para futuros tipos de efeito
        }
        return serverEffects;
    }
}