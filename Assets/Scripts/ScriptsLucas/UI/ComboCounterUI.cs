using System.Collections;
using TMPro;
using UnityEngine;

public class ComboCounterUI : MonoBehaviour
{
    [Header("Jugadores")]
    [SerializeField] Fighter playerLeft;
    [SerializeField] Fighter playerRight;

    [Header("UI")]
    [Tooltip("Combo del jugador izquierdo (ComboIzq)")]
    [SerializeField] RectTransform leftRoot;
    [SerializeField] TMP_Text leftText;
    [Tooltip("Combo del jugador derecho (ComboDer)")]
    [SerializeField] RectTransform rightRoot;
    [SerializeField] TMP_Text rightText;
    [Tooltip("{0} = numero de golpes. Ej: \"{0}\" o \"x{0}\"")]
    [SerializeField] string format = "{0}";

    [Header("Reglas")]
    [Tooltip("Segundos maximos entre golpes para seguir el combo")]
    [SerializeField, Min(0.1f)] float comboWindow = 1.5f;
    [Tooltip("Golpes minimos para que se vea el contador")]
    [SerializeField, Min(1)] int minToShow = 2;
    [Tooltip("Contar tambien los golpes de habilidades")]
    [SerializeField] bool countSkillHits = true;

    [Header("Animacion")]
    [SerializeField, Min(1f)] float punchScale = 1.3f;
    [SerializeField, Min(0.01f)] float punchTime = 0.12f;
    [SerializeField, Min(0.01f)] float fadeTime = 0.3f;

    class Side
    {
        public RectTransform root;
        public TMP_Text text;
        public CanvasGroup group;
        public Vector3 baseScale;
        public int count;
        public float lastHit;
        public Coroutine anim;
    }

    Side left;
    Side right;

    void Awake()
    {
        left = MakeSide(leftRoot, leftText);
        right = MakeSide(rightRoot, rightText);
    }

    void OnEnable()
    {
        if (playerLeft != null) playerLeft.HitReceived += OnHit;
        if (playerRight != null) playerRight.HitReceived += OnHit;
    }

    void OnDisable()
    {
        if (playerLeft != null) playerLeft.HitReceived -= OnHit;
        if (playerRight != null) playerRight.HitReceived -= OnHit;
    }

    Side MakeSide(RectTransform root, TMP_Text text)
    {
        Side s = new Side { root = root, text = text };
        if (root == null) return s;

        s.baseScale = root.localScale;
        s.group = root.GetComponent<CanvasGroup>();
        if (s.group == null) s.group = root.gameObject.AddComponent<CanvasGroup>();
        s.group.alpha = 0f;
        s.group.blocksRaycasts = false;
        s.group.interactable = false;
        return s;
    }

    Side SideOf(Fighter f)
    {
        if (f == null) return null;
        return f.isLeftPlayer ? left : right;
    }

    void Update()
    {
        CheckTimeout(left);
        CheckTimeout(right);
    }

    void CheckTimeout(Side s)
    {
        if (s != null && s.count > 0 && Time.time - s.lastHit > comboWindow)
            Break(s);
    }

    void OnHit(HitReport r)
    {
        if (r.IsDamageOverTime) return;   

        bool blocked = r.Result == BlockResult.Blocked || r.Result == BlockResult.Parried;
        bool clean = r.Result == BlockResult.None && r.FinalDamage > 0f;

        if (clean) Break(SideOf(r.Victim));

        if (!(r.Source is Fighter attacker) || attacker == r.Victim) return;
        Side s = SideOf(attacker);
        if (s == null) return;

        if (blocked) { Break(s); return; } 
        if (!clean) return;
        if (r.IsSkill && !countSkillHits) return;

        if (Time.time - s.lastHit > comboWindow) s.count = 0;
        s.count++;
        s.lastHit = Time.time;

        if (s.count >= minToShow) Show(s);
    }

    void Show(Side s)
    {
        if (s.root == null) return;
        if (s.text != null) s.text.text = string.Format(format, s.count);

        if (s.anim != null) StopCoroutine(s.anim);
        s.anim = StartCoroutine(Punch(s));
    }

    void Break(Side s)
    {
        if (s == null || s.count == 0) return;

        bool wasVisible = s.count >= minToShow;
        s.count = 0;

        if (wasVisible && s.root != null)
        {
            if (s.anim != null) StopCoroutine(s.anim);
            s.anim = StartCoroutine(Fade(s));
        }
    }

    IEnumerator Punch(Side s)
    {
        s.group.alpha = 1f;
        for (float t = 0f; t < punchTime; t += Time.deltaTime)
        {
            s.root.localScale = s.baseScale * Mathf.Lerp(punchScale, 1f, t / punchTime);
            yield return null;
        }
        s.root.localScale = s.baseScale;
        s.anim = null;
    }

    IEnumerator Fade(Side s)
    {
        s.root.localScale = s.baseScale;
        float start = s.group.alpha;
        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            s.group.alpha = Mathf.Lerp(start, 0f, t / fadeTime);
            yield return null;
        }
        s.group.alpha = 0f;
        s.anim = null;
    }
}
