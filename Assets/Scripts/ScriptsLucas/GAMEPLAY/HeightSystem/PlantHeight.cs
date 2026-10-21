using UnityEngine;

public class PlantHeight : MonoBehaviour
{
    [SerializeField] ScoreSystem score;
    [Tooltip("Si se deja vacio usa el Fighter de este mismo objeto")]
    [SerializeField] Fighter fighter;

    [Header("Bajada")]
    [Tooltip("maxima bajada como fraccion de la altura de ref (0.10 = 10%)")]
    [SerializeField, Range(0f, 0.5f)] float maxDropPercent = 0.10f;
    [Tooltip("Altura en unidades de mundo sobre la que se calcula el porcentaje (p.ej. la altura de la pantalla)")]
    [SerializeField, Min(0.1f)] float referernceHeight = 10f;
    [Tooltip("Mas alto = bajada mas lenta y progresiva")]
    [SerializeField, Min(0.1f)] float smoothTime = 1.5f;

    float baseY;
    float velocity;

    void Awake()
    {
        if (fighter == null) fighter = GetComponent<Fighter>();
    }

    void Start() => baseY = transform.position.y;

    private void LateUpdate()
    {
        if (score == null || fighter == null) return;

        float targetY = baseY - maxDropPercent * referernceHeight * score.GetDeficit(fighter);

        Vector3 p = transform.position;
        p.y = Mathf.SmoothDamp(p.y, targetY, ref velocity, smoothTime);
        transform.position = p;
    }
}
