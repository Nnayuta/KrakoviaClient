using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class AttackIndicator : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    /// <summary>
    /// Inicia o indicador de ataque.
    /// </summary>
    /// <param name="caster">O transform de quem está atacando.</param>
    /// <param name="target">O transform do alvo.</param>
    /// <param name="duration">Por quanto tempo a linha deve ser visível.</param>
    public void Initialize(Transform caster, Transform target, float duration)
    {
        if (caster == null || target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Coloca o indicador na posição do caster
        transform.position = caster.position;

        // O ponto 0 da linha é a origem do nosso GameObject (o caster)
        _lineRenderer.SetPosition(0, Vector3.zero);

        // O ponto 1 é a direção e distância até o alvo
        Vector3 targetPositionRelative = target.position - caster.position;
        _lineRenderer.SetPosition(1, targetPositionRelative);

        // Destrói o objeto da linha após a duração especificada
        Destroy(gameObject, duration);
    }
}