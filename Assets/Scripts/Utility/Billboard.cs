using UnityEngine;

/// <summary>
/// Faz com que o GameObject ao qual está anexado sempre se oriente para a câmera principal.
/// </summary>
public class Billboard : MonoBehaviour
{
    private Transform cameraTransform;

    private void Start()
    {
        // Pega a referência da câmera uma vez
        cameraTransform = Camera.main.transform;
    }

    // LateUpdate é usado para garantir que a rotação aconteça
    // depois que a câmera completou seu movimento no frame.
    private void LateUpdate()
    {
        if (cameraTransform != null)
        {
            transform.rotation = cameraTransform.rotation;
        }
    }
}