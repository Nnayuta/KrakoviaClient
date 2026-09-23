using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }
    public Transform vfxContainer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Se o container não foi atribuído no Inspector, cria um dinamicamente.
        if (vfxContainer == null)
        {
            vfxContainer = new GameObject("[VFX_Container]").transform;
        }
    }
}