using UnityEngine;
using UnityEngine.InputSystem;

public class SeedBag3D : MonoBehaviour
{
    private bool isFlipped = false;

    private Quaternion frontRotation = Quaternion.Euler(0, 0, 0);
    private Quaternion backRotation = Quaternion.Euler(0, 180, 0);

    [Header("Inspección")]
    public float rotationSpeed = 0.5f;
    public float returnSpeed = 8f;

    [Header("Stand By")]
    public float floatSpeed = 2f;
    public float floatAmount = 0.05f;

    private Vector3 baseTargetPos;
    private Vector3 baseTargetScale = Vector3.one;
    private float currentTransitionSpeed = 8f;
    private bool isActiveBag = false;

    private bool isDragging = false;
    private bool isSelected = false;
    private Quaternion currentInspectRotation;

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
        currentInspectRotation = transform.localRotation;
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
        if (!active)
        {
            isDragging = false;
            isSelected = false;
            currentInspectRotation = isFlipped ? backRotation : frontRotation;
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
            HandleMouseInteraction();

            if (isDragging || isSelected)
            {
                finalPos = baseTargetPos + new Vector3(0f, 0.3f, -0.2f);
            }
            else
            {
                float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
                finalPos = baseTargetPos + new Vector3(0f, floatOffset, 0f);
            }
        }
        else
        {
            Quaternion backgroundRot = isFlipped ? backRotation : frontRotation;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, backgroundRot, Time.deltaTime * currentTransitionSpeed);
        }

        transform.position = Vector3.Lerp(transform.position, finalPos, Time.deltaTime * currentTransitionSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, baseTargetScale, Time.deltaTime * currentTransitionSpeed);
    }

    private void HandleMouseInteraction()
    {
        if (Camera.main == null || Mouse.current == null)
            return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    isDragging = true;
                    isSelected = true;
                }
                else
                {
                    isSelected = false;
                }
            }
        }

        if (!Mouse.current.leftButton.isPressed)
        {
            isDragging = false;
            isSelected = false;
        }

        Quaternion targetRot = isFlipped ? backRotation : frontRotation;

        if (isDragging)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            currentInspectRotation *= Quaternion.Euler(-mouseDelta.y * rotationSpeed, mouseDelta.x * rotationSpeed, 0f);
            targetRot = currentInspectRotation;
        }
        else
        {
            currentInspectRotation = Quaternion.Slerp(currentInspectRotation, targetRot, Time.deltaTime * returnSpeed);
        }

        transform.localRotation = Quaternion.Slerp(transform.localRotation, currentInspectRotation, Time.deltaTime * 15f);
    }

    public void ToggleFlip()
    {
        isFlipped = !isFlipped;
        currentInspectRotation = isFlipped ? backRotation : frontRotation;
    }

    private void UpdateTextures()
    {
        if (frontRenderer != null && frontTex != null)
        {
            frontRenderer.material.mainTexture = frontTex;
        }

        if (backRenderer != null && backTex != null)
        {
            backRenderer.material.mainTexture = backTex;
        }
    }
}