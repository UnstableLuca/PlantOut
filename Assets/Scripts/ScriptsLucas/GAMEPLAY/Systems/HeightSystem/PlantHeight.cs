using UnityEngine;

public class PlantHeight : MonoBehaviour
{
    [SerializeField] ScoreSystem score;
    [Tooltip("Si se deja vacio usa el Fighter de este mismo objeto")]
    [SerializeField] Fighter fighter;

    [Header("Bajada")]
    [Tooltip("Maxima bajada como fraccion de la altura de referencia (0.10 = 10%)")]
    [SerializeField, Range(0f, 0.5f)] float maxDropPercent = 0.10f;
    [Tooltip("Altura en unidades de mundo sobre la que se calcula el porcentaje (p.ej. la altura de la pantalla)")]
    [SerializeField, Min(0.1f)] float referenceHeight = 10f;
    [Tooltip("Mas alto = bajada mas lenta y progresiva")]
    [SerializeField, Min(0.05f)] float smoothTime = 1.5f;

    Vector3 basePos;
    float dropY;
    float dropVelocity;

    // Cinematica: desplazamiento suave hacia un punto de escenario
    bool hasStage;
    Vector3 stagePos;
    float stageSmooth = 0.4f;
    Vector3 offset;
    Vector3 offsetVelocity;

    void Awake()
    {
        if (fighter == null) fighter = GetComponent<Fighter>();
    }

    void Start()
    {
        basePos = transform.position;
        dropY = basePos.y;
    }

    public Vector3 BasePosition => basePos;

    public void GoToStage(Vector3 worldPosition, float smooth)
    {
        hasStage = true;
        stagePos = worldPosition;
        stageSmooth = Mathf.Max(0.01f, smooth);
    }

    public void LeaveStage(float smooth)
    {
        hasStage = false;
        stageSmooth = Mathf.Max(0.01f, smooth);
    }

    void LateUpdate()
    {
        float targetY = basePos.y;
        if (score != null && fighter != null)
            targetY -= maxDropPercent * referenceHeight * score.GetDeficit(fighter);

        dropY = Mathf.SmoothDamp(dropY, targetY, ref dropVelocity, smoothTime);

        Vector3 targetOffset = hasStage ? stagePos - basePos : Vector3.zero;
        offset = Vector3.SmoothDamp(offset, targetOffset, ref offsetVelocity, stageSmooth);

        transform.position = new Vector3(basePos.x + offset.x, dropY + offset.y, basePos.z + offset.z);
    }
}
