using UnityEngine;

public class SunSystem : MonoBehaviour
{
    [SerializeField] ScoreSystem score;

    [Header("Sol segun la altura")]
    [Tooltip("Sol de la planta que va mas baja (0..1)")]
    [SerializeField, Range(0f, 1f)] float minSun = 0.15f;
    [Tooltip("Sol de la planta que va mas alta (0..1)")]
    [SerializeField, Range(0f, 1f)] float maxSun = 1f;
    [Tooltip("Forma de la subida: X = altura relativa (0 = la mas baja, 1 = la mas alta), Y = mezcla entre min y max")]
    [SerializeField] AnimationCurve response = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("Sol que se usa si no hay ScoreSystem")]
    [SerializeField, Range(0f, 1f)] float fallbackLevel = 0.5f;

    public float GetLevel(Fighter f)
    {
        if (score == null || f == null) return fallbackLevel;

        // 0 = vas muy por detras (la planta mas baja), 0.5 = igualados, 1 = vas muy por delante
        float relative = 0.5f + 0.5f * (score.GetLead(f) - score.GetDeficit(f));
        return Mathf.Lerp(minSun, maxSun, Mathf.Clamp01(response.Evaluate(relative)));
    }
}
