// Cliente/Data/Abilities/AbilityEffect.cs (NOVO ARQUIVO)
using UnityEngine;

// Define a "forma" que todo efeito de habilidade deve ter.
// Não contém lógica, apenas dados para o editor e para o exportador.
public abstract class AbilityEffect : ScriptableObject
{
    [TextArea(1, 3)]
    public string description; // Um campo para descrever o que o efeito faz no editor.
}