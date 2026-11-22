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
    [Tooltip("Mantiene la base del tallo pegada al suelo y lo estira hasta la planta (ignora Stem Scale Per Unit)")]
    [SerializeField] bool anchorStemToGround = true;

    [Header("Escenario / Cinemáticas")]
    [SerializeField] float stageOffsetY = 0f;

    private Vector3 baseLocalPosition;
    private float targetHeightOffset = 0f;
    private float currentHeightOffset = 0f;
    private bool onStage = false;
    private Renderer stemRenderer;
    private Vector3 stemBaseLocalPosition;
    private float stemGroundY;
    private float stemBaseHeight;

    public Transform StemTransform => stemTransform;

    void Awake()
    {
        baseLocalPosition = transform.localPosition;
    }

    public void SetStemTransform(Transform stem)
    {
        stemTransform = stem;
        if (stemTransform == null) return;

        baseStemScaleY = stemTransform.localScale.y;
        stemBaseLocalPosition = stemTransform.localPosition;

        stemRenderer = stemTransform.GetComponentInChildren<Renderer>();
        if (stemRenderer != null)
        {
            Bounds b = stemRenderer.bounds;
            stemGroundY = b.min.y;      // donde toca el suelo
            stemBaseHeight = b.size.y;  // altura sin estirar
        }
    }

    void Start()
    {
        // Por si el tallo se asigno a mano en el Inspector
        if (stemTransform != null && stemRenderer == null) SetStemTransform(stemTransform);
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
            if (anchorStemToGround && stemRenderer != null && stemBaseHeight > 0.001f)
            {
                StretchStemToGround();
            }
            else
            {
                Vector3 scale = stemTransform.localScale;
                scale.y = Mathf.Max(0.1f, baseStemScaleY + currentHeightOffset * stemScalePerUnit);
                stemTransform.localScale = scale;
            }
        }
    }

    void StretchStemToGround()
    {
        // La planta ha subido/bajado esto (en unidades del mundo)
        float parentScale = transform.parent != null ? transform.parent.lossyScale.y : 1f;
        float worldOffset = currentHeightOffset * parentScale;

        // Estirar el tallo lo que ha subido la planta
        float factor = Mathf.Max(0.05f, (stemBaseHeight + worldOffset) / stemBaseHeight);
        Vector3 scale = stemTransform.localScale;
        scale.y = baseStemScaleY * factor;
        stemTransform.localScale = scale;

        // Volver a pegar la base al suelo
        stemTransform.localPosition = stemBaseLocalPosition;
        float gap = stemGroundY - stemRenderer.bounds.min.y;
        stemTransform.position += Vector3.up * gap;
    }
}