// Cliente/Scripts/SpawnPointVisualizer.cs
using UnityEngine;
using System.Collections.Generic;

public class SpawnPointVisualizer : MonoBehaviour
{
    [Header("Configuração do Spawn")]
    [Tooltip("Arraste aqui o ScriptableObject (NpcData) do NPC que deve nascer neste ponto.")]
    public NpcData NpcToSpawn;

    [Tooltip("Quantos NPCs vão nascer neste ponto")]
    [Min(1)]
    public int Quantity = 1;

    [Tooltip("O raio em volta do ponto central onde os NPCs podem aparecer")]
    [Min(0)]
    public float SpawnRadius = 5.0f;

    // =================================================================================
    // NOVAS CONFIGURAÇÕES DE COMPORTAMENTO
    // =================================================================================
    [Header("Comportamento & Posição")]
    [Tooltip("Define como o NPC se comportará ao nascer neste ponto.")]
    public NpcAiType AiType = NpcAiType.Passive_Aggressive; // Valor padrão

    [Tooltip("A rotação inicial que o NPC terá ao nascer. Útil para guardas.")]
    public Vector3 InitialRotation = Vector3.zero;


    [Header("Patrulha (Opcional)")]
    [Tooltip("Lista de pontos para o NPC patrulhar. A posição deste objeto é o ponto inicial.")]
    public List<Transform> PatrolPathTransforms = new List<Transform>();


    [HideInInspector]
    public GameObject previewInstance;


    private void OnDrawGizmos()
    {
        // ... (o código de OnDrawGizmos não precisa de nenhuma alteração) ...
        // Se um NPC for selecionado, usa uma cor baseada na sua facção
        if (NpcToSpawn != null)
        {
            switch (NpcToSpawn.faction)
            {
                case NpcFaction.Enemy:
                    Gizmos.color = new Color(1, 0, 0, 0.3f); // Vermelho
                    break;
                case NpcFaction.Friendly:
                    Gizmos.color = new Color(0, 1, 0, 0.3f); // Verde
                    break;
                case NpcFaction.Neutral:
                    Gizmos.color = new Color(1, 0.92f, 0.016f, 0.3f); // Amarelo
                    break;
            }
        }
        else
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Cinza se nada for selecionado
        }

        // --- Desenha o Raio de Spawn ---
        Gizmos.DrawSphere(transform.position, SpawnRadius);
        Gizmos.DrawWireSphere(transform.position, SpawnRadius);

        // --- Desenha o Caminho da Patrulha ---
        if (PatrolPathTransforms != null && PatrolPathTransforms.Count > 0)
        {
            Gizmos.color = Color.cyan;
            Vector3 startPoint = transform.position;
            Vector3 previousPoint = startPoint;

            foreach (var pointTransform in PatrolPathTransforms)
            {
                if (pointTransform != null)
                {
                    Gizmos.DrawLine(previousPoint, pointTransform.position);
                    previousPoint = pointTransform.position;
                }
            }
        }

        // --- NOVO: Desenha a direção da Rotação Inicial ---
        Gizmos.color = Color.blue;
        Vector3 direction = Quaternion.Euler(InitialRotation) * Vector3.forward;
        Gizmos.DrawLine(transform.position, transform.position + direction * 2);
    }
}