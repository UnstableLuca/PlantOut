using System;
using UnityEngine;
    
public class SpykeSkills : SkillController
{
    [Header("Brazos (mantener con 1-4 cargas)")]
    [SerializeField, Min(1)] int maxArms = 4;
    [Tooltip("Segundos mantenidos por cada brazo")]
    [SerializeField, Min(0.1f)] float holdPerArm = 0.5f;
    [Tooltip("Duracion de los brazos al terminar de castear desde cero. Indice = numero de brazos (el 0 no se usa)")]
    [SerializeField] float[] armsDurationByCount = { 0f, 5f, 6f, 8f, 12f };
    [Tooltip("Si recasteas con brazos puestos: segundos que se suman al contador por cada brazo nuevo")]
    [SerializeField] float timeBonusPerArm = 1f;
    [Tooltip("Idem, pero para el ultimo brazo (el 4o)")]
    [SerializeField] float timeBonusFourthArm = 2f;

    [Header("Dano de los basicos")]
    [Tooltip("Dano extra por brazo: 10 con 0 brazos, 15 con 1, 20 con 2...")]
    [SerializeField] float damagePerExtraArm = 5f;
    [Tooltip("true: el melee solo cuenta brazos melee y el rango solo brazos de rango. false: cuentan todos")]
    [SerializeField] bool countOnlyMatchingArms = true;

    [Header("Pinchos")]
    [Tooltip("Duracion de los pinchos. Indice = numero de brazos (0..4)")]
    [SerializeField] float[] spikeDurationByArms = { 2f, 5f, 6f, 8f, 12f };
    [Tooltip("Multiplicador del dano de los basicos. Indice = numero de brazos (0..4)")]
    [SerializeField] float[] spikeMultiplierByArms = { 1.5f, 2f, 2f, 2.5f, 2.5f };
    [SerializeField] float spikeCooldown = 4f;

    [Header("Ultimate (5 cargas)")]
    [SerializeField] float ultimateDamage = 200f;
    [SerializeField] float ultimateDuration = 4f;
    [Tooltip("Cada cuantos segundos cae un tick de dano (dano total / ticks)")]
    [SerializeField, Min(0.02f)] float ultimateTickInterval = 0.1f;
    [Tooltip("Tiempo que mantiene los 4 brazos con pinchos al acabar")]
    [SerializeField] float afterUltimateTime = 4f;
    [SerializeField] float postUltimateCooldown = 4f;

    [Header("Animaciones (triggers del Animator; vacio = no se lanza)")]
    [SerializeField] string armGrowTrigger = "ArmGrow";
    [SerializeField] string spikesTrigger = "Spikes";
    [SerializeField] string ultimateTrigger = "Ultimate";
    [SerializeField] string ultimateEndTrigger = "UltimateEnd";

    [Header("Visuales opcionales")]
    [Tooltip("Brazos extra en orden de aparicion (melee, rango, melee, rango)")]
    [SerializeField] GameObject[] armVisuals;
    [Tooltip("Pinchos: se activan mientras duran")]
    [SerializeField] GameObject[] spikeVisuals;

    enum Phase { Idle, Holding, Ultimate }

    Phase phase;

    int arms;
    float armsTimer;
    int addedThisCast;
    float holdTimer;

    float spikeTimer;
    float spikeMultiplier = 1f;
    double spikeReadyAt;
    double skillsReadyAt;

    float ultimateTimer;
    float ultimateTickTimer;
    int ticksFired;
    int totalTicks;
    float tickDamage;

    // ---------- Estado publico (UI, animacion) ----------

    public int Arms => arms;
    public float ArmsTimeLeft => armsTimer;
    public bool SpikesActive => spikeTimer > 0f;
    public float SpikeTimeLeft => spikeTimer;
    public bool IsUltimate => phase == Phase.Ultimate;
    public float SpikeCooldownLeft => Mathf.Max(0f, (float)(spikeReadyAt - Time.timeAsDouble));
    public float SkillsCooldownLeft => Mathf.Max(0f, (float)(skillsReadyAt - Time.timeAsDouble));

    public event Action<int> ArmsChanged;
    public event Action<bool> SpikesChanged;
    public event Action<bool> UltimateChanged;

    public override bool SuperArmor => phase == Phase.Ultimate;

    protected override void Awake()
    {
        base.Awake();
        RefreshVisuals();
    }

    //  INPUT

    public override void OnSkillPressed()
    {
        if (phase != Phase.Idle || !fighter.CanAct) return;
        if (Time.timeAsDouble < skillsReadyAt) return;

        int charges = Mathf.FloorToInt(fighter.SkillBar + 0.0001f);

        if (charges >= fighter.MaxSkillUses)
        {
            StartUltimate();
            return;
        }

        // Sin cargas, o ya con todos los brazos: no hay nada que mantener, directo a pinchos
        if (charges == 0 || arms >= maxArms)
        {
            TryCastSpikes();
            return;
        }

        // 1-4 cargas: empieza a mantener. Si sueltas antes del primer brazo, son pinchos.
        if (!fighter.BeginChannel()) return;

        phase = Phase.Holding;
        holdTimer = 0f;
        addedThisCast = 0;
    }

    public override void OnSkillReleased()
    {
        if (phase == Phase.Holding) FinishHold(allowTap: true);
    }

    public override void OnChannelInterrupted()
    {
        // Te golpean: se corta el casteo, los brazos ya salidos se quedan (y su contador empieza)
        if (phase == Phase.Holding) FinishHold(allowTap: false);
        else if (phase == Phase.Ultimate) EndUltimate();   // con superarmadura no deberia pasar
    }

    //  UPDATE

    void Update()
    {
        float dt = Time.deltaTime;

        if (armsTimer > 0f)
        {
            armsTimer -= dt;
            if (armsTimer <= 0f) SetArms(0);
        }

        if (spikeTimer > 0f)
        {
            spikeTimer -= dt;
            if (spikeTimer <= 0f) EndSpikes();
        }

        switch (phase)
        {
            case Phase.Holding:
                holdTimer += dt;
                while (phase == Phase.Holding && holdTimer >= holdPerArm)
                {
                    holdTimer -= holdPerArm;
                    GrowArm();
                }
                break;

            case Phase.Ultimate:
                ultimateTimer -= dt;
                ultimateTickTimer += dt;

                while (ticksFired < totalTicks && ultimateTickTimer >= ultimateTickInterval)
                {
                    ultimateTickTimer -= ultimateTickInterval;
                    FireTick();
                }

                if (ultimateTimer <= 0f) EndUltimate();
                break;
        }
    }

    //  BRAZOS

    void GrowArm()
    {
        if (!fighter.TrySpendUses(1))
        {
            FinishHold(allowTap: false);
            return;
        }

        SetArms(arms + 1);
        addedThisCast++;
        fighter.PlayAnimation(armGrowTrigger);

        if (arms >= maxArms || fighter.SkillBar < 1f) FinishHold(allowTap: false);
    }

    void FinishHold(bool allowTap)
    {
        phase = Phase.Idle;
        fighter.EndChannel();

        int added = addedThisCast;
        addedThisCast = 0;

        if (added > 0) ApplyArmsTimer(added);
        else if (allowTap) TryCastSpikes();
    }

    void ApplyArmsTimer(int added)
    {
        if (armsTimer > 0f)
        {
            // Ya habia brazos: se suma tiempo por cada brazo nuevo (el ultimo suma mas)
            float bonus = 0f;
            for (int k = arms - added + 1; k <= arms; k++)
                bonus += k >= maxArms ? timeBonusFourthArm : timeBonusPerArm;

            armsTimer += bonus;
        }
        else
        {
            armsTimer = At(armsDurationByCount, arms);
        }
    }

    void SetArms(int value)
    {
        arms = Mathf.Clamp(value, 0, maxArms);
        if (arms == 0) armsTimer = 0f;

        RefreshVisuals();
        ArmsChanged?.Invoke(arms);
    }

    //  PINCHOS

    bool TryCastSpikes()
    {
        if (Time.timeAsDouble < spikeReadyAt) return false;

        spikeTimer = At(spikeDurationByArms, arms);
        spikeMultiplier = At(spikeMultiplierByArms, arms, 1f);
        spikeReadyAt = Time.timeAsDouble + spikeCooldown;

        SetSpikesVisible(true);
        fighter.PlayAnimation(spikesTrigger);
        SpikesChanged?.Invoke(true);
        return true;
    }

    void EndSpikes()
    {
        spikeTimer = 0f;
        spikeMultiplier = 1f;
        SetSpikesVisible(false);
        SpikesChanged?.Invoke(false);
    }

    //  ULTI

    void StartUltimate()
    {
        if (!fighter.BeginChannel()) return;
        fighter.TrySpendUses(fighter.MaxSkillUses);

        phase = Phase.Ultimate;
        ultimateTimer = ultimateDuration;
        ultimateTickTimer = 0f;
        ticksFired = 0;
        totalTicks = Mathf.Max(1, Mathf.RoundToInt(ultimateDuration / ultimateTickInterval));
        tickDamage = ultimateDamage / totalTicks;

        fighter.PlayAnimation(ultimateTrigger);
        UltimateChanged?.Invoke(true);
    }

    void FireTick()
    {
        ticksFired++;

        Fighter rival = fighter.Rival;
        if (rival == null) return;

        // Bloqueable (reduce el dano) pero no parreable ni da barra: un parry no puede cancelar la ultimate
        var hit = new HitData(fighter, tickDamage, 0f, isSkill: true, parryable: false);
        rival.ReceiveHit(hit);
    }

    void EndUltimate()
    {
        while (ticksFired < totalTicks) FireTick();   // asegura el dano total aunque el ultimo frame sea largo

        phase = Phase.Idle;
        fighter.EndChannel();

        // Al acabar: todos los brazos con pinchos un rato, y habilidades en cooldown
        SetArms(maxArms);
        armsTimer = afterUltimateTime;

        spikeTimer = afterUltimateTime;
        spikeMultiplier = At(spikeMultiplierByArms, maxArms, 1f);
        SetSpikesVisible(true);
        SpikesChanged?.Invoke(true);

        skillsReadyAt = spikeReadyAt = Time.timeAsDouble + postUltimateCooldown;

        fighter.PlayAnimation(ultimateEndTrigger);
        UltimateChanged?.Invoke(false);
    }

    //  MODIFICADORES / UTILIDADES

    public override float ModifyAttackDamage(AttackKind kind, float baseDamage)
    {
        int matching = countOnlyMatchingArms
            ? (kind == AttackKind.Melee ? (arms + 1) / 2 : arms / 2)
            : arms;

        float damage = baseDamage + damagePerExtraArm * matching;
        if (spikeTimer > 0f) damage *= spikeMultiplier;
        return damage;
    }

    public override void ResetState()
    {
        phase = Phase.Idle;
        fighter.EndChannel();

        addedThisCast = 0;
        holdTimer = 0f;
        armsTimer = 0f;
        spikeReadyAt = 0;
        skillsReadyAt = 0;

        SetArms(0);
        EndSpikes();
    }

    static float At(float[] values, int index, float fallback = 0f)
    {
        if (values == null || values.Length == 0) return fallback;
        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }

    void RefreshVisuals()
    {
        if (armVisuals != null)
            for (int i = 0; i < armVisuals.Length; i++)
                if (armVisuals[i] != null) armVisuals[i].SetActive(i < arms);

        SetSpikesVisible(spikeTimer > 0f);
    }

    void SetSpikesVisible(bool visible)
    {
        if (spikeVisuals == null) return;
        foreach (var go in spikeVisuals)
            if (go != null) go.SetActive(visible);
    }
}