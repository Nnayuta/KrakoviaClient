// Cliente/Scripts/NPC/NpcCustomizer.cs
using UnityEngine;

[RequireComponent(typeof(CharacterCustomizer))]
public class NpcCustomizer : MonoBehaviour
{
    // A lista de equipamentos e as configurações de gênero foram REMOVIDAS daqui.

    private CharacterCustomizer _customizer;

    void Awake()
    {
        _customizer = GetComponent<CharacterCustomizer>();
    }

    /// <summary>
    /// Chamado para configurar a aparência completa do NPC usando um NpcData.
    /// </summary>
    /// <param name="data">O ScriptableObject que contém todas as informações do NPC.</param>
    public void InitializeFromData(NpcData data)
    {
        if (_customizer == null)
        {
            Debug.LogError("NpcCustomizer não encontrou o componente CharacterCustomizer!", this);
            return;
        }
        if (data == null)
        {
            Debug.LogError("NpcData fornecido para o NpcCustomizer é nulo!", this);
            return;
        }

        // 1. Define o gênero com base na configuração do NpcData.
        bool isFemale;
        switch (data.genderSetting)
        {
            case NpcGenderSetting.ForceMale:
                isFemale = false;
                break;
            case NpcGenderSetting.ForceFemale:
                isFemale = true;
                break;
            case NpcGenderSetting.Random:
            default:
                isFemale = (UnityEngine.Random.Range(0, 2) == 0);
                break;
        }

        // 2. Cria e aplica a aparência base (gênero, cor de pele padrão, etc.)
        var baseAppearance = new CharacterAppearance { IsFemale = isFemale };
        _customizer.ApplyAppearance(baseAppearance);

        // 3. Aplica os equipamentos visuais a partir da lista DENTRO do NpcData.
        _customizer.UpdateEquipmentVisualsForNpc(data.DefaultEquipment);
    }
}