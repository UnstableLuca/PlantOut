using UnityEngine;

public class PlantHeight : MonoBehaviour
{
    [Header("Altura por Puntuación")]
    [SerializeField] float minHeightOffset = -1.5f;
    [SerializeField] float maxHeightOffset = 1.5f;
    [SerializeField] float smoothSpeed = 4f;

    [Header("Tallo (Opcional: inyectado por PlantCharacterModule)")]
    [SerializeField] Transform stemTransform;
    [SerializeField] float baseStemScaleY = 1f;
    [SerializeField] float stemScalePerUnit = 0.5f;

    [Header("Escenario / Cinemáticas")]
    [SerializeField] float stageOffsetY = 0f;

    private Vector3 baseLocalPosition;
    private float targetHeightOffset = 0f;
    private float currentHeightOffset = 0f;
    private bool onStage = false;

    public Transform StemTransform => stemTransform;

    void Awake()
    {
        baseLocalPosition = transform.localPosition;
    }

    public void SetStemTransform(Transform stem)
    {
        stemTransform = stem;
        if (stemTransform != null)
        {
            baseStemScaleY = stemTransform.localScale.y;
        }
    }

    /// <summary>
    /// Recibe un valor normalizado entre -1 (perdiendo al máximo) y +1 (ganando al máximo).
    /// </summary>
    public void SetNormalizedHeight(float normalized)
    {
        normalized = Mathf.Clamp(normalized, -1f, 1f);
        targetHeightOffset = normalized >= 0f
            ? normalized * maxHeightOffset
            : -normalized * minHeightOffset;
    }

    public void GoToStage()
    {
        onStage = true;
    }

    public void LeaveStage()
    {
        onStage = false;
    }

    void Update()
    {
        float desiredY = onStage ? stageOffsetY : targetHeightOffset;
        currentHeightOffset = Mathf.Lerp(currentHeightOffset, desiredY, Time.deltaTime * smoothSpeed);

        Vector3 pos = baseLocalPosition;
        pos.y += currentHeightOffset;
        transform.localPosition = pos;

        if (stemTransform != null)
        {
            Vector3 scale = stemTransform.localScale;
            scale.y = Mathf.Max(0.1f, baseStemScaleY + currentHeightOffset * stemScalePerUnit);
            stemTransform.localScale = scale;
        }
    }
}