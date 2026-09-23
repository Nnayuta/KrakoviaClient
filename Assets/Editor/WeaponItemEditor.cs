// Scripts/Editor/WeaponItemEditor.cs
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponItem))]
public class WeaponItemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Desenha o inspector padrão
        base.OnInspectorGUI();

        // Pega uma referência ao WeaponItem que estamos editando
        WeaponItem weapon = (WeaponItem)target;

        // Adiciona um espaço para separar
        EditorGUILayout.Space(10);

        // Adiciona um botão grande e chamativo
        if (GUILayout.Button("Gerar/Atualizar Dano & Preço", GUILayout.Height(40)))
        {
            if (weapon.itemLevel > 0)
            {
                // Chama nosso novo método do StatAllocator
                var (min, max) = StatAllocator.GenerateWeaponDamage(weapon);

                // Atualiza os valores no ScriptableObject
                weapon.minDamage = min;
                weapon.maxDamage = max;

                // Também atualiza o preço de venda
                weapon.sellPrice = StatAllocator.CalculateSellPrice(weapon);

                // Marca o objeto como "sujo" para que o Unity saiba que precisa salvar as alterações
                EditorUtility.SetDirty(weapon);

                Debug.Log($"Dano para '{weapon.name}' atualizado para {min}-{max} (iLvl {weapon.itemLevel})");
            }
            else
            {
                Debug.LogWarning("O Item Level precisa ser maior que 0 para gerar os stats.");
            }
        }
    }
}