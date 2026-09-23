// Cliente/Scripts/Player/PlayerController.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Dependencies")]
    private InputReader inputReader;

    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 7.0f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20.0f;
    // [SerializeField] private float turnSmoothTime = 0.1f;

    [Header("Ground Check Settings")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("A altura do centro da caixa de detecção, a partir da base do personagem.")]
    [SerializeField] private float boxCastCenterY = 0.5f;
    [Tooltip("A distância que a caixa irá 'cair' para detectar o chão.")]
    [SerializeField] private float boxCastDistance = 0.6f;
    [Tooltip("O tamanho da caixa de detecção nos eixos X e Z.")]
    [SerializeField] private Vector3 boxCastSize = new Vector3(0.8f, 0.1f, 0.8f);

    // --- Referências ---
    private Animator animator;
    private CharacterController controller;
    private Transform cameraMainTransform;
    private CombatController combatController;
    private NetworkCharacter networkCharacter;
    private Transform rootTransform;

    // --- Estado Interno ---
    private Vector2 moveInput;
    private Vector3 playerVelocity;
    private bool _isInputInitialized = false;
    private bool _isRMBPressed = false;
    private float turnSmoothVelocity;
    private bool isGrounded;

    // >>> NOVA VARIÁVEL AQUI <<<
    private Vector3 _horizontalVelocity; // Armazena a velocidade horizontal para ser usada no ar

    private void Awake()
    {
        rootTransform = transform.parent;
        controller = rootTransform.GetComponent<CharacterController>();
        animator = rootTransform.GetComponentInChildren<Animator>();
        combatController = rootTransform.GetComponent<CombatController>();
        networkCharacter = rootTransform.GetComponent<NetworkCharacter>();

        if (Camera.main != null && networkCharacter.IsLocalPlayer)
        {
            cameraMainTransform = Camera.main.transform;
            var cameraController = cameraMainTransform.GetComponent<CameraController>();
            if (cameraController != null) cameraController.SetTarget(rootTransform);
        }

        if (UIManager.Instance != null && networkCharacter.IsLocalPlayer)
        {
            inputReader = UIManager.Instance.GetInputReader();
        }
    }

    // >>> MÉTODO UPDATE SUBSTITUÍDO <<<
    private void Update()
    {
        if (controller == null || !controller.enabled && networkCharacter.IsLocalPlayer) return;

        // Ground Check (sem alterações)
        Vector3 boxCenter = transform.position + Vector3.up * boxCastCenterY;
        isGrounded = Physics.BoxCast(boxCenter, boxCastSize / 2, Vector3.down, transform.rotation, boxCastDistance, groundMask);

        // Se estiver no chão...
        if (isGrounded)
        {
            // Reseta a velocidade vertical para evitar que a gravidade se acumule
            if (playerVelocity.y < 0)
            {
                playerVelocity.y = -2f;
            }

            // Calcula a velocidade horizontal baseada no input do jogador
            float speedMultiplier = (networkCharacter != null) ? networkCharacter.MovementSpeed / 100f : 1f;
            float currentMoveSpeed = baseMoveSpeed * speedMultiplier;
            Vector3 moveDirection;

            if (_isRMBPressed)
            {
                // MODO AÇÃO (BOTÃO DIREITO)
                Vector3 camForward = cameraMainTransform.forward;
                Vector3 camRight = cameraMainTransform.right;
                camForward.y = 0; camRight.y = 0;
                camForward.Normalize(); camRight.Normalize();
                moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

                // Rotação
                if (moveDirection != Vector3.zero)
                {
                    float targetAngle = cameraMainTransform.eulerAngles.y;
                    rootTransform.rotation = Quaternion.Slerp(rootTransform.rotation, Quaternion.Euler(0f, targetAngle, 0f), rotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                // MODO PADRÃO
                moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
                moveDirection = rootTransform.TransformDirection(moveDirection).normalized;

                // Penalidade de backpedal
                if (moveInput.y < 0)
                {
                    currentMoveSpeed *= 0.6f;
                }
            }

            // Armazena a velocidade horizontal calculada
            _horizontalVelocity = moveDirection * currentMoveSpeed;
        }

        // Aplica a gravidade constantemente
        playerVelocity.y += gravity * Time.deltaTime;

        // Combina o movimento horizontal (armazenado) com o vertical (gravidade/pulo)
        // e aplica em um único Move(). Isso garante que o movimento aéreo continue.
        Vector3 finalVelocity = _horizontalVelocity;
        finalVelocity.y = playerVelocity.y;
        controller.Move(finalVelocity * Time.deltaTime);

        // --- ANIMAÇÃO (sem alterações) ---
        Vector3 horizontalVelocityForAnim = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        Vector3 localVelocity = rootTransform.InverseTransformDirection(horizontalVelocityForAnim);

        animator.SetFloat("velocityX", localVelocity.x / baseMoveSpeed);
        animator.SetFloat("velocityY", localVelocity.z / baseMoveSpeed);
        animator.SetBool("isGrounded", isGrounded);
    }

    #region Input Handlers e Inicialização

    public void InitializeInputs()
    {
        if (_isInputInitialized || inputReader == null) return;
        inputReader.MoveEvent += OnMove;
        inputReader.JumpEvent += OnJump;
        inputReader.ActionBarEvent += UseActionBarSlot;
        inputReader.RotatePlayerAndCameraEvent += OnRotatePlayerAndCamera;
        _isInputInitialized = true;
    }

    private void OnDestroy()
    {
        if (_isInputInitialized && inputReader != null)
        {
            inputReader.MoveEvent -= OnMove;
            inputReader.JumpEvent -= OnJump;
            inputReader.ActionBarEvent -= UseActionBarSlot;
            inputReader.RotatePlayerAndCameraEvent -= OnRotatePlayerAndCamera;
        }
    }

    private void OnMove(Vector2 movement) { moveInput = movement; }

    private void OnJump()
    {
        if (isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            animator.SetTrigger("jump");
            networkCharacter?.SendJumpTrigger();
        }
    }

    private void OnRotatePlayerAndCamera(bool isPressed) { _isRMBPressed = isPressed; }
    private void UseActionBarSlot(int slotIndex)
    {
        if (combatController != null) combatController.RequestUseActionBarSlot(slotIndex);
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 boxCenter = transform.position + Vector3.up * boxCastCenterY;

        // Desenha o ponto de partida do BoxCast
        Gizmos.DrawWireCube(boxCenter, boxCastSize);

        // Desenha o ponto de chegada do BoxCast
        Gizmos.DrawWireCube(boxCenter + Vector3.down * boxCastDistance, boxCastSize);

        // Desenha uma linha conectando os dois
        Gizmos.DrawLine(boxCenter, boxCenter + Vector3.down * boxCastDistance);
    }
}