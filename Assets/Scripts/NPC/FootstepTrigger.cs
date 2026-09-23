using UnityEngine;
public class FootstepsModule : MonoBehaviour
{
    public void Footsteps()
    {
        try
        {
            // O Raycast continua sendo a base de tudo
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 0.3f))
            {
                SurfaceType detectedSurface = null;

                // 1. Prioridade 1: O objeto atingido é um Terreno?
                if (hit.collider.TryGetComponent<TerrainSurfaceMap>(out var terrainMap))
                {
                    // Sim! Pergunta ao mapa qual é a superfície no ponto de impacto.
                    detectedSurface = terrainMap.GetSurfaceAt(hit.point);
                }
                // 2. Prioridade 2: Se não for um terreno, ele tem um SurfaceIdentifier?
                else if (hit.collider.TryGetComponent<SurfaceIdentifier>(out var surfaceIdentifier))
                {
                    // Sim! Usa o tipo de superfície definido nele.
                    detectedSurface = surfaceIdentifier.surfaceType;
                }

                // 3. Toca o som (a superfície detectada ou o padrão se nada foi encontrado)
                FootstepManager.Instance.PlayFootstep(detectedSurface);
            }
            else
            {
                // O raio não atingiu nada (pulando).
                FootstepManager.Instance.PlayFootstep(null);
            }
        }
        catch (System.Exception)
        {
            // Debug.Log("Erro no footstep");
        }
    }
}