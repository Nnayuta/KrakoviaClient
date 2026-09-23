using UnityEngine;
using TMPro;

public class CombatText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private float floatSpeed = 1.0f;
    [SerializeField] private float fadeSpeed = 1.0f;
    [SerializeField] private float lifetime = 1.5f;

    private void Awake()
    {
        Destroy(gameObject, lifetime);
    }

    // O método Setup volta a ser simples
    public void Setup(int value, Color color)
    {
        textMesh.text = value.ToString();
        textMesh.color = color;
    }

    // O Update agora só cuida da animação local
    private void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, textMesh.color.a - (fadeSpeed * Time.deltaTime));
    }
}