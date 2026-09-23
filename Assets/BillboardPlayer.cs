// Scripts/Billboard.cs
using UnityEngine;

public class BillboardPlayer : MonoBehaviour
{
    private Transform cameraTransform;

    void Start()
    {
        // Encontra a câmera principal na cena.
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    // Usamos LateUpdate para garantir que a câmera já se moveu neste quadro.
    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Faz o objeto olhar diretamente para a posição da câmera.
        transform.LookAt(cameraTransform);

        // Zera a rotação em X e Z para manter o sprite "em pé",
        // permitindo apenas a rotação em Y (esquerda/direita).
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
    }
}