using System.Collections;
using UnityEngine;

public class HitBurstUI : MonoBehaviour
{
    [Header("Jugadores")]
    [SerializeField] Fighter playerLeft;
    [SerializeField] Fighter playerRight;

    [Header("UI")]
    [SerializeField] RectTransform burst;

    [Header("Probabilidad")]
    [SerializeField, Range(0f, 1f)] float chance = 0.3f;
    [SerializeField] bool onlyCleanHits = true;
    [SerializeField] bool ignoreDamageOverTime = true;
    [SerializeField, Min(0f)] float cooldown = 0.5f;

    [Header("Animacion")]
    [SerializeField, Min(0.01f)] float popTime = 0.1f;
    [SerializeField, Min(0f)] float holdTime = 0.4f;
    [SerializeField, Min(0.01f)] float fadeTime = 0.2f;
    [SerializeField, Min(1f)] float popScale = 1.25f;
    [SerializeField, Min(0f)] float randomRotation = 12f;
    [SerializeField, Min(0f)] float showDelay = 0.1f;

    CanvasGroup group;
    Vector3 baseScale;
    Coroutine routine;

    float lastShown = -999f;

    void Awake()
    {
        if (burst == null) return;

        baseScale = burst.localScale;
        group = burst.GetComponent<CanvasGroup>();

        if (group == null) group = burst.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
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

    void OnHit(HitReport report)
    {
        if (burst == null) return;
        if (!(report.Source is Fighter)) return;          // solo golpes entre plantas, no bichos
        if (report.FinalDamage <= 0f) return;
        if (onlyCleanHits && report.Result != BlockResult.None) return;
        if (ignoreDamageOverTime && report.IsDamageOverTime) return;
        if (Time.time - lastShown < cooldown) return;
        if (Random.value > chance) return;

        lastShown = Time.time;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Pop());
    }

    IEnumerator Pop()
    {
        if (showDelay > 0f) yield return new WaitForSeconds(showDelay);

        burst.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-randomRotation, randomRotation));
        group.alpha = 1f;

        for (float t = 0f; t < popTime; t += Time.deltaTime)
        {
            burst.localScale = baseScale * Mathf.Lerp(0f, popScale, t / popTime);
            yield return null;
        }

        for (float t = 0f; t < popTime; t += Time.deltaTime)
        {
            burst.localScale = baseScale * Mathf.Lerp(popScale, 1f, t / popTime);
            yield return null;
        }
        burst.localScale = baseScale;

        yield return new WaitForSeconds(holdTime);

        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            group.alpha = 1f - t / fadeTime;
            yield return null;
        }
        group.alpha = 0f;
        routine = null;
    }
}
