using UnityEngine;
using System.Collections.Generic;

// Classe auxiliar para ser exibida no Inspector do Unity
[System.Serializable]
public class TextureSurfacePair
{
    // A TerrainLayer (que contém a textura de grama, pedra, etc.)
    public TerrainLayer terrainLayer;
    // O SurfaceType (que contém os sons) correspondente
    public SurfaceType surfaceType;
}

[RequireComponent(typeof(Terrain))]
public class TerrainSurfaceMap : MonoBehaviour
{
    [Header("Mapeamento de Texturas para Sons")]
    [Tooltip("Associe cada TerrainLayer usada no seu terreno ao SurfaceType correspondente.")]
    public List<TextureSurfacePair> textureMappings = new List<TextureSurfacePair>();

    private Terrain _terrain;
    private TerrainData _terrainData;

    private void Awake()
    {
        _terrain = GetComponent<Terrain>();
        _terrainData = _terrain.terrainData;
    }

    /// <summary>
    /// Encontra o SurfaceType predominante em uma determinada posição do mundo.
    /// </summary>
    public SurfaceType GetSurfaceAt(Vector3 worldPosition)
    {
        // Converte a posição do mundo para a posição relativa no mapa de texturas (alphamap)
        Vector3 terrainPosition = worldPosition - _terrain.transform.position;
        Vector3 alphamapPosition = new Vector3(
            terrainPosition.x / _terrainData.size.x,
            0,
            terrainPosition.z / _terrainData.size.z
        );

        int alphamapX = (int)(alphamapPosition.x * _terrainData.alphamapWidth);
        int alphamapY = (int)(alphamapPosition.z * _terrainData.alphamapHeight);

        // Pega as "forças" de cada textura naquele ponto
        float[,,] alphamap = _terrainData.GetAlphamaps(alphamapX, alphamapY, 1, 1);

        float strongestMix = 0f;
        int strongestTextureIndex = 0;

        // Itera sobre todas as texturas para encontrar a mais forte
        for (int i = 0; i < alphamap.Length; i++)
        {
            if (alphamap[0, 0, i] > strongestMix)
            {
                strongestMix = alphamap[0, 0, i];
                strongestTextureIndex = i;
            }
        }

        // Se o índice encontrado corresponde a uma TerrainLayer no nosso mapa, retorna o SurfaceType
        if (strongestTextureIndex < _terrainData.terrainLayers.Length)
        {
            TerrainLayer dominantLayer = _terrainData.terrainLayers[strongestTextureIndex];
            foreach (var mapping in textureMappings)
            {
                if (mapping.terrainLayer == dominantLayer)
                {
                    return mapping.surfaceType;
                }
            }
        }

        // Se não encontrar um mapa, retorna nulo
        return null;
    }
}