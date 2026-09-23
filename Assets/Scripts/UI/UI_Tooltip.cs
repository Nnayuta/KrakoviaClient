// UI/UI_Tooltip.cs
using UnityEngine;
using TMPro;
using System.Text;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;

[RequireComponent(typeof(CanvasGroup))]
public class UI_Tooltip : MonoBehaviour
{
    #region Singleton
    private static UI_Tooltip _instance;
    public static UI_Tooltip Instance { get { if (_instance == null) { _instance = FindFirstObjectByType<UI_Tooltip>(FindObjectsInactive.Include); } return _instance; } }
    #endregion

    [Header("Painel Principal")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private TextMeshProUGUI mainContentText;

    [Header("Painel de Comparação")]
    [SerializeField] private GameObject comparisonPanel;
    [SerializeField] private TextMeshProUGUI comparisonContentText;

    [Header("Configuração")]
    [SerializeField] private Vector2 offset = new Vector2(15f, -15f);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }
    private void LateUpdate()
    {
        if (gameObject.activeSelf) { ApplyPositioning(); }
    }

    #region Métodos de Exibição (Públicos)

    /// <summary>
    /// MÉTODO DE ENTRADA ATUALIZADO. Recebe um ItemStack e orquestra a exibição.
    /// </summary>
    public void ShowItemTooltip(ItemStack itemStack, int contextualPrice = -1, bool isBuyPrice = false)
    {
        if (itemStack == null) return;
        Item itemTemplate = GameDatabase.Instance.GetItem(itemStack.ItemID);
        if (itemTemplate == null) return;

        ItemInstanceData instanceData = ItemInstanceDataManager.Instance.GetData(itemStack.InstanceID);

        string mainContent = GenerateItemTooltip(itemTemplate, instanceData, contextualPrice, isBuyPrice);
        mainContentText.text = mainContent;
        mainPanel.SetActive(true);

        if (itemTemplate is EquipmentItem equipmentToShow)
        {
            ItemStack equippedItemStack = LocalPlayerData.Instance.GetEquippedItemStackInSlot(equipmentToShow.equipmentSlot);
            if (equippedItemStack != null && equippedItemStack.InstanceID != itemStack.InstanceID)
            {
                Item equippedTemplate = GameDatabase.Instance.GetItem(equippedItemStack.ItemID);
                // CORREÇÃO: Pega os dados da instância do item equipado
                ItemInstanceData equippedInstanceData = ItemInstanceDataManager.Instance.GetData(equippedItemStack.InstanceID);

                string comparisonContent = GenerateItemTooltip(equippedTemplate, equippedInstanceData);
                comparisonContentText.text = comparisonContent;
                comparisonPanel.SetActive(true);
            }
            else
            {
                comparisonPanel.SetActive(false);
            }
        }
        else
        {
            comparisonPanel.SetActive(false);
        }

        gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        ApplyPositioning();
    }

    // Este método agora serve como um fallback para casos onde não temos um ItemStack (ex: item de quest, etc.)
    public void ShowItemTooltip(Item itemToShow, int contextualPrice = -1, bool isBuyPrice = false)
    {
        string mainContent = GenerateItemTooltip(itemToShow, null, contextualPrice, isBuyPrice);
        mainContentText.text = mainContent;
        mainPanel.SetActive(true);
        comparisonPanel.SetActive(false);
        ShowTooltip(mainContent);
    }


    /// <summary>
    /// Mostra uma tooltip genérica (como a de habilidades).
    /// </summary>
    public void ShowTooltip(string content)
    {
        if (string.IsNullOrEmpty(content)) return;
        mainContentText.text = content;
        mainPanel.SetActive(true);
        comparisonPanel.SetActive(false);
        gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        ApplyPositioning();
    }

    /// <summary>
    /// Esconde completamente a tooltip.
    /// </summary>
    public void HideTooltip()
    {
        gameObject.SetActive(false);
        mainPanel.SetActive(false);
        comparisonPanel.SetActive(false);
    }

    #endregion

    #region Geradores de Conteúdo (Estáticos)
    /// <summary>
    /// MÉTODO PRINCIPAL ATUALIZADO para gerar a string da tooltip.
    /// Ele agora recebe os stats da instância como a fonte primária da verdade.
    /// </summary>
    public static string GenerateItemTooltip(Item itemTemplate, ItemInstanceData instanceData, int contextualPrice = -1, bool isBuyPrice = false)
    {
        if (itemTemplate == null) return "";

        List<ItemBaseStatUnity> statsToDisplay = instanceData?.Stats ?? itemTemplate.Stats;
        ItemQuality qualityToDisplay = instanceData?.Quality ?? itemTemplate.quality;
        int iLvlToDisplay = instanceData?.ItemLevel ?? itemTemplate.itemLevel;
        int reqLvlToDisplay = instanceData?.RequiredLevel ?? itemTemplate.requiredLevel;
        // <<< NOVA LINHA >>> Pega o preço de venda da instância, ou do template como fallback
        int sellPriceToDisplay = instanceData?.SellPrice ?? itemTemplate.sellPrice;

        StringBuilder sb = new StringBuilder();

        sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(GetQualityColor(qualityToDisplay))}><b>{itemTemplate.itemName}</b></color>\n");
        if (iLvlToDisplay > 0) sb.Append($"Nível do Item {iLvlToDisplay}\n");

        if (itemTemplate is WeaponItem weapon)
        {
            sb.Append($"<color=grey>{GetEquipmentSlotName(weapon.equipmentSlot)}</color>");
            sb.Append($"<color=grey>   |   {GetWeaponTypeName(weapon.weaponType)}</color>\n");
            sb.Append($"<b>{weapon.minDamage} - {weapon.maxDamage}</b> Dano");
            sb.Append($"<align=right>Velocidade <b>{weapon.weaponSpeed:F2}</b></align>\n");
            sb.Append($"<align=right>(<b>{(weapon.minDamage + weapon.maxDamage) / 2f / weapon.weaponSpeed:F1}</b> de dano por segundo)</align>\n");
        }
        else if (itemTemplate is ArmorItem armor)
        {
            sb.Append($"<color=grey>{GetEquipmentSlotName(armor.equipmentSlot)}</color>");
            sb.Append($"<color=grey>   |   {armor.armorType}</color>\n");
            var armorStat = statsToDisplay.Find(s => s.Stat == StatType.Armor);
            if (armorStat != null) sb.Append($"<b>{armorStat.Value}</b> Armadura\n");
        }
        else if (itemTemplate is ConsumableItem) sb.Append($"<color=grey>Consumível</color>\n");
        else if (itemTemplate is JunkItem) sb.Append($"<color=grey>Item de Troca</color>\n");
        else if (itemTemplate is EquipmentItem eq) sb.Append($"<color=grey>{GetEquipmentSlotName(eq.equipmentSlot)}</color>\n");

        if (statsToDisplay.Any()) sb.Append("\n");

        if (itemTemplate is EquipmentItem)
        {
            foreach (var stat in statsToDisplay.Where(s => s.Stat != StatType.Armor))
            {
                sb.Append($"+{stat.Value} {GetStatName(stat.Stat)}\n");
            }
        }

        if (itemTemplate is ConsumableItem consumableEffect)
        {
            if (consumableEffect.instantHealthGain > 0) sb.Append($"<color=green>Uso: Restaura {consumableEffect.instantHealthGain} de Vida.</color>\n");
            if (consumableEffect.instantResourceGain > 0) sb.Append($"<color=#87CEFA>Uso: Restaura {consumableEffect.instantResourceGain} de Mana.</color>\n");
            if (consumableEffect.effectToApply != null) sb.Append(GenerateStatusEffectDescription(consumableEffect.effectToApply));
        }

        if (reqLvlToDisplay > 1)
        {
            string color = (LocalPlayerData.Instance != null && LocalPlayerData.Instance.Level < reqLvlToDisplay) ? "red" : "white";
            sb.Append($"\n<color={color}>Requer Nível {reqLvlToDisplay}</color>\n");
        }

        // =================================================================================
        // <<< LÓGICA DE PREÇO ATUALIZADA >>>
        // =================================================================================
        sb.Append("\n");
        if (isBuyPrice && contextualPrice > 0)
        {
            sb.Append($"<color=#FFD700>Compra por: {new Currency(contextualPrice)}</color>\n");
        }
        else if (sellPriceToDisplay > 0)
        {
            sb.Append($"Vende por: {new Currency(sellPriceToDisplay)}\n");
        }
        // =================================================================================

        return sb.ToString().TrimEnd('\n');
    }

    public static string GenerateItemTooltip(Item item, int contextualPrice = -1, bool isBuyPrice = false)
    {
        return GenerateItemTooltip(item, null, contextualPrice, isBuyPrice);
    }

    /// <summary>
    /// Gera uma linha de texto descrevendo os bônus de um StatusEffect.
    /// </summary>
    private static string GenerateStatusEffectDescription(StatusEffect status)
    {
        // Se o efeito não existe ou não tem modificadores, não mostra nada.
        if (status == null || status.statModifiers == null || status.statModifiers.Count == 0)
        {
            return "";
        }

        // Cria uma lista de strings, cada uma descrevendo um modificador.
        // Ex: "Aumenta Força em 10", "Aumenta Vigor em 15"
        var modifierDescriptions = status.statModifiers
            .Select(mod => $"{(mod.value > 0 ? "Aumenta" : "Reduz")} {GetStatName(mod.targetStat)} em <b>{Mathf.Abs(mod.value)}</b>")
            .ToList();

        // Junta todas as descrições com ", " e adiciona a duração.
        // Ex: "Uso: Aumenta Força em 10, Aumenta Vigor em 15 por 600 segundos."
        string finalDescription = string.Join(", ", modifierDescriptions);
        return $"<color=green>Uso: {finalDescription} por {status.duration} segundos.</color>\n";
    }

    public static string GenerateAbilityTooltip(Ability ability)
    {
        if (ability == null) return "";

        StringBuilder sb = new StringBuilder();

        // --- 1. Cabeçalho ---
        sb.Append($"<b>{ability.abilityName}</b>\n");
        var headerInfo = new List<string>();
        if (ability.resourceCost > 0) headerInfo.Add($"<color=#ADD8E6>{ability.resourceCost} Mana</color>");
        if (ability.range > 0) headerInfo.Add($"{ability.range}m de Alcance");
        if (headerInfo.Count > 0) sb.Append(string.Join("   ", headerInfo) + "\n");
        var timingInfo = new List<string>();
        if (ability.abilityType == AbilityType.Active)
        {
            timingInfo.Add(ability.castTime > 0 ? $"{ability.castTime:F1}s de lançamento" : "Instantâneo");
            if (ability.cooldownTime > 0) timingInfo.Add($"{ability.cooldownTime:F1}s de recarga");
            if (timingInfo.Count > 0) sb.Append(string.Join("   ", timingInfo) + "\n");
        }
        else
        {
            sb.Append("<color=grey>Passiva</color>\n");
        }
        sb.Append("\n");

        // --- 2. Descrição dos Efeitos ---
        if (ability.effects != null && ability.effects.Count > 0)
        {
            foreach (var effect in ability.effects)
            {
                sb.Append(GenerateEffectDescription(effect));
                sb.Append("\n");
            }
        }

        // --- 3. Flavor Text e Requisitos ---
        if (!string.IsNullOrEmpty(ability.description)) sb.Append($"\n<color=yellow><i>\"{ability.description}\"</i></color>");
        if (ability.weaponRequirement != WeaponRequirement.None) sb.Append($"\n\n<color=red>Requer: {GetWeaponRequirementName(ability.weaponRequirement)}</color>");

        return sb.ToString();
    }

    #endregion

    #region Métodos Auxiliares

    private void ApplyPositioning()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 pivot = new Vector2(0, 1);

        // O cálculo do tamanho agora considera os dois painéis
        float totalWidth = mainPanel.activeSelf ? mainPanel.GetComponent<RectTransform>().rect.width : 0;
        if (comparisonPanel.activeSelf) totalWidth += comparisonPanel.GetComponent<RectTransform>().rect.width;
        float totalHeight = mainPanel.activeSelf ? mainPanel.GetComponent<RectTransform>().rect.height : 0;

        if (mousePosition.x + totalWidth > Screen.width) pivot.x = 1;
        if (mousePosition.y - totalHeight < 0) pivot.y = 0;

        rectTransform.pivot = pivot;
        Vector2 finalOffset = new Vector2(offset.x * (pivot.x == 0 ? 1 : -1), offset.y * (pivot.y == 1 ? 1 : -1));
        transform.position = mousePosition + finalOffset;
    }

    private static string GenerateEffectDescription(AbilityEffect effect)
    {
        if (effect is DamageEffect damage)
        {
            string scalingText = GetScalingText(damage.attackPowerScaling, damage.spellPowerScaling);
            return $"Causa <b>{damage.baseValue}</b> de dano {damage.damageType}{scalingText}.";
        }
        if (effect is HealEffect heal)
        {
            string scalingText = GetScalingText(0, heal.spellPowerScaling);
            return $"Restaura <b>{heal.baseValue}</b> de Vida{scalingText}.";
        }
        if (effect is ApplyStatusEffect applyStatus)
        {
            var status = applyStatus.statusEffectToApply;
            if (status == null) return "";
            string effectText = status.isBuff ? $"Aplica {status.effectName}, que " : $"Aflige o alvo com {status.effectName}, que ";
            var modifierDescriptions = new List<string>();
            foreach (var mod in status.statModifiers)
            {
                string sign = mod.value > 0 ? "+" : "";
                modifierDescriptions.Add($"{GetStatName(mod.targetStat)} em <b>{sign}{mod.value}</b>");
            }
            effectText += string.Join(", ", modifierDescriptions);
            effectText += $", por {status.duration} segundos.";
            return effectText;
        }
        return "Efeito desconhecido.";
    }

    private static string GetScalingText(float apScaling, float spScaling)
    {
        var scalingParts = new List<string>();
        if (apScaling > 0) scalingParts.Add($"<color=#FF8C00>{apScaling * 100:F0}%</color> do Poder de Ataque");
        if (spScaling > 0) scalingParts.Add($"<color=#87CEFA>{spScaling * 100:F0}%</color> do Poder de Magia");
        return scalingParts.Count > 0 ? $" (+ {string.Join(" e ", scalingParts)})" : "";
    }

    private static string GetStatName(StatType stat)
    {
        switch (stat)
        {
            case StatType.Strength: return "Força";
            case StatType.Agility: return "Agilidade";
            case StatType.Intellect: return "Intelecto";
            case StatType.Stamina: return "Vigor";
            case StatType.Armor: return "Armadura";
            case StatType.CriticalStrikeRating: return "Acerto Crítico";
            case StatType.HasteRating: return "Aceleração";
            case StatType.MasteryRating: return "Maestria";
            case StatType.MovementSpeed: return "Velocidade de Movimento";
            case StatType.Leech: return "Roubo de Vida";
            case StatType.Avoidance: return "Evasão";
            case StatType.Health: return "Vida";
            case StatType.Mana: return "Mana";
            case StatType.AttackPower: return "Poder de Ataque";
            case StatType.SpellPower: return "Poder Mágico";
            case StatType.CriticalStrikeChance: return "Chance de Crítico";
            case StatType.Haste: return "Aceleração Efetiva";
            default: return stat.ToString();
        }
    }

    private static string GetEquipmentSlotName(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.MainHand: return "Mão Principal";
            case EquipmentSlot.OffHand: return "Mão Secundária";
            case EquipmentSlot.Head: return "Cabeça";
            case EquipmentSlot.Chest: return "Peitoral";
            case EquipmentSlot.Legs: return "Pernas";
            case EquipmentSlot.Feet: return "Pés";
            case EquipmentSlot.Hands: return "Mãos";
            case EquipmentSlot.Cloak: return "Capa";
            default: return slot.ToString();
        }
    }

    private static string GetWeaponTypeName(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Sword1H: return "Espada de 1 Mão";
            case WeaponType.Axe1H: return "Machado de 1 Mão";
            case WeaponType.Mace1H: return "Maça de 1 Mão";
            case WeaponType.Dagger: return "Adaga";
            case WeaponType.Fist: return "Soqueira";
            case WeaponType.Sword2H: return "Espada de 2 Mãos";
            case WeaponType.Axe2H: return "Machado de 2 Mãos";
            case WeaponType.Mace2H: return "Maça de 2 Mãos";
            case WeaponType.Polearm: return "Lança";
            case WeaponType.Staff: return "Cajado";
            case WeaponType.Bow: return "Arco";
            case WeaponType.Crossbow: return "Besta";
            case WeaponType.Gun: return "Arma de Fogo";
            default: return type.ToString();
        }
    }

    private static string GetWeaponRequirementName(WeaponRequirement requirement)
    {
        switch (requirement)
        {
            case WeaponRequirement.None: return "";
            case WeaponRequirement.WeaponRequired: return "Qualquer Arma";
            case WeaponRequirement.MeleeWeapon: return "Arma Corpo a Corpo";
            case WeaponRequirement.RangedWeapon: return "Arma de Longo Alcance";
            case WeaponRequirement.Unarmed: return "Desarmado";
            default: return requirement.ToString();
        }
    }

    private static Color GetQualityColor(ItemQuality quality)
    {
        switch (quality)
        {
            case ItemQuality.Uncommon: return Color.green;
            case ItemQuality.Rare: return Color.blue;
            case ItemQuality.Epic: return new Color(0.5f, 0, 1);
            case ItemQuality.Legendary: return new Color(1, 0.5f, 0);
            default: return Color.white;
        }
    }

    #endregion
}