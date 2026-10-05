using NUnit.Framework;
using TMPro;
using UnityEngine;

public class BlinkingText : MonoBehaviour
{
    [SerializeField][UnityEngine.Range(0.0f, 10.0f)] private float fadeSpeed = 2.0f;
    private TextMeshProUGUI blinkingText;
    private float alphaValue = 1.0f;
    private bool fadingOut = true;
    private bool loadingMenu = false;

    private void Start()
    {
        blinkingText = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (blinkingText == null) return;

        if (loadingMenu) return;

        if (fadingOut)
        {
            alphaValue -= fadeSpeed * Time.deltaTime;

            if (alphaValue < 0.0f)
            {
                alphaValue = 0.0f;
                fadingOut = false;
            }
        }
        else
        {
            alphaValue += fadeSpeed * Time.deltaTime;
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

    public void LoadMenu()
    {
        loadingMenu = true;

        Color color = Color.white;
    }
}
