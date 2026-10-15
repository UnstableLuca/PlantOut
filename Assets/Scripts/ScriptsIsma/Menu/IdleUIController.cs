using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class IdleHUDController : MonoBehaviour
{
    [Header("Referencias del Jugador")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Sub-Elementos de la Interfaz")]
    [SerializeField] private RectTransform currencyPanel;
    [SerializeField] private RectTransform controlMapPanel;

    [Header("Ajustes de Inactividad")]
    [SerializeField] private float idleThresholdTime = 3.0f;
    [SerializeField] private float animDuration = 0.45f;

    [Header("Distancia Fuera de Pantalla (Offset)")]
    [SerializeField] private float topOffscreenOffsetY = 300f;
    [SerializeField] private float bottomOffscreenOffsetY = -300f;

    private CanvasGroup canvasGroup;

    private Vector2 currencyOnscreenPos;
    private Vector2 currencyOffscreenPos;

    private Vector2 controlMapOnscreenPos;
    private Vector2 controlMapOffscreenPos;

    private float idleTimer = 0f;
    private bool isVisible = false;
    private Coroutine activeAnimationRoutine;
    private Vector3 lastPlayerPosition;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (currencyPanel != null)
        {
            currencyOnscreenPos = currencyPanel.anchoredPosition;
            currencyOffscreenPos = currencyOnscreenPos + new Vector2(0f, topOffscreenOffsetY);
        }

        if (controlMapPanel != null)
        {
            controlMapOnscreenPos = controlMapPanel.anchoredPosition;
            controlMapOffscreenPos = controlMapOnscreenPos + new Vector2(0f, bottomOffscreenOffsetY);
        }
    }

    private void Start()
    {
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (playerMovement != null)
        {
            lastPlayerPosition = playerMovement.transform.position;
        }

        ResetToHiddenState();
    }

    private void Update()
    {
        if (playerMovement == null || PlazaManager.Instance == null) return;

        if (PlazaManager.Instance.CurrentState != GameState.PlazaGameplay)
        {
            if (isVisible) HideHUD();
            idleTimer = 0f;
            return;
        }

        float movementDelta = Vector3.Distance(playerMovement.transform.position, lastPlayerPosition);
        bool isPlayerMoving = movementDelta > 0.01f;
        lastPlayerPosition = playerMovement.transform.position;

        if (isPlayerMoving)
        {
            idleTimer = 0f;
            if (isVisible)
            {
                HideHUD();
            }
        }
        else
        {
            idleTimer += Time.deltaTime;

            if (idleTimer >= idleThresholdTime && !isVisible)
            {
                ShowHUD();
            }
        }
    }

    private void ShowHUD()
    {
        isVisible = true;
        if (activeAnimationRoutine != null) StopCoroutine(activeAnimationRoutine);
        activeAnimationRoutine = StartCoroutine(AnimateHUDSequence(true));
    }

    private void HideHUD()
    {
        isVisible = false;
        if (activeAnimationRoutine != null) StopCoroutine(activeAnimationRoutine);
        activeAnimationRoutine = StartCoroutine(AnimateHUDSequence(false));
    }

    private IEnumerator AnimateHUDSequence(bool show)
    {
        float elapsed = 0f;

        float startAlpha = canvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;

        Vector2 startCurrency = currencyPanel != null ? currencyPanel.anchoredPosition : Vector2.zero;
        Vector2 targetCurrency = show ? currencyOnscreenPos : currencyOffscreenPos;

        Vector2 startControlMap = controlMapPanel != null ? controlMapPanel.anchoredPosition : Vector2.zero;
        Vector2 targetControlMap = show ? controlMapOnscreenPos : controlMapOffscreenPos;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            if (currencyPanel != null)
            {
                currencyPanel.anchoredPosition = Vector2.Lerp(startCurrency, targetCurrency, t);
            }

            if (controlMapPanel != null)
            {
                controlMapPanel.anchoredPosition = Vector2.Lerp(startControlMap, targetControlMap, t);
            }

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        if (currencyPanel != null) currencyPanel.anchoredPosition = targetCurrency;
        if (controlMapPanel != null) controlMapPanel.anchoredPosition = targetControlMap;
    }

    private void ResetToHiddenState()
    {
        canvasGroup.alpha = 0f;

        if (currencyPanel != null)
        {
            currencyPanel.anchoredPosition = currencyOffscreenPos;
        }

        if (controlMapPanel != null)
        {
            controlMapPanel.anchoredPosition = controlMapOffscreenPos;
        }
    }
}