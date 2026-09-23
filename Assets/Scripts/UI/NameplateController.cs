// Scripts/UI/NameplateController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // Necessário para o Dicionário

[RequireComponent(typeof(Canvas))]
public class NameplateController : MonoBehaviour
{
    [Header("UI refs")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image highlightBorder; // <<< NOVO: A borda que será ativada
    [SerializeField] private TextMeshProUGUI AlertMarker;

    [Header("Config")]
    [Tooltip("Tamanho visual do nameplate (em unidades do mundo). Ajuste até ficar ótimo.")]
    [SerializeField] private float fixedScale = 0.01f;
    [Tooltip("Se > 0, usa esse Y offset (em metros). Se <= 0, calcula automatico.")]
    [SerializeField] private float manualYOffset = 0f;

    [Header("Destaque do Alvo")] // <<< NOVO
    [SerializeField] private Color defaultNameColor = Color.white;
    [SerializeField] private Color highlightedNameColor = Color.yellow;



    private Transform target;
    private Transform camTransform;
    private float computedYOffset = 0.5f;
    private static Transform root;

    private Canvas canvas;
    private CanvasScaler canvasScaler;
    private int permlevel = 0;

    // --- NOVO: Sistema de Registro ---
    private Targetable owner; // Armazena a referência do Targetable a que este nameplate pertence
    private static Dictionary<Targetable, NameplateController> nameplateRegistry = new Dictionary<Targetable, NameplateController>();

    public static NameplateController GetNameplateForTarget(Targetable target)
    {
        nameplateRegistry.TryGetValue(target, out var nameplate);
        return nameplate;
    }
    // --- FIM DO NOVO ---


    private void Awake()
    {
        camTransform = Camera.main ? Camera.main.transform : null;
        canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvasScaler = canvas.GetComponent<CanvasScaler>();
            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            }
        }

        EnsureRootExists();
        transform.SetParent(root, true);
        transform.localScale = Vector3.one * fixedScale;

        // --- NOVO: Garante que o destaque comece desativado ---
        if (highlightBorder != null)
        {
            highlightBorder.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // --- NOVO: Limpa o registro para evitar memory leaks ---
        if (owner != null && nameplateRegistry.ContainsKey(owner))
        {
            nameplateRegistry.Remove(owner);
        }
    }

    private void EnsureRootExists()
    {
        if (root != null) return;
        var go = GameObject.Find("NameplateRoot");
        if (go == null)
        {
            go = new GameObject("NameplateRoot");
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }
        root = go.transform;
    }

    // --- MODIFICADO: A função Initialize agora também registra o nameplate ---
    public void Initialize(Transform targetToFollow, string characterName, float overrideYOffset = -1f)
    {
        target = targetToFollow;
        nameText.text = characterName;

        if (overrideYOffset > 0f) manualYOffset = overrideYOffset;
        computedYOffset = (manualYOffset > 0f) ? manualYOffset : CalculateAutoOffset(targetToFollow);
        transform.localScale = Vector3.one * fixedScale;

        // --- NOVO: Registra este nameplate ---
        owner = targetToFollow.GetComponentInParent<Targetable>();
        if (owner != null)
        {
            nameplateRegistry[owner] = this;
        }
    }

    // Sobrecarga de Initialize para manter a compatibilidade
    public void Initialize(Transform anchor)
    {
        Initialize(anchor, "", -1f);
    }

    // --- NOVO: Função que controla o visual do destaque ---
    public void SetHighlight(bool isHighlighted)
    {
        if (highlightBorder != null)
        {
            highlightBorder.gameObject.SetActive(isHighlighted);
        }

        if (nameText != null)
        {
            nameText.color = isHighlighted ? highlightedNameColor : defaultNameColor;
        }
    }


    private float CalculateAutoOffset(Transform t)
    {
        if (t == null) return 0.5f;
        Renderer r = t.GetComponentInParent<Renderer>();
        if (r != null) return r.bounds.size.y + 0.15f;
        CharacterController cc = t.GetComponentInParent<CharacterController>();
        if (cc != null) return cc.height + 0.15f;
        Collider col = t.GetComponentInParent<Collider>();
        if (col != null) return col.bounds.size.y + 0.15f;
        return 1.5f;
    }

    public void SetName(string characterName)
    {
        string tag = "";
        if (permlevel >= 99) tag = "<GM>";
        else if (permlevel >= 50) tag = "<MOD>";
        else if (permlevel >= 10) tag = "<VIP>";

        if (nameText != null) nameText.text = $"{tag}{characterName}";
    }

    public void SetLevel(int level)
    {
        if (levelText != null) levelText.text = $"{level}";
    }

    public void SetPermissionLevel(int level)
    {
        permlevel = level;
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthSlider != null) healthSlider.value = Mathf.Clamp01(currentHealth / maxHealth);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = target.position + Vector3.up * computedYOffset;

        if (camTransform != null)
        {
            Vector3 lookDir = transform.position - camTransform.position;
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }

        transform.localScale = Vector3.one * fixedScale;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            UnityEditor.Handles.Label(target.position + Vector3.up * 0.1f, $"lossyScale: {target.lossyScale}");
        }
    }
#endif
}