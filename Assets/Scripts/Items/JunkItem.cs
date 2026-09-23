// Cliente/Scripts/Items/JunkItem.cs
using UnityEngine;

/// <summary>
/// Representa um item "lixo" ou "material de troca".
/// Sua principal função é ser coletado e vendido. Ele não tem
/// funcionalidades extras como ser equipado ou consumido.
/// </summary>
[CreateAssetMenu(fileName = "New Junk Item", menuName = "RPG/Items/Junk Item")]
public class JunkItem : Item
{
    // Intencionalmente vazio.
    // Esta classe herda tudo o que precisa da classe base 'Item':
    // - itemID
    // - itemName
    // - icon
    // - quality
    // - maxStackSize (geralmente alto para esses itens)
    //
    // A única razão de esta classe existir é para nos ajudar a organizar
    // e identificar este tipo de item no código e no editor.
}