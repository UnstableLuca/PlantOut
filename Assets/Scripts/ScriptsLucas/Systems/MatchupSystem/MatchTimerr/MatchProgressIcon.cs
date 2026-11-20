using UnityEngine;

public class MatchProgressIcon : MonoBehaviour
{
    [SerializeField] MatchTimer matchTimer;
    [Tooltip("Icono que sube. Vacio = este mismo objeto")]
    [SerializeField] RectTransform icon;
    [Tooltip("Punto de abajo (inicio de la partida)")]
    [SerializeField] RectTransform bottom;
    [Tooltip("Punto de arriba (llegada a la luz)")]
    [SerializeField] RectTransform top;
    [Tooltip("Forma del avance. Usa la misma curva que el parallax para que vayan a la par")]
    [SerializeField] AnimationCurve progressCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    void Awake()
    {
        if (icon == null) icon = (RectTransform)transform;
    }

    void LateUpdate()
    {
        if (matchTimer == null || bottom == null || top == null) return;

        float t = progressCurve.Evaluate(matchTimer.Progress);
        icon.position = Vector3.Lerp(bottom.position, top.position, t);
    }
}
