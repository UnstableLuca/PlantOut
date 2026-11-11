using System;
using System.Collections.Generic;
using UnityEngine;

public enum FighterState { Idle, Acting, Blocking, Stunned, Channeling }

public readonly struct HitReport
{
    public readonly Fighter Victim;
    public readonly IDamageable Source;   // el rival o un bicho
    public readonly float RawDamage;
    public readonly float FinalDamage;    // tras bloqueo/parry
    public readonly BlockResult Result;
    public readonly bool IsSkill;

    public HitReport(Fighter victim, IDamageable source, float rawDamage,
                     float finalDamage, BlockResult result, bool isSkill)
    {
        Victim = victim;
        Source = source;
        RawDamage = rawDamage;
        FinalDamage = finalDamage;
        Result = result;
        IsSkill = isSkill;
    }
}


public class Fighter : MonoBehaviour, IFighter
{
    [Header("Acciones")]
    [SerializeField] AttackAction meleeAttack;
    [SerializeField] AttackAction rangedAttack;
    [SerializeField] BlockAction block;

    [Header("Habilidades")]
    [Tooltip("Si se deja vacio usa el SkillController de este mismo objeto")]
    [SerializeField] SkillController skills;

    [Header("Puntos de referencia")]
    [Tooltip("De donde sale el pincho al disparar (la punta del brazo de rango). Vacio = el propio objeto")]
    [SerializeField] Transform projectileOrigin;
    [Tooltip("A donde apuntan los disparos enemigos. Vacio = el propio objeto")]
    [SerializeField] Transform aimPoint;

    [Header("Lados")]
    [Tooltip("Lado que mira al rival. El otro lado es por donde llegan los bichos.")]
    [SerializeField] AttackSide rivalSide = AttackSide.Right;

    [Header("Barra de habilidad")]
    [SerializeField, Min(1)] int maxSkillUses = 5;

    [Header("Parry")]
    [Tooltip("Stun que sufre el atacante cuando le parreas")]
    [SerializeField] float parryStunOnAttacker = 0.5f;

    [Header("Animaciones (vacio = no se lanza)")]
    [SerializeField] Animator animator;
    [SerializeField] string hitTrigger = "Hit";             // leve retroceso al recibir dano
    [SerializeField] string parryTrigger = "Parry";         // se libera de la guardia
    [SerializeField] string blockEndTrigger = "BlockEnd";   // baja la guardia al soltar
    [SerializeField] string stunTrigger = "";
    [SerializeField, Min(0f)] float stunAnimMinDuration = 0.5f; 

    // ---------- Estado publico ----------

    public Fighter Rival { get; set; }              // lo asigna el gestor de partida
    public bool InputEnabled { get; set; } = true;  // false entre rondas, al acabar, etc.
    public FighterState State => state;
    public AttackSide RivalSide => rivalSide;
    public Transform ProjectileOrigin => projectileOrigin != null ? projectileOrigin : transform;
    public Transform AimPoint => aimPoint != null ? aimPoint : transform;

    public float ActionSpeed => Time.timeAsDouble < slowEnd ? slowFactor : 1f;
    public int MaxSkillUses => maxSkillUses;
    public float SkillBar { get; private set; }
    public bool CanAct => InputEnabled && state == FighterState.Idle && Time.timeAsDouble >= lockUntil;

    public event Action<HitReport> HitReceived;
    public event Action<float> SkillBarChanged;

    // ---------- Estado interno ----------

    FighterState state = FighterState.Idle;

    PlayerAction currentAction;
    ActionContext currentCtx;
    ActionPhase phase;
    double actionStart;
    float actionDuration;
    float actionSpeed = 1f;

    double blockPressTime;
    double lockUntil;     // anti-spam tras soltar el bloqueo
    double stunEnd;
    double slowEnd;
    float slowFactor = 1f;

    readonly List<IDamageable> leftBugs = new List<IDamageable>();
    readonly List<IDamageable> rightBugs = new List<IDamageable>();

    //Efectos en el tiempo / DOT (Damage Over Time)

    const double EffectTick = 0.5;

    class Dot
    {
        public IDamageable source;
        public float dps;
        public double end;
        public double nextTick;
        public float endDamage;
    }

    class Swarm
    {
        public IDamageable source;
        public int count;
        public float dpsPerBug;
        public double end;
        public double nextTick;
        public AttackKind shakeKind;
        public int bugsPerPress;
    }

    readonly List<Dot> bleeds = new List<Dot>();
    Swarm swarm;

    public int SwarmCount => swarm != null ? swarm.count : 0;
    public event Action<int> SwarmChanged;
    bool Armored => skills != null && skills.SuperArmor;

    void Awake()
    {
        if (skills == null) skills = GetComponent<SkillController>();
    }

    //  INPUT (lo llama FighterInput)

    public bool RequestAttack(AttackKind kind)
    {
        if (skills != null && skills.InterceptAttack(kind)) return true;

        if (swarm != null && InputEnabled && kind == swarm.shakeKind)
        {
            ShakeSwarm(swarm.bugsPerPress);
            return true;
        }

        AttackAction action = kind == AttackKind.Melee ? meleeAttack : rangedAttack;
        return action != null && TryStart(action);
    }

    public void RequestSkillPress() { if (skills != null) skills.OnSkillPressed(); }
    public void RequestSkillRelease() { if (skills != null) skills.OnSkillReleased(); }

    public bool RequestBlockPress()
    {
        if (skills != null && skills.InterceptBlockPress()) return true;
        if (block == null) return false;

        var ctx = new ActionContext(this, Time.timeAsDouble);
        if (!block.CanExecute(ctx)) return false;

        currentAction = block;
        currentCtx = ctx;
        block.Execute(ctx);   // llama a BeginBlock -> state = Blocking
        return true;
    }

    public void RequestBlockRelease() => EndBlock();

    //  EJECUCION DE ACCIONES

    bool TryStart(PlayerAction action)
    {
        var ctx = new ActionContext(this, Time.timeAsDouble);
        if (!action.CanExecute(ctx)) return false;

        currentAction = action;
        currentCtx = ctx;
        actionStart = ctx.Now;
        actionDuration = action.GetTotalDuration(ctx);
        actionSpeed = ActionSpeed;
        phase = ActionPhase.Startup;
        state = FighterState.Acting;

        action.Execute(ctx);
        return true;
    }

    void Update()
    {
        TickEffects();

        switch (state)
        {
            case FighterState.Acting:
                TickAction();
                break;
            case FighterState.Stunned:
                if (Time.timeAsDouble >= stunEnd) state = FighterState.Idle;
                break;
        }
    }

    void TickAction()
    {

        while (state == FighterState.Acting)
        {
            float elapsed = (float)(Time.timeAsDouble - actionStart);
            PlayerAction a = currentAction;

            if (phase == ActionPhase.Startup && elapsed >= a.startup)
                phase = ActionPhase.Active;
            else if (phase == ActionPhase.Active && elapsed >= a.startup + a.active)
                phase = ActionPhase.Recovery;
            else if (phase == ActionPhase.Recovery && elapsed >= actionDuration)
            {
                FinishAction();
                return;
            }
            else
                return;

            a.OnPhaseEnter(currentCtx, phase);
        }
    }

    void FinishAction()
    {
        currentAction = null;
        state = FighterState.Idle;
    }

    void Interrupt()
    {
        if (state == FighterState.Channeling)
        {
            state = FighterState.Idle;
            if (skills != null) skills.OnChannelInterrupted();
        }

        if (currentAction == null) return;

        PlayerAction a = currentAction;
        ActionContext c = currentCtx;
        currentAction = null;
        state = FighterState.Idle;
        a.OnInterrupted(c);
    }

    public void Stun(float duration)
    {
        if (Armored) return;   // superarmadura: ni stun ni interrupcion

        Interrupt();
        state = FighterState.Stunned;
        stunEnd = Math.Max(stunEnd, Time.timeAsDouble + duration);
        if (duration >= stunAnimMinDuration) PlayAnimation(stunTrigger);
    }

    //CHANELLING

    public void ApplySlow(float factor, float duration)
    {
        bool active = Time.timeAsDouble < slowEnd;
        slowFactor = active ? Mathf.Min(slowFactor, factor) : factor;
        slowEnd = Math.Max(slowEnd, Time.timeAsDouble + duration);
    }

    public bool BeginChannel()
    {
        if (state != FighterState.Idle) return false;
        state = FighterState.Channeling;
        return true;
    }

    public void EndChannel()
    {
        if (state == FighterState.Channeling) state = FighterState.Idle;
    }

    //  IFighter

    public void PlayAnimation(string trigger)
    {
        if (animator != null && !string.IsNullOrEmpty(trigger)) animator.SetTrigger(trigger);
    }

    public void BeginBlock(double pressTime)
    {
        state = FighterState.Blocking;
        blockPressTime = pressTime;
    }

    public void EndBlock()
    {
        if (state != FighterState.Blocking) return;

        state = FighterState.Idle;
        currentAction = null;
        lockUntil = Time.timeAsDouble + (block != null ? block.recovery : 0f);
        PlayAnimation(blockEndTrigger);
    }

    public bool TrySpendUses(int uses)
    {
        if (SkillBar < uses) return false;
        SetBar(SkillBar - uses);
        return true;
    }

    public void AddBar(float amount)
    {
        if (skills != null && !skills.AllowsBarGain) return;
        SetBar(SkillBar + amount);
    }
    public void AddBarPassive(float amount) => SetBar(SkillBar + amount);

    void SetBar(float value)
    {
        SkillBar = Mathf.Clamp(value, 0f, maxSkillUses);
        SkillBarChanged?.Invoke(SkillBar);
    }

    public float ModifyAttackDamage(AttackKind kind, float baseDamage)
        => skills != null ? skills.ModifyAttackDamage(kind, baseDamage) : baseDamage;

    public IDamageable GetAutoTarget(bool includeBugs)
    {
        if (includeBugs)
        {
            AttackSide bugSide = rivalSide == AttackSide.Left ? AttackSide.Right : AttackSide.Left;
            List<IDamageable> lane = bugSide == AttackSide.Left ? leftBugs : rightBugs;

            for (int i = 0; i < lane.Count; i++)
            {
                IDamageable bug = lane[i];
                if (!IsAlive(bug)) continue;
                if (bug is IShootable shootable && !shootable.CanBeTargeted) continue;
                return bug;
            }
        }

        return GetTarget(rivalSide);
    }

    public IDamageable GetTarget(AttackSide side)
    {
        if (side == rivalSide)
            return IsAlive(Rival) ? Rival : null;

        List<IDamageable> lane = side == AttackSide.Left ? leftBugs : rightBugs;
        while (lane.Count > 0)
        {
            if (IsAlive(lane[0])) return lane[0];
            lane.RemoveAt(0);   // bicho destruido sin desregistrarse
        }
        return null;
    }

    //  RECIBIR GOLPES (rival o bichos)

    public BlockResult ReceiveHit(in HitData hit)
    {
        BlockResult result = BlockResult.None;
        float damage = hit.Damage;

        if (state == FighterState.Blocking && block != null)
        {
            result = block.Evaluate(blockPressTime, Time.timeAsDouble);
            if (result == BlockResult.Parried && !hit.Parryable) result = BlockResult.Blocked;

            damage = block.ModifyDamage(result, damage);
            if (result == BlockResult.Blocked && hit.BlockScale >= 0f) damage = hit.Damage * hit.BlockScale;
            if (result == BlockResult.Parried && hit.ParryScale >= 0f) damage = hit.Damage * hit.ParryScale;

            if (hit.Parryable) AddBar(block.BarReward(result));   // los ticks de la ultimate no dan barra

            if (result == BlockResult.Parried)
            {
                PlayAnimation(parryTrigger);
                if (hit.Source is Fighter attacker && attacker != this)
                    attacker.Stun(parryStunOnAttacker);
            }
        }
        else
        {
            if(hit.Interrupts)
            {
                if (!Armored)
                {
                    if (hit.Hitstun > 0f) Stun(hit.Hitstun);
                    else Interrupt();
                }
                PlayAnimation(hitTrigger);
            }
        }

        HitReceived?.Invoke(new HitReport(this, hit.Source, hit.Damage, damage, result, hit.IsSkill));
        return result;
    }

    //  BICHOS (los llama el spawner / el propio bicho)

    public void RegisterBug(AttackSide side, IDamageable bug)
    {
        List<IDamageable> lane = side == AttackSide.Left ? leftBugs : rightBugs;
        if (!lane.Contains(bug)) lane.Add(bug);
    }

    public void UnregisterBug(IDamageable bug)
    {
        leftBugs.Remove(bug);
        rightBugs.Remove(bug);
    }

    public void CollectBugs(List<IDamageable> into)
    {
        into.Clear();
        foreach (var b in leftBugs) if (IsAlive(b)) into.Add(b);
        foreach (var b in rightBugs) if (IsAlive(b)) into.Add(b);
    }

    //DOT

    public void TakeDirectDamage(IDamageable source, float damage, bool isSkill)
    {
        if (damage <= 0f) return;
        HitReceived?.Invoke(new HitReport(this, source, damage, damage, BlockResult.None, isSkill));
    }

    public void ApplyBleed(IDamageable source, float dps, float duration, float endDamage)
    {
        double now = Time.timeAsDouble;
        bleeds.Add(new Dot
        {
            source = source,
            dps = dps,
            end = now + duration,
            nextTick = now + EffectTick,
            endDamage = endDamage
        });
    }

    public void ApplySwarm(IDamageable source, int count, float dpsPerBug, float duration,
                           AttackKind shakeKind, int bugsPerPress)
    {
        double now = Time.timeAsDouble;
        swarm = new Swarm
        {
            source = source,
            count = count,
            dpsPerBug = dpsPerBug,
            end = now + duration,
            nextTick = now + EffectTick,
            shakeKind = shakeKind,
            bugsPerPress = Mathf.Max(1, bugsPerPress)
        };
        SwarmChanged?.Invoke(swarm.count);
    }

    void ShakeSwarm(int amount)
    {
        if (swarm == null) return;

        swarm.count -= amount;
        if (swarm.count <= 0) swarm = null;
        SwarmChanged?.Invoke(SwarmCount);
    }

    public void ClearEffects()
    {
        bleeds.Clear();
        slowEnd = 0;
        if (swarm != null)
        {
            swarm = null;
            SwarmChanged?.Invoke(0);
        }
    }

    void TickEffects()
    {
        double now = Time.timeAsDouble;

        for (int i = bleeds.Count - 1; i >= 0; i--)
        {
            Dot b = bleeds[i];

            while (b.nextTick <= now && b.nextTick <= b.end)
            {
                TakeDirectDamage(b.source, b.dps * (float)EffectTick, true);
                b.nextTick += EffectTick;
            }

            if (now >= b.end)
            {
                TakeDirectDamage(b.source, b.endDamage, true);   // dano extra al terminar
                bleeds.RemoveAt(i);
            }
        }

        while (swarm != null && swarm.nextTick <= now && swarm.nextTick <= swarm.end)
        {
            TakeDirectDamage(swarm.source, swarm.count * swarm.dpsPerBug * (float)EffectTick, true);
            if (swarm != null) swarm.nextTick += EffectTick;
        }

        if (swarm != null && now >= swarm.end)
        {
            swarm = null;
            SwarmChanged?.Invoke(0);
        }
    }
    static bool IsAlive(IDamageable d)
        => d != null && (!(d is UnityEngine.Object o) || o);
}