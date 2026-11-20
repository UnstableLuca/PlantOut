using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusIconView : MonoBehaviour
{
    [Tooltip("Icono a color (abajo)")]
    public Image colorIcon;
    [Tooltip("Icono gris (encima). Se va rellenando en circulo con el tiempo")]
    public Image greyFill;
    [Tooltip("Opcional: numero de nivel (bichos del enjambre)")]
    public TMP_Text levelText;

    void Awake()
    {
        if (greyFill != null)
        {
            greyFill.type = Image.Type.Filled;
            greyFill.fillMethod = Image.FillMethod.Radial360;
            greyFill.fillOrigin = (int)Image.Origin360.Top;
            greyFill.fillClockwise = true;
            greyFill.fillAmount = 0f;
        }
    }

    public void Setup(Sprite color, Sprite grey)
    {
        if (colorIcon != null) colorIcon.sprite = color;
        if (greyFill != null) greyFill.sprite = grey;
    }
    public void SetProgress(float elapsed01, int level)
    {
        if (greyFill != null) greyFill.fillAmount = elapsed01;

        if (levelText != null)
        {
            bool show = level > 0;
            if (levelText.gameObject.activeSelf != show) levelText.gameObject.SetActive(show);
            if (show) levelText.text = level.ToString();
        }
    }
}
