// Cliente/Scripts/Player/CameraController.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms;

public class CameraController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReader inputReader;

    [Header("Target & Focus")]
    [SerializeField] private Transform minemap;
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 focusOffset = new Vector3(0, 1.5f, 0);

    [Header("Zoom & Rotation")]
    [SerializeField] private float minDistance = 2.0f;
    [SerializeField] private float maxDistance = 15.0f;
    [SerializeField] private float zoomSpeed = 10.0f;

    [Header("Rotation")] // (Opcional) Movi rotationSpeed para sua própria categoria para clareza
    [Tooltip("Velocidade base da rotação. Será multiplicada pela sensibilidade definida pelo jogador.")]
    [SerializeField] private float rotationSpeed = 2.0f;

    [Header("Pitch Clamping")]
    [SerializeField] private float minPitch = -40.0f;
    [SerializeField] private float maxPitch = 85.0f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayerMask;
    [SerializeField] private float collisionCushion = 0.35f;

    private float currentDistance = 10.0f;
    private float yaw = 0.0f;
    private float pitch = 20.0f;

    // --- (MUDANÇA) Variáveis de estado separadas para cada botão do mouse ---
    private Vector2 _lookInput;
    private float _zoomScroll;
    private bool _isLMBPressed = false; // Estado para o botão esquerdo (só câmera)
    private bool _isRMBPressed = false; // Estado para o botão direito (câmera + personagem)

    // (REMOVIDO) Não precisamos mais expor isso. O PlayerController tem sua própria lógica agora.
    // public bool IsRightMouseButtonPressed { get; private set; }

    private void OnEnable()
    {
        if (inputReader == null) return;

        inputReader.LookEvent += OnLook;
        inputReader.ZoomEvent += OnZoom;
        inputReader.RotateCameraEvent += OnRotateCamera; // Evento do Botão Esquerdo
        inputReader.RotatePlayerAndCameraEvent += OnRotatePlayerAndCamera; // Evento do Botão Direito
    }

    private void OnDisable()
    {
        if (inputReader == null) return;

        inputReader.LookEvent -= OnLook;
        inputReader.ZoomEvent -= OnZoom;
        inputReader.RotateCameraEvent -= OnRotateCamera;
        inputReader.RotatePlayerAndCameraEvent -= OnRotatePlayerAndCamera;
    }

    public void SetTarget(Transform newTarget)
    {
        // Debug.Log($"Changing Camera to {newTarget.name}");

        target = newTarget;
        if (target != null)
        {
            yaw = target.eulerAngles.y;
            pitch = 20f;
        }
    }


    private void LateUpdate()
    {
        // Adicione uma verificação de segurança para o caso do SettingsManager não estar pronto ainda
        if (target == null || SettingsManager.Instance == null) return;

        bool isPointerOverUI = EventSystem.current.IsPointerOverGameObject() || DraggableUIItem.IsDraggingItem;

        // --- LÓGICA DE ZOOM (sem alterações) ---
        if (!isPointerOverUI && Mathf.Abs(_zoomScroll) > 0.1f)
        {
            currentDistance -= _zoomScroll * zoomSpeed * Time.deltaTime;
        }
        _zoomScroll = 0f;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        // --- (MUDANÇA) LÓGICA DE ROTAÇÃO APLICANDO A SENSIBILIDADE ---
        bool shouldRotateCamera = (_isLMBPressed || _isRMBPressed) && !isPointerOverUI;

        if (shouldRotateCamera)
        {
            ConfineCursorAndHide();

            // Aqui está a mágica: multiplicamos pela sensibilidade do SettingsManager
            float currentRotationSpeed = rotationSpeed * SettingsManager.Instance.CameraSensitivity;

            yaw += _lookInput.x * currentRotationSpeed;
            pitch -= _lookInput.y * currentRotationSpeed;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
        else
        {
            EnsureCursorIsUnlockedAndVisible();
        }
        _lookInput = Vector2.zero;

        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        if (target == null) return;
        Vector3 pivotPoint = target.position + focusOffset;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 direction = rotation * Vector3.forward;
        Vector3 desiredPosition = pivotPoint - direction * currentDistance;

        if (Physics.SphereCast(pivotPoint, collisionCushion, -direction, out RaycastHit hit, currentDistance, collisionLayerMask))
        {
            transform.position = hit.point + hit.normal * collisionCushion;
        }
        else
        {
            transform.position = desiredPosition;
        }

        minemap.position = new Vector3(target.position.x, target.position.y + 100, target.position.z);

        transform.LookAt(pivotPoint);
    }

    // --- Handlers de Evento ---
    private void OnLook(Vector2 lookInput) { _lookInput = lookInput; }
    private void OnZoom(float zoomValue) { _zoomScroll = Mathf.Sign(zoomValue); }

    // (MUDANÇA) Handler para o botão esquerdo
    private void OnRotateCamera(bool isPressed)
    {
        _isLMBPressed = isPressed;
    }

    // (MUDANÇA) Handler para o botão direito
    private void OnRotatePlayerAndCamera(bool isPressed)
    {
        _isRMBPressed = isPressed;
        // A propriedade pública foi removida, a comunicação agora é via eventos para o PlayerController
    }

    private void ConfineCursorAndHide()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }

    private void EnsureCursorIsUnlockedAndVisible()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}