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

    [Header("Rotación con ratón")]
    [Tooltip("Grados de rotación aplicados por cada punto de movimiento del ratón.")]
    public float mouseRotationSensitivity = 0.5f;

    [Tooltip("Velocidad con la que la bolsa alcanza suavemente la rotación del ratón.")]
    public float rotationSmoothSpeed = 18f;

    [Tooltip("Velocidad con la que vuelve a su orientación original al soltar el ratón.")]
    public float returnRotationSpeed = 5f;

    private Quaternion originalRotation;
    private Quaternion dragTargetRotation;
    private bool isDraggingRotation = false;
    private bool restoringRotation = false;

    [Header("Texturas")]
    private Texture2D frontTex;
    private Texture2D backTex;

    private Renderer frontRenderer;
    private Renderer backRenderer;

    private void Awake()
    {
        Transform frontObj = transform.Find("EtiquetaFrontal");
        Transform backObj = transform.Find("EtiquetaTrasera");

        if (frontObj != null)
            frontRenderer = frontObj.GetComponent<Renderer>();

        if (backObj != null)
            backRenderer = backObj.GetComponent<Renderer>();
    }

    private void Start()
    {
        baseTargetPos = transform.position;
        baseTargetScale = transform.localScale;

        // Guardamos la orientación inicial de la bolsa.
        originalRotation = transform.rotation;
        dragTargetRotation = originalRotation;
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

    private void Update()
    {
        // Movimiento de posición y escala.
        // El ratón nunca modifica la posición de la bolsa.
        Vector3 finalPos = baseTargetPos;

        if (isActiveBag)
        {
            float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            finalPos += new Vector3(0f, floatOffset, 0f);
        }

        float transitionFactor = 1f - Mathf.Exp(-currentTransitionSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(
            transform.position,
            finalPos,
            transitionFactor
        );

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            baseTargetScale,
            transitionFactor
        );

        // Rotación suave mientras se arrastra.
        if (isDraggingRotation)
        {
            float rotationFactor = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                dragTargetRotation,
                rotationFactor
            );
        }
        // Retorno automático al soltar el ratón.
        else if (restoringRotation)
        {
            float returnFactor = 1f - Mathf.Exp(-returnRotationSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                originalRotation,
                returnFactor
            );

            if (Quaternion.Angle(transform.rotation, originalRotation) < 0.1f)
            {
                transform.rotation = originalRotation;
                restoringRotation = false;
            }
        }
    }

    public void StartRotation()
    {
        isDraggingRotation = true;
        restoringRotation = false;
        dragTargetRotation = transform.rotation;
    }

    public void StopRotation()
    {
        if (!isDraggingRotation)
            return;

        isDraggingRotation = false;
        restoringRotation = true;
    }

    public void RotateWithMouse(float mouseX, float mouseY)
    {
        if (!isDraggingRotation)
            return;

        Quaternion yaw = Quaternion.AngleAxis(
            -mouseX * mouseRotationSensitivity,
            Vector3.up
        );

        dragTargetRotation = yaw * dragTargetRotation;

        Quaternion pitch = Quaternion.AngleAxis(
            mouseY * mouseRotationSensitivity,
            dragTargetRotation * Vector3.right
        );

        dragTargetRotation = pitch * dragTargetRotation;
    }

    private void UpdateTextures()
    {
        if (frontRenderer != null && frontTex != null)
        {
            frontRenderer.material.mainTexture = frontTex;

            float aspectRatio = (float)frontTex.width / frontTex.height;
            Vector3 currentScale = frontRenderer.transform.localScale;
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
        isDraggingRotation = false;
        restoringRotation = true;
        dragTargetRotation = originalRotation;
    }

    public void ReturnToShelf(Vector3 shelfPos, Vector3 targetScale)
    {
        isActiveBag = false;
        isDraggingRotation = false;

        baseTargetPos = shelfPos;
        baseTargetScale = targetScale;

        restoringRotation = true;
        dragTargetRotation = originalRotation;

        transform.position = shelfPos;
        transform.localScale = targetScale;
    }
}