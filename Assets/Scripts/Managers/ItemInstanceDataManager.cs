// ARQUIVO COMPLETO E CORRIGIDO: Managers/ItemInstanceDataManager.cs

using System.Collections.Generic;
using UnityEngine;

public class ItemInstanceDataManager : MonoBehaviour
{
    private static ItemInstanceDataManager _instance;
    public static ItemInstanceDataManager Instance { get { if (_instance == null) { _instance = FindFirstObjectByType<ItemInstanceDataManager>(); if (_instance == null) { GameObject go = new GameObject("ItemInstanceDataManager"); _instance = go.AddComponent<ItemInstanceDataManager>(); } } return _instance; } }

    private readonly Dictionary<string, ItemInstanceData> _instanceData = new();

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StoreItemData(string instanceId, ItemInstanceData data)
    {
        // Adiciona uma verificação aqui também, por segurança.
        if (string.IsNullOrEmpty(instanceId) || data == null) return;
        _instanceData[instanceId] = data;
    }

    public ItemInstanceData GetData(string instanceId)
    {
        // <<< A CORREÇÃO PRINCIPAL ESTÁ AQUI >>>
        // Se o instanceId for nulo ou vazio (como o de um item de loja),
        // simplesmente retorna nulo. Não tenta procurar no dicionário.
        if (string.IsNullOrEmpty(instanceId))
        {
            return null;
        }

        _instanceData.TryGetValue(instanceId, out var data);
        return data;
    }

    // Por consistência, vamos remover o método antigo GetStats que está obsoleto
    // para evitar confusão no futuro. O GetData já faz tudo que precisamos.
    /*
    public List<ItemBaseStatUnity> GetStats(string instanceId)
    {
        // ... (código obsoleto)
    }
    */

    public void ClearData()
    {
        _instanceData.Clear();
    }
}