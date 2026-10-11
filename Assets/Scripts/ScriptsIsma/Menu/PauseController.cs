using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class PauseController : MonoBehaviour
{
    [Header("Fondo")]
    [SerializeField] private RectTransform backgroundPanel;
    [SerializeField] private float backgroundOffscreenX = 1200f;
    [SerializeField] private float backgroundTargetX = 0f;
    [SerializeField] private float backgroundSlideDuration = 0.5f;

    [Header("Botones")]
    [SerializeField] private List<RectTransform> menuButtons;
    [SerializeField] private float buttonOffscreenX = 1000f;
    [SerializeField] private float buttonTargetX = 420f;
    [SerializeField] private float buttonSlideDuration = 0.4f;
    [SerializeField] private float delayBetweenButtons = 0.08f;

    [Header("Ajustes")]
    [SerializeField] private CanvasGroup settingsButtonCanvasGroup;
    [SerializeField] private Button defaultSelectedButton;

    private CanvasGroup menuCanvasGroup;
    private Coroutine activeAnimationRoutine;
    private bool isVisible = false;

    private void Awake()
    {
        menuCanvasGroup = GetComponent<CanvasGroup>();
        if (menuCanvasGroup == null) menuCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        ResetToHiddenState();
    }

    private void OnEnable()
    {
        if (PlazaManager.Instance != null && PlazaManager.Instance.CurrentState == GameState.MainMenu)
        {
            ShowMenu();
        }
    }

    #region Animacion
    public void ShowMenu()
    {
        if (activeAnimationRoutine != null) StopCoroutine(activeAnimationRoutine);

        ResetToHiddenState();
        
        gameObject.SetActive(true);
        activeAnimationRoutine = StartCoroutine(AnimateInRoutine());
    }

    public void HideMenu(System.Action onComplete = null)
    {
        if (activeAnimationRoutine != null) StopCoroutine(activeAnimationRoutine);
        activeAnimationRoutine = StartCoroutine(AnimateOutRoutine(onComplete));
    }

    private IEnumerator AnimateInRoutine()
    {
        isVisible = true;
        menuCanvasGroup.alpha = 1f;
        menuCanvasGroup.blocksRaycasts = false;

        ResetToHiddenState();

        float elapsed = 0f;
        Vector2 bgStartPos = new Vector2(backgroundOffscreenX, backgroundPanel.anchoredPosition.y);
        Vector2 bgTargetPos = new Vector2(backgroundTargetX, backgroundPanel.anchoredPosition.y);

        while (elapsed < backgroundSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / backgroundSlideDuration);
            backgroundPanel.anchoredPosition = Vector2.Lerp(bgStartPos, bgTargetPos, t);
            yield return null;
        }
        backgroundPanel.anchoredPosition = bgTargetPos;

        for (int i = 0; i < menuButtons.Count; i++)
        {
            StartCoroutine(AnimateButtonIn(menuButtons[i]));
            yield return new WaitForSeconds(delayBetweenButtons);
        }

        yield return new WaitForSeconds(buttonSlideDuration - delayBetweenButtons);

        if (settingsButtonCanvasGroup != null)
        {
            elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                settingsButtonCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.3f);
                yield return null;
            }
            settingsButtonCanvasGroup.alpha = 1f;
        }

        menuCanvasGroup.blocksRaycasts = true;
        if (defaultSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject);
        }
    }

    private IEnumerator AnimateButtonIn(RectTransform btn)
    {
        float elapsed = 0f;
        Vector2 startPos = new Vector2(buttonOffscreenX, btn.anchoredPosition.y);
        Vector2 targetPos = new Vector2(buttonTargetX, btn.anchoredPosition.y);

        while(elapsed < buttonSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / buttonSlideDuration);
            btn.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        btn.anchoredPosition = targetPos;
    }

    private IEnumerator AnimateOutRoutine(System.Action onComplete)
    {
        isVisible = false;
        menuCanvasGroup.blocksRaycasts = false;

        if (settingsButtonCanvasGroup != null)
        {
            float elapsedSettings = 0f;
            while (elapsedSettings < 0.2f)
            {
                elapsedSettings += Time.deltaTime;
                settingsButtonCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedSettings / 0.2f);
                yield return null;
            }
            settingsButtonCanvasGroup.alpha = 0f;
        }

        for (int i = menuButtons.Count - 1; i >= 0; i--)
        {
            StartCoroutine(AnimateButtonOut(menuButtons[i]));
            yield return new WaitForSeconds(delayBetweenButtons);
        }

        yield return new WaitForSeconds(buttonSlideDuration - delayBetweenButtons);

        float elapsedBg = 0f;
        Vector2 bgStartPos = backgroundPanel.anchoredPosition;
        Vector2 bgTargetPos = new Vector2(backgroundOffscreenX, backgroundPanel.anchoredPosition.y);

        while (elapsedBg < backgroundSlideDuration)
        {
            elapsedBg += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedBg / backgroundSlideDuration);
            backgroundPanel.anchoredPosition = Vector2.Lerp(bgStartPos, bgTargetPos, t);
            yield return null;
        }
        backgroundPanel.anchoredPosition = bgStartPos;

        gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    private IEnumerator AnimateButtonOut(RectTransform btn)
    {
        float elapsed = 0f;
        Vector2 startPos = btn.anchoredPosition;
        Vector2 targetPos = new Vector2(buttonOffscreenX, btn.anchoredPosition.y);

        while (elapsed < buttonSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / buttonSlideDuration);
            btn.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        btn.anchoredPosition = targetPos;
    }

    private void ResetToHiddenState()
    {
        if (backgroundPanel != null)
        {
            backgroundPanel.anchoredPosition = new Vector2(backgroundOffscreenX, backgroundPanel.anchoredPosition.y);
        }

        foreach (var btn in menuButtons)
        {
            if (btn != null)
            {
                btn.anchoredPosition = new Vector2(buttonOffscreenX, btn.anchoredPosition.y);
            }
        }

        if (settingsButtonCanvasGroup != null)
        {
            settingsButtonCanvasGroup.alpha = 0f;
        }
    }
    #endregion

    #region Botones
    public void OnClick_EnterPlaza()
    {
        if (!isVisible) return;

        HideMenu(() =>
        {
            if (PlazaManager.Instance != null)
            {
                PlazaManager.Instance.SetState(GameState.PlazaGameplay);
            }
        });
    }
    #endregion
}
