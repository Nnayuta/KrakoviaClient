// --- CLIENTE (Unity) ---
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AbilityUnlockEntry
{
    public Ability ability;
    public int levelRequired = 1;
}

[CreateAssetMenu(fileName = "New Ability", menuName = "RPG/Ability")]
public class Ability : ScriptableObject
{
    [Header("== DADOS SINCRONIZADOS COM O SERVIDOR ==")]
    [Tooltip("ID único, ex: 'warrior_charge', 'mage_fireball'. Usado em todo o código.")]
    public string ID;
    public AbilityType abilityType;
    public AbilityIntent intent;
    public TargetType targetType;
    public int priority = 0;

    public float resourceCost;
    public float cooldownTime;
    public float range;
    public bool requiresTarget;
    public WeaponRequirement weaponRequirement;

    // --- (A MUDANÇA ESTÁ AQUI) ---
    [Tooltip("Para habilidades de Área de Efeito, este é o raio da área em metros.")]
    public float AoeRadius;

    [Header("Efeitos da Habilidade")]
    public List<AbilityEffect> effects;

    [Header("== Mecânicas de Casting (Servidor) ==")]
    [Tooltip("Tempo em segundos para conjurar a habilidade. 0 = instantâneo.")]
    public float castTime;
    public bool canMoveWhileCasting;

    [Header("== Mecânicas de Projétil (Servidor) ==")]
    public float projectileSpeed;

    [Header("== DADOS EXCLUSIVOS DO CLIENTE (Apresentação) ==")]
    public string abilityName;
    [TextArea] public string description;
    public Sprite icon;

    public CombatStyle combatStyleForAnimation = CombatStyle.Melee;
    public DamageType visualDamageType = DamageType.Physical;

    [Header("SFX")]
    public AudioClip soundEffect;
    public AnimationClip animationClip;

    [Header("SFX Caster")]
    public AudioClip soundEffectCast;
    public AnimationClip animationClipCast;

    [Header("VFX")]
    public GameObject hitEffectPrefab;
    public EffectSpawnLocation spawnLocation = EffectSpawnLocation.OnTarget;
    public Vector3 effectSpawnOffset = Vector3.zero;
    public bool shouldFollowTarget = false;
    public bool rotateToSurfaceNormal = false;
    public bool alignToMovementDirection = true;
    public float effectDuration = 2f;

    [Header("VFX Caster")]
    public GameObject casterEffectPrefab;
    public Vector3 casterEffectSpawnOffset = Vector3.zero;

    [Header("Projétil")]
    public GameObject projectilePrefab;
}