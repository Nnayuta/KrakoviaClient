// Scripts/GM/GmManager.cs
using UnityEngine;

public class GmManager : MonoBehaviour
{
    public static GmManager Instance { get; private set; }

    private int requiredPermissionLevel = 50;
    [Header("Configurações de GM")]
    [SerializeField] private float noclipSpeed = 20f;

    // Estado interno
    private bool isGmActive = false;
    private bool isNoclipActive = false;

    // Referências
    private InputReader inputReader;
    private CharacterController playerController;
    private Transform playerTransform;
    private Transform cameraTransform;

    // Variáveis de input do GM
    private Vector2 noclipMoveInput;
    private bool isFlyingUp;
    private bool isFlyingDown;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.OnPlayerDataInitialized += HandlePlayerDataInitialized;
            LocalPlayerData.Instance.OnDataReset += DeactivateGmTools;
        }
    }

    private void OnDestroy()
    {
        if (LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.OnPlayerDataInitialized -= HandlePlayerDataInitialized;
            LocalPlayerData.Instance.OnDataReset -= DeactivateGmTools;
        }
        UnsubscribeFromInput();
    }

    private void HandlePlayerDataInitialized()
    {
        // Debug.Log("SISTEMA DE GM");
        if (LocalPlayerData.Instance.PermissionLevel >= requiredPermissionLevel) ActivateGmTools();
        else DeactivateGmTools();
    }

    private void ActivateGmTools()
    {
        isGmActive = true;
        PlayerController pc = UDPClient.Instance.MyPlayerObject.GetComponentInChildren<PlayerController>(true);
        if (pc != null)
        {
            // <<< CORREÇÃO APLICADA AQUI >>>
            playerTransform = pc.transform.parent;
            playerController = playerTransform.GetComponent<CharacterController>();
            cameraTransform = Camera.main.transform;

            if (playerController != null)
            {
                SubscribeToInput();
                // Debug.Log($"<color=cyan>[GmManager] Ferramentas de GM ATIVADAS para {LocalPlayerData.Instance.CharacterName}.</color>");
            }
            else
            {
                // Debug.LogError("[GmManager] ERRO CRÍTICO: Não foi possível encontrar o CharacterController no objeto pai do PlayerController!");
                isGmActive = false;
            }
        }
        else
        {
            // Debug.LogError("[GmManager] Não foi possível encontrar o PlayerController no mundo!");
            isGmActive = false;
        }
    }

    private void DeactivateGmTools()
    {
        if (!isGmActive) return;
        if (isNoclipActive) ToggleNoclip();
        UnsubscribeFromInput();
        isGmActive = false;
        playerController = null;
        playerTransform = null;
        cameraTransform = null;
        // Debug.Log("<color=yellow>[GmManager] Ferramentas de GM Desativadas.</color>");
    }

    private void Update()
    {
        if (isNoclipActive)
        {
            HandleNoclipMovement();
        }
    }

    public void ToggleNoclip()
    {
        if (!isGmActive) return;
        isNoclipActive = !isNoclipActive;

        if (playerController != null)
        {
            playerController.enabled = !isNoclipActive;
        }
        else
        {
            // Debug.LogError("[GmManager] Tentou alternar o Noclip, mas a referência ao CharacterController é NULA!");
            return; // Sai da função para evitar problemas
        }

        if (!isNoclipActive)
        {
            noclipMoveInput = Vector2.zero;
            isFlyingUp = false;
            isFlyingDown = false;
        }

        // Debug.Log($"<color=cyan>[GmManager] Noclip {(isNoclipActive ? "ATIVADO" : "DESATIVADO")}.</color>");
    }

    private void HandleNoclipMovement()
    {
        if (playerTransform == null || cameraTransform == null) return;

        Vector3 moveDirection = cameraTransform.forward * noclipMoveInput.y + cameraTransform.right * noclipMoveInput.x;
        if (isFlyingUp) moveDirection.y = 1f;
        else if (isFlyingDown) moveDirection.y = -1f;

        playerTransform.position += moveDirection.normalized * noclipSpeed * Time.deltaTime;
    }

    #region Input Subscription

    private void SubscribeToInput()
    {
        if (inputReader != null) return;
        if (UIManager.Instance != null)
        {
            inputReader = UIManager.Instance.GetInputReader();
            if (inputReader != null)
            {
                inputReader.MoveEvent += OnNoclipMove;
                inputReader.ToggleNoclipEvent += ToggleNoclip;
                inputReader.GmFlyUpEvent += OnFlyUp;
                inputReader.GmFlyDownEvent += OnFlyDown;
            }
        }
    }

    private void UnsubscribeFromInput()
    {
        if (inputReader != null)
        {
            inputReader.MoveEvent -= OnNoclipMove;
            inputReader.ToggleNoclipEvent -= ToggleNoclip;
            inputReader.GmFlyUpEvent -= OnFlyUp;
            inputReader.GmFlyDownEvent -= OnFlyDown;
            inputReader = null;
        }
    }

    private void OnNoclipMove(Vector2 movement) { noclipMoveInput = movement; }
    private void OnFlyUp(bool isPressed) { isFlyingUp = isPressed; }
    private void OnFlyDown(bool isPressed) { isFlyingDown = isPressed; }

    #endregion

    private void OnGUI()
    {
        if (!isGmActive) return;
        GUI.color = Color.cyan;
        GUI.Label(new Rect(10, 10, 200, 40), "--- GM TOOLS ---");
        string noclipStatus = isNoclipActive ? "<color=green>ON</color>" : "<color=red>OFF</color>";
        GUI.Label(new Rect(10, 30, 200, 20), $"Noclip (F11): {noclipStatus}");
    }
}