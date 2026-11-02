using UnityEngine;

public class SeedBag3D : MonoBehaviour
{
    [Header("Stand By (Flotación)")]
    public float floatSpeed = 2f;
    public float floatAmount = 0.05f;

    private Vector3 baseTargetPos;
    private Vector3 baseTargetScale = Vector3.one;
    private float currentTransitionSpeed = 8f;
    private bool isActiveBag = false;

    private Quaternion targetRotation;

    [Header("Texturas")]
    private Texture2D frontTex;
    private Texture2D backTex;

    private Renderer frontRenderer;
    private Renderer backRenderer;

    void Awake()
    {
        Transform frontObj = transform.Find("EtiquetaFrontal");
        Transform backObj = transform.Find("EtiquetaTrasera");

        if (frontObj != null) frontRenderer = frontObj.GetComponent<Renderer>();
        if (backObj != null) backRenderer = backObj.GetComponent<Renderer>();
    }

    void Start()
    {
        baseTargetPos = transform.position;
        baseTargetScale = transform.localScale;
        targetRotation = Quaternion.Euler(0, 90, 0);
    }

    public void SetupTextures(Texture2D front, Texture2D back)
    {
        frontTex = front;
        backTex = back;
        UpdateTextures();
    }

    public void SetIsActive(bool active)
    {
        isActiveBag = active;
        if (active)
        {
            targetRotation = Quaternion.Euler(0, 90, 0);
        }
    }

    public void SetTargetTransform(Vector3 pos, float speed, Vector3 targetScale)
    {
        baseTargetPos = pos;
        baseTargetScale = targetScale;
        currentTransitionSpeed = speed;
    }

    public void SetTargetTransform(Vector3 pos, float speed)
    {
        SetTargetTransform(pos, speed, transform.localScale);
    }

    void Update()
    {
        Vector3 finalPos = baseTargetPos;

        if (isActiveBag)
        {
            // Solo mantiene la animación de flotación en el punto de spawn
            float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            finalPos = baseTargetPos + new Vector3(0f, floatOffset, 0f);
        }

        // Movimiento y escala suaves hacia la posición objetivo
        transform.position = Vector3.Lerp(transform.position, finalPos, Time.deltaTime * currentTransitionSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, baseTargetScale, Time.deltaTime * currentTransitionSpeed);

        // Mantiene la rotación fija y orientada correctamente
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
    }

    private void UpdateTextures()
    {
        if (frontRenderer != null && frontTex != null)
        {
            frontRenderer.material.mainTexture = frontTex;

            // Ajustar automáticamente la proporción para que no se deforme
            float aspectRatio = (float)frontTex.width / frontTex.height;
            Vector3 currentScale = frontRenderer.transform.localScale;

            // Suponiendo que el eje Y es la altura fija y ajustamos el ancho (Eje X o Z según tu modelo)
            // Cambia 'x' por 'z' si tu plano está orientado en el otro sentido
            currentScale.x = currentScale.y * aspectRatio;
            frontRenderer.transform.localScale = currentScale;
        }

        if (backRenderer != null && backTex != null)
        {
            backRenderer.material.mainTexture = backTex;

            float aspectRatioBack = (float)backTex.width / backTex.height;
            Vector3 currentScaleBack = backRenderer.transform.localScale;
            currentScaleBack.x = currentScaleBack.y * aspectRatioBack;
            backRenderer.transform.localScale = currentScaleBack;
        }
    }

    public void ForceDeselect()
    {
        isActiveBag = false;
        targetRotation = Quaternion.Euler(0, 90, 0);
    }

    public void ReturnToShelf(Vector3 shelfPos, Vector3 targetScale)
    {
        isActiveBag = false;
        targetRotation = Quaternion.Euler(0, 90, 0);
        baseTargetPos = shelfPos;
        baseTargetScale = targetScale;
        transform.position = shelfPos;
        transform.localScale = targetScale;
        transform.rotation = targetRotation;
    }
}