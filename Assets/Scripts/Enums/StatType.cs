// Cliente/Enums/StatType.cs (DEVE SER IDÊNTICO AO DO SERVIDOR)
public enum StatType
{
    // --- Atributos Primários ---
    Strength, Agility, Intellect, Stamina,

    // --- Atributos Defensivos ---
    Armor,

    // --- Atributos Secundários (Combat Ratings) ---
    CriticalStrikeRating, HasteRating, MasteryRating,

    // --- Atributos Terciários (Bônus Raros) ---
    MovementSpeed, Leech, Avoidance,

    // --- Atributos Derivados (Calculados) ---
    Health, Mana, AttackPower, SpellPower,
    CriticalStrikeChance, Haste,
}