// UI/UI_SpellBook.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class UI_SpellBook : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("O objeto pai onde os slots de habilidade serão criados.")]
    [SerializeField] private Transform slotsParent;

    [Tooltip("O prefab para um único slot (pode ser o mesmo do inventário).")]
    [SerializeField] private GameObject slotPrefab;

    [Tooltip("O prefab do ícone arrastável (o mesmo do inventário/barra de ações).")]
    [SerializeField] private GameObject draggablePrefab;

    private List<UI_Slot> _uiSlots = new List<UI_Slot>();

    // O Spell Book é ativado/desativado. Usamos OnEnable para garantir que ele sempre se atualize.
    private void OnEnable()
    {
        PopulateSpellBook();
    }

    /// <summary>
    /// Limpa e preenche o livro de habilidades. Mostra todas as habilidades da classe
    /// e aplica um feedback visual para as que ainda não foram aprendidas.
    /// </summary>
    private void PopulateSpellBook()
    {
        if (slotsParent == null || slotPrefab == null || draggablePrefab == null) return;
        if (LocalPlayerData.Instance == null || GameDatabase.Instance == null) return;

        // 2. Limpa os slots visuais antigos
        foreach (Transform child in slotsParent) Destroy(child.gameObject);
        _uiSlots.Clear();


        // 3. Obtém os dados necessários a partir dos Singletons
        string playerClassID = LocalPlayerData.Instance.ClassID;
        // UnityEngine.Debug.Log($"Populating Spell Book for class ID: {playerClassID}");
        if (string.IsNullOrEmpty(playerClassID)) return;

        PlayerClass playerClassData = GameDatabase.Instance.GetClass(playerClassID);
        if (playerClassData == null) return;

        // Pega a lista de IDs de habilidades conhecidas
        List<string> knownAbilityIDs_List = LocalPlayerData.Instance.KnownAbilityIDs;
        HashSet<string> knownAbilityIDs = new HashSet<string>(knownAbilityIDs_List ?? new List<string>());

        // Pega todas as habilidades possíveis da classe
        List<AbilityUnlockEntry> allPossibleEntries = playerClassData.GetAllPossibleAbilityEntries();

        // 4. Itera sobre TODAS as habilidades possíveis da classe
        foreach (AbilityUnlockEntry entry in allPossibleEntries.OrderBy(e => e.levelRequired))
        {
            Ability ability = entry.ability;
            if (ability == null || ability.abilityType == AbilityType.Passive) continue;

            // Cria o slot e o ícone
            GameObject slotGO = Instantiate(slotPrefab, slotsParent);
            UI_Slot newSlot = slotGO.GetComponent<UI_Slot>();
            if (newSlot == null) continue;

            newSlot.contentType = SlotContentType.SpellBook;
            _uiSlots.Add(newSlot);

            GameObject draggableGO = Instantiate(draggablePrefab, newSlot.itemAnchor);
            DraggableUIItem draggable = draggableGO.GetComponent<DraggableUIItem>();
            draggable.SetAbility(ability, entry.levelRequired);
        }
    }
}