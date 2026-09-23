// Scripts/Items/WeaponItem.cs
using Newtonsoft.Json;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "RPG/Items/Weapon")]
public class WeaponItem : EquipmentItem
{
    [Header("Dados de Combate")]
    [Tooltip("O dano mínimo que esta arma pode causar.")]
    public float minDamage;
    [Tooltip("O dano máximo que esta arma pode causar.")]
    public float maxDamage;
    [Tooltip("A velocidade do ataque da arma (ex: 2.8s).")]
    public float weaponSpeed = 2.0f;

    [Header("Dados da Arma")]
    public WeaponHandType handType;
    public WeaponType weaponType;
    public GameObject weaponPrefab;
    public WeaponSheathSlot sheathSlot;
}