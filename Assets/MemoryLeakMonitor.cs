using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Monitor persistente de uso de memória — detecta crescimento anormal e possíveis leaks.
/// Mantém-se entre cenas e evita duplicatas.
/// </summary>
public class MemoryLeakMonitor : MonoBehaviour
{
    [Header("Configurações")]
    [Tooltip("Intervalo entre verificações (em segundos).")]
    public float checkInterval = 5f;

    [Tooltip("Limite de aumento permitido antes de avisar (em MB).")]
    public float leakThreshold = 50f;

    [Tooltip("Mostrar info na tela?")]
    public bool showOnScreen = true;

    private static MemoryLeakMonitor instance;
    private float lastCheckTime;
    private long lastMemory;
    private float totalIncrease;
    private GUIStyle guiStyle;

    private void Awake()
    {
        // Evita duplicatas
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        lastMemory = Profiler.GetTotalAllocatedMemoryLong();
        lastCheckTime = Time.time;
        guiStyle = new GUIStyle
        {
            fontSize = 16,
            normal = { textColor = Color.white }
        };
    }

    private void Update()
    {
        if (Time.time - lastCheckTime >= checkInterval)
        {
            long currentMemory = Profiler.GetTotalAllocatedMemoryLong();
            long diff = currentMemory - lastMemory;

            if (diff > leakThreshold * 1024 * 1024)
            {
                totalIncrease += diff / (1024f * 1024f);
                Debug.LogWarning($"[MemoryLeakMonitor] ⚠️ Possível leak detectado: +{diff / (1024f * 1024f):F2} MB em {checkInterval:F0}s | Total aumento: {totalIncrease:F2} MB");
            }

            lastMemory = currentMemory;
            lastCheckTime = Time.time;
        }
    }

    private void OnGUI()
    {
        if (!showOnScreen) return;

        float currentMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
        GUI.Label(new Rect(10, 10, 500, 25), $"Memória atual: {currentMB:F2} MB", guiStyle);
        GUI.Label(new Rect(10, 30, 500, 25), $"Aumento total: {totalIncrease:F2} MB", guiStyle);
    }
}
