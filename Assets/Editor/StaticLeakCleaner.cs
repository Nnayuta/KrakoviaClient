using UnityEditor;
using UnityEngine;
using System; // Necessário para System.GC

[InitializeOnLoad]
public class StaticLeakCleaner
{
    static StaticLeakCleaner()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // Nós só nos importamos com o momento em que estamos SAINDO do modo Play
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Debug.Log("<color=orange>[Editor Only] Limpando dados estáticos e forçando Garbage Collection...</color>");

            // --- FASE 1: Limpar todas as referências em listas e dicionários estáticos ---

            // Limpa a lista de Targetables
            if (Targetable.AllTargetables != null)
            {
                Targetable.AllTargetables.Clear();
                Debug.Log("<color=orange>... Lista de Targetables limpa.</color>");
            }

            // Limpa os dados do UDPClient
            if (UDPClient.Instance != null)
            {
                UDPClient.Instance.ResetClient();
                Debug.Log("<color=orange>... Dados do UDPClient resetados.</color>");
            }

            // Limpa os dados do LocalPlayerData
            if (LocalPlayerData.Instance != null)
            {
                // Supondo que você tenha um método de reset. Se não, você precisaria criar um.
                LocalPlayerData.Instance.ResetData();
                Debug.Log("<color=orange>... Dados do LocalPlayerData resetados.</color>");
            }

            // Adicione a limpeza de QUALQUER outro Singleton aqui...
            // Ex: if (QuestManager.Instance != null) QuestManager.Instance.ResetQuests();


            // // --- FASE 2: Forçar o Garbage Collector a rodar ---

            // // Agora que removemos as referências, o GC pode realmente liberar a memória.
            // Debug.Log("<color=orange>... Chamando GC.Collect() para recuperar memória.</color>");
            // GC.Collect();
        }
    }
}