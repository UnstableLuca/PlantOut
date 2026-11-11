using System;
using UnityEngine;
public class RosarosSkills : SkillController
{
    [Header("Sol (pasiva)")]
    [Tooltip("Vacio = lo busca en la escena")]
    [SerializeField] SunSystem sun;
    [Tooltip("Barras por segundo con el sol al maximo (0.1 = una carga cada 10 s)")]
    [SerializeField, Min(0f)] float barPerSecondAtFullSun = 0.1f;
    [Tooltip("Nivel de sol que se usa si no hay SunSystem en la escena")]
    [SerializeField, Range(0f, 1f)] float fallbackSunLevel = 0.5f;

    [Header("Aros (mantener con 1-4 cargas)")]
    [SerializeField, Min(1)] int maxLevels = 4;
    [SerializeField, Min(0.1f)] float holdPerLevel = 0.5f;
    [Tooltip("Golpes por nivel. Indice = nivel (el 0 no se usa)")]
    [SerializeField] int[] hitsByLevel = { 0, 1, 2, 3, 5 };
    [Tooltip("Dano por segundo del sangrado de cada golpe. Indice = nivel")]
    [SerializeField] float[] bleedDpsByLevel = { 0f, 10f, 10f, 10f, 20f };
    [Tooltip("Duracion del sangrado en segundos. Indice = nivel")]
    [SerializeField] float[] bleedDurationByLevel = { 0f, 2f, 4f, 8f, 14f };
    [Tooltip("Dano extra cuando termina cada sangrado. Indice = nivel")]
    [SerializeField] float[] bleedEndDamageByLevel = { 0f, 0f, 0f, 20f, 60f };
    [Tooltip("Dano directo de cada golpe de aro (0 = solo el sangrado)")]
    [SerializeField] float hitDamage = 0f;
    [Tooltip("Segundos hasta el primer golpe")]
    [SerializeField] float strikeStartup = 0.2f;
    [SerializeField] float hitInterval = 0.25f;
    [Tooltip("Tiempo que queda sin poder actuar tras el ultimo golpe")]
    [SerializeField] float strikeRecovery = 0.3f;

    [Header("Ultimate (5 cargas): tormenta de petalos")]
    [Tooltip("Segundos del beso de aviso antes de que empiece la tormenta")]
    [SerializeField] float kissTime = 0.8f;
    [SerializeField, Min(1)] int petalCount = 12;
    [Tooltip("Los petalos caen de forma asincrona dentro de este intervalo")]
    [SerializeField] float stormDuration = 5f;
    [SerializeField] float petalDamage = 80f;
    [Tooltip("Si esta activo, cada petalo corta la accion del rival")]
    [SerializeField] bool petalsInterrupt = false;
    [Tooltip("Prefab del petalo (con el componente Projectile). Vacio = golpe instantaneo")]
    [SerializeField] Projectile petalPrefab;
    [Tooltip("De donde salen los petalos (se elige uno al azar). Vacio = golpe instantaneo")]
    [SerializeField] Transform[] petalSpawnPoints;
    [SerializeField] float petalSpeed = 14f;

    [Header("Animator (vacio = no se usa)")]
    [Tooltip("Si se deja vacio busca el Animator en los hijos")]
    [SerializeField] Animator animator;
    [Tooltip("Int: nivel de aros mientras mantienes (0-4)")]
    [SerializeField] string levelParameter = "RingLevel";
    [Tooltip("Bool: true mientras mantienes la habilidad")]
    [SerializeField] string chargingParameter = "Charging";
    [SerializeField] string strikeTrigger = "RingStrike";
    [SerializeField] string kissTrigger = "Kiss";

    enum Phase { Idle, Holding, Striking, Kissing }

    Phase phase;
    int level;
    int strikeLevel;
    int hitsTotal;
    int hitsFired;
    float holdTimer;
    float timer;

    bool stormActive;
    float stormElapsed;
    float[] petalTimes;
    int petalIndex;

    bool sunSearched;

    // ---------- Estado publico (UI) ----------

    public int Level => level;
    public bool StormActive => stormActive;
    public float SunLevel
    {
        get
        {
            if (sun == null && !sunSearched)
            {
                sunSearched = true;
                sun = FindAnyObjectByType<SunSystem>();
            }
            return sun != null ? sun.GetLevel(fighter) : fallbackSunLevel;
        }
    }

    public event Action<int> LevelChanged;
    public event Action StormStarted;

    /// <summary>Rosaros no recarga energia de ninguna otra forma que el sol.</summary>
    public override bool AllowsBarGain => false;

    protected override void Awake()
    {
        base.Awake();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    //  INPUT
    public override void OnSkillPressed()
    {
        if (phase != Phase.Idle || !fighter.CanAct) return;

        int charges = Mathf.FloorToInt(fighter.SkillBar + 0.0001f);

        if (charges >= fighter.MaxSkillUses)
        {
            StartUltimate();
            return;
        }

        if (charges == 0) return;   // la pasiva no se activa: solo hay que esperar al sol

        if (!fighter.BeginChannel()) return;

        phase = Phase.Holding;
        holdTimer = 0f;
        SetLevel(0);
        SetCharging(true);
    }

    public override void OnSkillReleased()
    {
        if (phase != Phase.Holding) return;

        SetCharging(false);

        if (level > 0) StartStrikes();
        else EndPhase();   // toque rapido: no pasa nada y no se gasta barra
    }

    public override void OnChannelInterrupted()
    {
        // Te golpean: se cancela lo que estuvieras haciendo. Los sangrados ya aplicados siguen.
        if (phase == Phase.Holding)
        {
            SetCharging(false);
            SetLevel(0);
        }

        phase = Phase.Idle;
    }

    //  UPDATE

    void Update()
    {
        // Pasiva: la barra sube con el sol (y con nada mas)
        if (fighter.InputEnabled && barPerSecondAtFullSun > 0f)
            fighter.AddBarPassive(SunLevel * barPerSecondAtFullSun * Time.deltaTime);

        float dt = Time.deltaTime * fighter.ActionSpeed;

        switch (phase)
        {
            case Phase.Holding:
                holdTimer += dt;
                while (phase == Phase.Holding && holdTimer >= holdPerLevel)
                {
                    holdTimer -= holdPerLevel;

                    if (level >= maxLevels || !fighter.TrySpendUses(1))
                    {
                        holdTimer = 0f;   // tope o sin barra: espera a que sueltes
                        break;
                    }
                    SetLevel(level + 1);
                }
                break;

            case Phase.Striking:
                timer += dt;
                while (hitsFired < hitsTotal && timer >= strikeStartup + hitsFired * hitInterval)
                {
                    hitsFired++;
                    FireRingHit();
                    if (phase != Phase.Striking) return;   // el golpe fue parreado y te aturdieron
                }

                if (hitsFired >= hitsTotal &&
                    timer >= strikeStartup + Mathf.Max(0, hitsTotal - 1) * hitInterval + strikeRecovery)
                    EndPhase();
                break;

            case Phase.Kissing:
                timer -= dt;
                if (timer <= 0f)
                {
                    EndPhase();
                    StartStorm();
                }
                break;
        }

        if (stormActive) UpdateStorm(Time.deltaTime);
    }

    void EndPhase()
    {
        phase = Phase.Idle;
        fighter.EndChannel();
    }

    //  AROS

    void StartStrikes()
    {
        phase = Phase.Striking;
        strikeLevel = level;
        hitsTotal = Mathf.Max(1, At(hitsByLevel, strikeLevel));
        hitsFired = 0;
        timer = 0f;
        SetLevel(0);
    }

    void FireRingHit()
    {
        Fighter rival = fighter.Rival;
        if (rival == null) return;

        Trigger(strikeTrigger);

        var hit = new HitData(fighter, hitDamage, 0f, isSkill: true, parryable: true,
                              kind: AttackKind.Melee, interrupts: false);

        // El sangrado solo se aplica si el golpe entra limpio (no bloqueado ni parreado)
        if (rival.ReceiveHit(hit) == BlockResult.None)
            rival.ApplyBleed(fighter,
                             At(bleedDpsByLevel, strikeLevel),
                             At(bleedDurationByLevel, strikeLevel),
                             At(bleedEndDamageByLevel, strikeLevel));
    }

    //  ULTIMATE

    void StartUltimate()
    {
        if (!fighter.BeginChannel()) return;
        fighter.TrySpendUses(fighter.MaxSkillUses);

        phase = Phase.Kissing;
        timer = kissTime;
        Trigger(kissTrigger);
    }

    void StartStorm()
    {
        petalTimes = new float[petalCount];
        for (int i = 0; i < petalCount; i++)
            petalTimes[i] = UnityEngine.Random.Range(0f, stormDuration);
        Array.Sort(petalTimes);

        petalIndex = 0;
        stormElapsed = 0f;
        stormActive = true;
        StormStarted?.Invoke();
    }

    void UpdateStorm(float dt)
    {
        stormElapsed += dt;

        while (petalIndex < petalTimes.Length && stormElapsed >= petalTimes[petalIndex])
        {
            petalIndex++;
            FirePetal();
        }

        if (petalIndex >= petalTimes.Length) stormActive = false;
    }

    void FirePetal()
    {
        Fighter rival = fighter.Rival;
        if (rival == null) return;

        Transform aim = rival.AimPoint;
        bool hasSpawn = petalSpawnPoints != null && petalSpawnPoints.Length > 0;

        if (petalPrefab == null || aim == null || !hasSpawn)
        {
            HitWithPetal(rival);
            return;
        }

        Transform from = petalSpawnPoints[UnityEngine.Random.Range(0, petalSpawnPoints.Length)];
        Projectile petal = Instantiate(petalPrefab, from.position, from.rotation);
        petal.Launch(aim, petalSpeed, () => HitWithPetal(rival));
    }

    void HitWithPetal(Fighter rival)
    {
        if (rival == null) return;

        // Bloqueo normal: dano completo. Parry ("justo a tiempo"): la mitad. El atacante no se aturde.
        rival.ReceiveHit(new HitData(fighter, petalDamage, 0f, isSkill: true, parryable: true,
                                     kind: AttackKind.Ranged, interrupts: petalsInterrupt,
                                     blockScale: 1f, parryScale: 0.5f));
    }

    //  UTILIDADES

    public override void ResetState()
    {
        phase = Phase.Idle;
        fighter.EndChannel();
        stormActive = false;
        SetLevel(0);
        SetCharging(false);
    }

    void SetLevel(int value)
    {
        level = Mathf.Clamp(value, 0, maxLevels);
        if (animator != null && !string.IsNullOrEmpty(levelParameter))
            animator.SetInteger(levelParameter, level);
        LevelChanged?.Invoke(level);
    }

    void SetCharging(bool value)
    {
        if (animator != null && !string.IsNullOrEmpty(chargingParameter))
            animator.SetBool(chargingParameter, value);
    }

    void Trigger(string trigger)
    {
        if (animator != null && !string.IsNullOrEmpty(trigger)) animator.SetTrigger(trigger);
    }

    static float At(float[] values, int index, float fallback = 0f)
    {
        if (values == null || values.Length == 0) return fallback;
        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }

    static int At(int[] values, int index, int fallback = 0)
    {
        if (values == null || values.Length == 0) return fallback;
        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }
}