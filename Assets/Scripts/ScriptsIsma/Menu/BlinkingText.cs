using NUnit.Framework;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class BlinkingText : MonoBehaviour
{
    [Header("Ajustes")]
    [SerializeField][UnityEngine.Range(0.0f, 10.0f)] private float blinkSpeed = 2.0f;
    [SerializeField] private CanvasGroup splashCanvasGroup;
    
    private TextMeshProUGUI blinkingText;
    private float alphaValue = 1.0f;
    private bool fadingOut = true;
    private bool loadingMenu = false;

    private InputActions inputActions;

    private void Awake()
    {
        blinkingText = GetComponent<TextMeshProUGUI>();
        inputActions = new InputActions();
    }

    private void OnEnable()
    {
        inputActions.Plaza.Enable();
    }

    private void OnDisable()
    {
        inputActions.Plaza.Disable();
    }

    private void Start()
    {
        if (splashCanvasGroup == null)
        {
            splashCanvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void Update()
    {
        if (blinkingText == null || loadingMenu) return;

        bool keyPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool mousePressed = Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame);

        bool gamepadPressed = false;
        if (Gamepad.current != null)
        {
            foreach (var control in Gamepad.current.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.wasPressedThisFrame) gamepadPressed = true;
            }
        }

        if (keyPressed || mousePressed || gamepadPressed)
        {
            StartCoroutine(LoadMenuRoutine());
            return;
        }

        if (fadingOut)
        {
            alphaValue -= blinkSpeed * Time.deltaTime;

            if (alphaValue < 0.0f)
            {
                alphaValue = 0.0f;
                fadingOut = false;
            }
        }
        else
        {
            alphaValue += blinkSpeed * Time.deltaTime;
            if(alphaValue >= 1.0f)
            {
                alphaValue = 1.0f;
                fadingOut = true;
            }
        }

        Color color = blinkingText.color;
        color.a = alphaValue;
        blinkingText.color = color;
    }

    public IEnumerator LoadMenuRoutine()
    {
        loadingMenu = true;

        blinkingText.color = Color.white;

        DollyTransitionController dollyController = FindFirstObjectByType<DollyTransitionController>();
        if (dollyController != null) dollyController.StartTransition(); 

        yield return new WaitForSeconds(0.3f);

        /*
        if (PlazaManager.Instance != null)
        {
            PlazaManager.Instance.SetState(GameState.MainMenu);
        }
        */

        float fadeDuration = 0.5f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp (1.0f, 0.0f, elapsed / fadeDuration);

            if (splashCanvasGroup != null)
            {
                splashCanvasGroup.alpha = newAlpha;
            }
            else
            {
                Color c = blinkingText.color;
                c.a = newAlpha;
                blinkingText.color = c;
            }

            yield return null;
        }

        if (splashCanvasGroup != null)
        {
            splashCanvasGroup.gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
