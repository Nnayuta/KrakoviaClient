// Cliente/Scripts/Editor/GatherableSpawnPointVisualizer.cs
using UnityEngine;

public class GatherableSpawnPointVisualizer : MonoBehaviour
{
    [Header("Configuração do Coletável")]
    [Tooltip("Arraste aqui o ScriptableObject (GatherableData) do item que deve nascer neste ponto.")]
    public GatherableData GatherableToSpawn;

    [Tooltip("O raio em volta do ponto central onde o item pode reaparecer.")]
    [Min(0)]
    public float SpawnRadius = 0.5f;

    [Tooltip("A rotação inicial aleatória que o item pode ter. Útil para plantas não parecerem todas iguais.")]
    public Vector3 InitialRotation = Vector3.zero;

    // Gizmos para visualização no Editor
    private void OnDrawGizmos()
    {
        // Usa uma cor padrão para coletáveis
        Gizmos.color = new Color(0.5f, 0, 1, 0.4f); // Roxo

        // Desenha uma esfera sólida para representar a área
        Gizmos.DrawSphere(transform.position, SpawnRadius);

        // Desenha uma esfera de arame para delimitar
        Gizmos.color = new Color(0.5f, 0, 1, 0.8f);
        Gizmos.DrawWireSphere(transform.position, SpawnRadius);

        // Desenha uma linha para indicar a rotação base
        Gizmos.color = Color.cyan;
        Vector3 direction = Quaternion.Euler(InitialRotation) * Vector3.forward;
        Gizmos.DrawLine(transform.position, transform.position + direction * 1.5f);
    }
}