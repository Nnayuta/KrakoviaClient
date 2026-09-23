using UnityEngine;

public class CombatTextManager : MonoBehaviour
{
    public static CombatTextManager Instance { get; private set; }

    [SerializeField] private GameObject combatTextPrefab; // Arraste o CombatText_Prefab aqui

    [Header("Estilos de Cor")]
    public Color playerDamageColor = Color.yellow;
    public Color enemyDamageColor = Color.white;
    public Color healColor = Color.green;
    public Color criticalColor = Color.red;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowText(int value, Transform spawnParent, CombatEventType eventType, bool isCritical)
    {
        if (combatTextPrefab == null || spawnParent == null) return;

        GameObject textInstance = Instantiate(combatTextPrefab, spawnParent);

        Color textColor = GetColorForEventType(eventType, spawnParent.root);

        // A chamada agora é a versão mais simples
        textInstance.GetComponent<CombatText>()?.Setup(value, textColor);
    }

    private Color GetColorForEventType(CombatEventType eventType, Transform target)
    {
        if (eventType == CombatEventType.Heal || eventType == CombatEventType.CriticalHeal) return healColor;
        if (eventType == CombatEventType.CriticalDamage) return criticalColor;
        if (target.GetComponent<NetworkCharacter>() != null) return enemyDamageColor;
        return playerDamageColor;
    }
}