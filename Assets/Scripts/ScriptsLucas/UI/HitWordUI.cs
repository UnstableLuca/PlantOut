using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HitWordUI : MonoBehaviour
{
    [Header("Partida")]
    [SerializeField] MatchSetupManager matchSetup;
    [SerializeField] Fighter playerLeft;
    [SerializeField] Fighter playerRight;

    [Header("Imagenes (una por lado)")]
    [Tooltip("Donde salen las palabras del jugador de la izquierda")]
    [SerializeField] Image leftWord;
    [Tooltip("Donde salen las palabras del jugador de la derecha")]
    [SerializeField] Image rightWord;

    [Header("Cuando sale")]
    [Tooltip("Probabilidad por disparo acertado (1 = siempre)")]
    [SerializeField, Range(0f, 1f)] float chance = 1f;
    [Tooltip("Solo disparos limpios (no bloqueados ni parados)")]
    [SerializeField] bool onlyCleanHits = true;
    [Tooltip("Tambien con disparos de habilidades (escupitajo, bayas...)")]
    [SerializeField] bool includeSkillShots = false;
    [Tooltip("Segundos minimos entre dos palabras del mismo jugador")]
    [SerializeField, Min(0f)] float cooldown = 0.3f;

    [Header("Animacion")]
    [SerializeField, Min(0f)] float showDelay = 0f;
    [SerializeField, Min(0.01f)] float popTime = 0.1f;
    [SerializeField, Min(0f)] float holdTime = 0.5f;
    [SerializeField, Min(0.01f)] float fadeTime = 0.2f;
    [SerializeField, Min(1f)] float popScale = 1.25f;
    [SerializeField, Min(0f)] float randomRotation = 10f;

    class Slot
    {
        public Image image;
        public RectTransform rect;
        public CanvasGroup group;
        public CharacterUI data;
        public Vector3 baseScale;
        public Coroutine routine;
        public float lastShown = -999f;
    }

    Slot left;
    Slot right;

    void Start()
    {
        left = CreateSlot(leftWord, matchSetup != null ? matchSetup.LeftCharacter : null, true);
        right = CreateSlot(rightWord, matchSetup != null ? matchSetup.RightCharacter : null, false);
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

    Slot CreateSlot(Image image, CharacterUI data, bool isLeft)
    {
        if (image == null) return null;

        Slot s = new Slot { image = image, rect = image.rectTransform, data = data };

        s.group = image.GetComponent<CanvasGroup>();
        if (s.group == null) s.group = image.gameObject.AddComponent<CanvasGroup>();
        s.group.alpha = 0f;
        s.group.blocksRaycasts = false;
        s.group.interactable = false;

        image.preserveAspect = true;

        if (data != null)
        {
            Vector2 pos = data.hitWordPosition;
            if (!isLeft) pos.x = -pos.x;   // a la derecha, reflejado
            s.rect.anchoredPosition = pos;
            s.rect.sizeDelta = data.hitWordSize;
        }

        s.baseScale = s.rect.localScale;
        return s;
    }

    void OnHit(HitReport report)
    {
        if (!(report.Source is Fighter attacker)) return;   // solo golpes entre plantas
        if (report.Kind != AttackKind.Ranged) return;       // solo a distancia
        if (report.IsDamageOverTime) return;
        if (report.IsSkill && !includeSkillShots) return;
        if (onlyCleanHits && report.Result != BlockResult.None) return;

        Slot s = attacker.isLeftPlayer ? left : right;
        if (s == null || s.data == null || s.data.hitWords == null || s.data.hitWords.Length == 0) return;
        if (Time.time - s.lastShown < cooldown) return;
        if (Random.value > chance) return;

        s.lastShown = Time.time;
        s.image.sprite = s.data.hitWords[Random.Range(0, s.data.hitWords.Length)];

        if (s.routine != null) StopCoroutine(s.routine);
        s.routine = StartCoroutine(Pop(s));
    }

    IEnumerator Pop(Slot s)
    {
        s.group.alpha = 0f;
        if (showDelay > 0f) yield return new WaitForSeconds(showDelay);

        s.rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-randomRotation, randomRotation));
        s.group.alpha = 1f;

        for (float t = 0f; t < popTime; t += Time.deltaTime)
        {
            s.rect.localScale = s.baseScale * Mathf.Lerp(0f, popScale, t / popTime);
            yield return null;
        }
        for (float t = 0f; t < popTime; t += Time.deltaTime)
        {
            s.rect.localScale = s.baseScale * Mathf.Lerp(popScale, 1f, t / popTime);
            yield return null;
        }
        s.rect.localScale = s.baseScale;

        yield return new WaitForSeconds(holdTime);

        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            s.group.alpha = 1f - t / fadeTime;
            yield return null;
        }
        s.group.alpha = 0f;
        s.routine = null;
    }
}
