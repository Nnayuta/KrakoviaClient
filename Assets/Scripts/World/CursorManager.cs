// Cliente/Scripts/UI/CursorManager.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    [Header("Cursores")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D interactCursor; // Para NPCs amigáveis/neutros
    [SerializeField] private Texture2D combatCursor;   // Para inimigos
    [SerializeField] private Texture2D lootCursor;     // Para corpos saqueáveis

    [Header("Configuração")]
    [SerializeField] private Vector2 hotspot = Vector2.zero; // Ponto de clique do cursor
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    // Layers que vamos checar com o Raycast
    [Header("Máscaras de Camada")]
    [SerializeField] private LayerMask worldLayers; // Todas as layers do mundo (Enemy, Friendly, Loot, Ground, etc.)

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        // Inicia com o cursor padrão
        SetDefaultCursor();
    }

    // Usamos LateUpdate para garantir que o cursor seja definido após
    // todos os outros eventos do frame, incluindo a movimentação da câmera.
    private void LateUpdate()
    {
        // 1. Prioridade máxima: Se o mouse estiver sobre a UI, usa o cursor padrão e para.
        if (EventSystem.current.IsPointerOverGameObject())
        {
            SetDefaultCursor();
            return;
        }

        // 2. Dispara um raio no mundo do jogo.
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, worldLayers))
        {
            // 3. Verifica a camada do objeto atingido e define o cursor apropriado.
            int hitLayer = hit.collider.gameObject.layer;

            if (hitLayer == LayerMask.NameToLayer("Enemy"))
            {
                SetCombatCursor();
            }
            else if (hitLayer == LayerMask.NameToLayer("Friendly") || hitLayer == LayerMask.NameToLayer("Neutral"))
            {
                SetInteractCursor();
            }
            else if (hitLayer == LayerMask.NameToLayer("Loot") || hitLayer == LayerMask.NameToLayer("Gatherable"))
            {
                SetLootCursor();
            }
            else
            {
                // Se atingiu o chão ou qualquer outra coisa, usa o cursor padrão.
                SetDefaultCursor();
            }
        }
        else
        {
            // Se o raio não atingiu nada no mundo, usa o cursor padrão.
            SetDefaultCursor();
        }
    }

    // --- Métodos Auxiliares para Definir o Cursor ---

    private void SetCursor(Texture2D cursorTexture)
    {
        Cursor.SetCursor(cursorTexture, hotspot, cursorMode);
    }

    public void SetDefaultCursor()
    {
        SetCursor(defaultCursor);
    }

    public void SetInteractCursor()
    {
        SetCursor(interactCursor);
    }

    public void SetCombatCursor()
    {
        SetCursor(combatCursor);
    }

    public void SetLootCursor()
    {
        SetCursor(lootCursor);
    }
}