// Editor/ItemEditor.cs
using UnityEngine;
using UnityEditor;

// O 'true' no final faz este editor se aplicar a todas as classes que herdam de Item
[CustomEditor(typeof(Item), true)]
public class ItemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Desenha o inspector padrão
        base.OnInspectorGUI();

        // Pega uma referência ao objeto que estamos inspecionando
        Item item = target as Item;

        // Linha divisória para clareza
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("==== Campos Específicos do Tipo ====", EditorStyles.boldLabel);

        // =========================================================
        // AQUI ACONTECE A MÁGICA
        // Verificamos o tipo real do objeto e mostramos um "guia"
        // =========================================================

        if (item is WeaponItem)
        {
            EditorGUILayout.HelpBox("Este é um item do tipo ARMA. Preencha os campos de Prefab, Estilo de Combate e Estatísticas de Arma.", MessageType.Info);
        }
        else if (item is ArmorItem)
        {
            EditorGUILayout.HelpBox("Este é um item do tipo ARMADURA. Preencha os campos de Tipo de Armadura e Valor de Armadura.", MessageType.Info);
        }
        else if (item is ConsumableItem)
        {
            EditorGUILayout.HelpBox("Este é um item do tipo CONSUMÍVEL. Preencha os campos de efeitos (restaurar vida/recurso).", MessageType.Info);
        }

        // No futuro, em vez de HelpBox, você pode desenhar campos customizados aqui
        // se precisar de lógicas mais complexas. Mas por enquanto, a herança já separa
        // os campos para nós!
    }
}