using UnityEngine;
using UnityEngine.UI;

public class SkillBarUI : MonoBehaviour
{
    [Tooltip("Which character this bar belongs to. MatchSetupManager uses it to turn on the right bar.")]
    [SerializeField] CharacterUI character;
    public CharacterUI Character => character;

    [Tooltip("Fighter whose skill bar this shows. Can also be set from code with Bind().")]
    [SerializeField] Fighter fighter;

    [Tooltip("One Image per skill use, in order. Leave empty to use the direct child Images (1, 2, 3...).")]
    [SerializeField] Image[] segments;

    [Tooltip("Tint applied to a segment that is not fully charged yet.")]
    [SerializeField] Color chargingColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Smoothing")]
    [Tooltip("How fast the bar fills when energy is gained, in segments per second.")]
    [SerializeField, Min(0.01f)] float fillSpeed = 2f;

    [Tooltip("If on, spending a skill also slides down. If off, it drops instantly.")]
    [SerializeField] bool smoothSpend = false;

    [Tooltip("How fast the bar empties when Smooth Spend is on, in segments per second.")]
    [SerializeField, Min(0.01f)] float spendSpeed = 8f;

    const float FullEpsilon = 0.001f;

    Color[] fullColors;
    float shown;        // value currently drawn
    float lastDrawn = -1f;


    void Start()
    {
        if (segments == null || segments.Length == 0)
            CollectChildSegments();

        fullColors = new Color[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            Image img = segments[i];
            fullColors[i] = img.color;

            if (img.type != Image.Type.Filled)
            {
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
        }

        shown = 0f;
        Draw(0f);
    }

    void OnEnable()
    {
        if (fighter != null) shown = fighter.SkillBar;
        Draw(shown);
    }

    public void Bind(Fighter newFighter)
    {
        fighter = newFighter;
        if (fighter != null) shown = fighter.SkillBar;
        if (fullColors != null) Draw(shown);
    }

    void Update()
    {
        if (fighter == null) return;

        float target = fighter.SkillBar;

        if (target > shown)
        {
            shown = Mathf.MoveTowards(shown, target, fillSpeed * Time.deltaTime);
        }
        else if (target < shown)
        {
            shown = smoothSpend
                ? Mathf.MoveTowards(shown, target, spendSpeed * Time.deltaTime)
                : target;
        }

        if (!Mathf.Approximately(shown, lastDrawn))
            Draw(shown);
    }

    void Draw(float value)
    {
        lastDrawn = value;

        for (int i = 0; i < segments.Length; i++)
        {
            // Segment i covers the range [i, i + 1] of the skill bar.
            float fill = Mathf.Clamp01(value - i);
            bool full = fill >= 1f - FullEpsilon; // 0.1 added ten times is 0.9999...
            if (full) fill = 1f;

            segments[i].fillAmount = fill;
            segments[i].color = full ? fullColors[i] : chargingColor;
        }
    }

    void CollectChildSegments()
    {
        var list = new System.Collections.Generic.List<Image>();
        foreach (Transform child in transform)
        {
            Image img = child.GetComponent<Image>();
            if (img != null) list.Add(img);
        }
        segments = list.ToArray();
    }
}
