using System.Collections.Generic;

// Este helper estático pode ser acessado de qualquer lugar
public static class WeaponHelper
{
    // Um conjunto (HashSet) para busca ultra-rápida.
    // Ele contém todos os tipos de arma considerados corpo a corpo.
    private static readonly HashSet<WeaponType> MeleeWeaponTypes = new HashSet<WeaponType>
    {
        WeaponType.Sword1H, WeaponType.Axe1H, WeaponType.Mace1H,
        WeaponType.Dagger, WeaponType.Fist,
        WeaponType.Sword2H, WeaponType.Axe2H, WeaponType.Mace2H,
        WeaponType.Polearm, WeaponType.Staff
    };

    // Um conjunto para as armas de longo alcance
    private static readonly HashSet<WeaponType> RangedWeaponTypes = new HashSet<WeaponType>
    {
        WeaponType.Bow, WeaponType.Crossbow, WeaponType.Gun
    };

    /// <summary>
    /// Verifica se um tipo de arma é considerado corpo a corpo (Melee).
    /// </summary>
    public static bool IsMelee(WeaponType type)
    {
        return MeleeWeaponTypes.Contains(type);
    }

    /// <summary>
    /// Verifica se um tipo de arma é considerado de longo alcance (Ranged).
    /// </summary>
    public static bool IsRanged(WeaponType type)
    {
        return RangedWeaponTypes.Contains(type);
    }
}