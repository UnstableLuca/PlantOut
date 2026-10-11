using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour
{
    [Header("Ajustes")]
    [SerializeField] private float shiftAmount = 30.0f;
    [SerializeField] private float animDuration = 0.15f;
    [SerializeField] private float basePositionX = 420f;

    private RectTransform rectTransform;
    private Button button;
    private Coroutine activeMoveCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        button = GetComponent<Button>();

        button.onClick.AddListener(OnButtonClicked);
    }

    #region Hover
    public void OnPointerEnter(PointerEventData eventData)
    {
        SelectButton();
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DeselectButton();
    }
    #endregion

    #region Gamepad
    public void OnSelect(BaseEventData eventData)
    {
        SelectButton();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        DeselectButton();
    }
    #endregion

    #region Animaciones
    private void SelectButton()
    {
        float targetX = basePositionX - shiftAmount;
        Debug.Log($"targetX: {targetX}, baseX {basePositionX}");
        MoveToX(targetX);
    }

    private void DeselectButton()
    {
        MoveToX(basePositionX);
    }

    private void MoveToX(float targetX)
    {
        if (activeMoveCoroutine != null) StopCoroutine(activeMoveCoroutine);
        activeMoveCoroutine = StartCoroutine(AnimateToX(targetX));
    }

    private IEnumerator AnimateToX(float targetX)
    {
        float elapsed = 0f;
        float startX = rectTransform.anchoredPosition.x;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);

            Vector2 pos = rectTransform.anchoredPosition;
            pos.x = Mathf.Lerp(startX, targetX, t);
            rectTransform.anchoredPosition = pos;

            yield return null;
        }

        Vector2 finalPos = rectTransform.anchoredPosition;
        finalPos.x = targetX;
        rectTransform.anchoredPosition = finalPos;
    }
    #endregion

    #region Salida
    private void OnButtonClicked()
    {
        StartCoroutine(ClickSequenceRoutine());
    }

    private IEnumerator ClickSequenceRoutine()
    {
        yield return AnimateToX(basePositionX);
    }

    public void SetBasePositionX(float x)
    {
        basePositionX = x;
    }
    #endregion
}
