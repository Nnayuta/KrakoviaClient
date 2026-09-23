// Assets/Scripts/SharedServerData/ServerAbilityData.cs (NOVO ARQUIVO NO UNITY)

using System.Collections.Generic;
using Newtonsoft.Json;

// Coloque todas as definições de DTOs do servidor relacionadas a habilidades aqui.
// O seu AbilityExporter usará estas.

// Enums compartilhados
public enum AbilityIntent { Harmful, Helpful }

public enum DamageType { Physical, Magical, Fire, Frost, Nature, Holy, Shadow }

[System.Serializable]
public class StatModifierDefinition
{
    public StatType targetStat;
    public float value;
    public StatModifierType type;
}

public class ServerStatusEffectData
{
    public string EffectID { get; set; } = string.Empty;
    public float Duration { get; set; }
    public bool IsBuff { get; set; }
    public List<StatModifierDefinition> StatModifiers { get; set; } = new();
}

public abstract class ServerAbilityEffectData
{
    public string EffectType => this.GetType().Name;
    public AbilityIntent Intent { get; set; }
}

// --- EFEITOS EXISTENTES (SEM MUDANÇAS) ---
public class ServerDamageEffectData : ServerAbilityEffectData
{
    public float BaseValue { get; set; }
    public float AttackPowerScaling { get; set; }
    public float SpellPowerScaling { get; set; }
    public ServerDamageEffectData() { Intent = AbilityIntent.Harmful; }
}
public class ServerHealEffectData : ServerAbilityEffectData
{
    public float BaseValue { get; set; }
    public float SpellPowerScaling { get; set; }
    public ServerHealEffectData() { Intent = AbilityIntent.Helpful; }
}
public class ServerApplyStatusEffectData : ServerAbilityEffectData
{
    public string StatusEffectID { get; set; } = string.Empty;
}
public enum WeaponRequirement
{
    None,           // Pode ser usada com qualquer coisa (ex: um grito de buff)
    WeaponRequired, // Requer QUALQUER arma equipada (não pode ser desarmado)
    MeleeWeapon,    // Requer uma espada, machado, maça, etc.
    RangedWeapon,   // Requer um arco, besta, arma de fogo
    Unarmed         // Requer que o jogador esteja desarmado
}

public enum AbilityEffectType { Damage, Heal, ApplyBuff }
public enum AbilityType { Active, Passive }
public enum TargetType
{
    Self,               // Afeta apenas o próprio caster.
    SingleTarget,       // Afeta um único alvo.

    // --- NOVOS TIPOS DE AOE ---
    AreaOfEffectSelf,   // Um círculo de efeito centrado no próprio caster (ex: Nova).
    AreaOfEffectTarget, // Um círculo de efeito centrado no alvo atual (ex: Explosão).
    AreaOfEffectGround, // Um círculo de efeito em um local selecionado no chão (ex: Chuva de Fogo).

    Cone,               // Um cone na frente do caster.
    Projectile          // Um projétil que viaja até um alvo.
}

public class AbilityData
{
    public string ID { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AbilityIntent Intent { get; set; }
    public List<ServerAbilityEffectData> Effects { get; set; } = new List<ServerAbilityEffectData>();
    public float Cooldown { get; set; }
    public float Range { get; set; }
    public float ResourceCost { get; set; }
    public bool RequiresTarget { get; set; }
    public WeaponRequirement WeaponRequirement { get; set; } // Supondo que este enum esteja em outro arquivo compartilhado
    public int Priority { get; set; } = 0;
    public AbilityType Type { get; set; }
    public float CastTime { get; set; }
    public bool CanMoveWhileCasting { get; set; }
    public float ProjectileSpeed { get; set; }
    public TargetType TargetType { get; set; }
    public float AoeRadius { get; set; }
    public float ConeAngle { get; set; }

}


/// <summary>
/// Efeito que invoca um ou mais NPCs em uma posição específica.
/// </summary>
public class ServerSummonNpcEffectData : ServerAbilityEffectData
{
    // O ID do tipo de NPC a ser invocado (ex: "skeleton_minion").
    public string NpcTypeId { get; set; } = string.Empty;

    // Quantos NPCs serão invocados.
    public int Quantity { get; set; } = 1;

    // A duração em segundos que os NPCs invocados permanecerão vivos. 0 = permanente.
    public float DurationSeconds { get; set; } = 30.0f;

    // O raio ao redor do ponto de impacto onde os NPCs podem aparecer.
    public float SpawnRadius { get; set; } = 2.0f;

    public ServerSummonNpcEffectData() { Intent = AbilityIntent.Helpful; } // Invocar é "útil" para o caster.
}

/// <summary>
/// Efeito que cria uma zona persistente no chão que aplica outros efeitos.
/// Ex: Uma poça de fogo que causa dano contínuo a quem estiver dentro.
/// </summary>
public class ServerCreateHazardEffectData : ServerAbilityEffectData
{
    // A duração em segundos que a zona perigosa permanecerá no chão.
    public float DurationSeconds { get; set; } = 8.0f;

    // O raio da zona perigosa.
    public float Radius { get; set; } = 3.0f;

    // Com que frequência (em segundos) o efeito será aplicado em quem estiver dentro.
    public float TickRate { get; set; } = 1.0f;

    // A lista de efeitos que serão aplicados a cada "tick".
    // (Sim, um efeito pode conter outros efeitos!)
    public List<ServerAbilityEffectData> TickEffects { get; set; } = new();

    public ServerCreateHazardEffectData() { Intent = AbilityIntent.Harmful; }
}

public class AbilityListWrapper
{
    public List<AbilityData> Abilities { get; set; } = new List<AbilityData>();
}
