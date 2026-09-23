// Scripts/UI/TargetingReticle.cs
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TargetingReticle : MonoBehaviour
{
    public static TargetingReticle Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private InputReader inputReader; // (NOVO) Arraste seu asset InputReader aqui

    [Header("Config")]
    [SerializeField] private LayerMask groundLayer;

    public bool IsActive { get; private set; } = false;

    private Action<Vector3> _onPositionConfirmed;
    private Camera _mainCamera;
    private bool _confirmRequested = false;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _mainCamera = Camera.main;
    }

    // (NOVO) Usa OnEnable e OnDisable para se inscrever nos eventos do InputReader
    private void OnEnable()
    {
        if (inputReader == null)
        {
            // Debug.LogError("InputReader não está atribuído no TargetingReticle!", this);
            return;
        }

        // Inscreve-se nos eventos de clique e cancelamento
        inputReader.TargetClickEvent += OnConfirmInput;
        inputReader.ClearTargetEvent += OnCancelInput;
        inputReader.GameMenuEvent += OnCancelInput; // Esc também cancela
    }

    private void OnDisable()
    {
        if (inputReader == null) return;

        inputReader.TargetClickEvent -= OnConfirmInput;
        inputReader.ClearTargetEvent -= OnCancelInput;
        inputReader.GameMenuEvent -= OnCancelInput;
    }

    private void Start()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsActive) return;

        Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer))
        {
            transform.position = hit.point;
        }

        if (_confirmRequested)
        {
            if (!EventSystem.current.IsPointerOverGameObject() && !DraggableUIItem.IsDraggingItem)
            {
                ConfirmPosition();
            }
            _confirmRequested = false;
        }
    }

    // --- (MUDANÇA) Handlers de Eventos agora são privados e sem parâmetros ---

    private void OnConfirmInput()
    {
        if (!IsActive) return;
        // Apenas levanta a bandeira. O Update() fará o resto.
        _confirmRequested = true;
    }

    private void OnCancelInput()
    {
        if (!IsActive) return;
        // Chama o método de cancelamento diretamente.
        Cancel();
    }

    // --- Lógica Pública (sem alterações) ---

    public void BeginTargeting(Action<Vector3> onConfirmCallback)
    {
        _onPositionConfirmed = onConfirmCallback;
        gameObject.SetActive(true);
        _confirmRequested = false;
        IsActive = true;
        // Debug.Log("<color=cyan>TargetingReticle ativado.</color>");
    }

    private void ConfirmPosition()
    {
        // Debug.Log($"<color=green>Posição confirmada em: {transform.position}.</color>");
        _onPositionConfirmed?.Invoke(transform.position);
        FinishTargeting();
    }

    public void Cancel()
    {
        // Debug.Log("<color=orange>Targeting cancelado.</color>");
        FinishTargeting();
    }

    private void FinishTargeting()
    {
        _onPositionConfirmed = null;
        IsActive = false;
        gameObject.SetActive(false);
    }
}