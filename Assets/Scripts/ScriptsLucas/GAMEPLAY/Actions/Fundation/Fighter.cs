using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum FighterState { Idle, Acting, Blocking, Stunned, Channeling}

public readonly struct HitReport
{
    public readonly Fighter Victim;
    public readonly IDamageable Source;
    public readonly float RawDamage;
    public readonly float FinalDamage;
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
    [SerializeField] SkillController skills;

    [Header("Lados")]
    [SerializeField] AttackSide rivalSide = AttackSide.Right;

    [Header("Barra de habilidad")]
    [SerializeField, Min(1)] int maxSkillUses = 5;

    [Header("Parry")]
    [Tooltip("Stun que sufre el atacante cuando le parreas")]
    [SerializeField] float parryStunOnAttacker = 0.5f;

    [Header("Animaciones")]
    [SerializeField] Animator animator;
    [SerializeField] string hitTrigger = "Hit";
    [SerializeField] string parryTrigger = "Parry";
    [SerializeField] string blockEndTrigger = "BlockEnd";

    //ESTADOS PUBLICOS

    public Fighter Rival { get; set; }
    public bool InputEnabled { get; set; } = true;
    public FighterState State => state;
    public int MaxSkillUses => maxSkillUses;
    public float SkillBar { get; private set; }
    public bool CanAct => InputEnabled && state == FighterState.Idle && Time.timeAsDouble >= lockUntil;



    public event Action<HitReport> HitReceived;
    public event Action<float> SkillBarChanged;

    //ESTADOS INTERNOS

    FighterState state = FighterState.Idle;

    PlayerAction currentAction;
    ActionContext currentCtx;
    ActionPhase phase;
    double actionStart;
    float actionDuration;

    double blockPressTime;
    double lockUntil; //anti-spam
    double stunEnd;

    readonly List<IDamageable> leftBugs = new List<IDamageable>();
    readonly List<IDamageable> rightBugs = new List<IDamageable>();

    bool Armored => skills != null && skills.SuperArmor;

    void Awake()
    {
        if (skills == null) skills = GetComponent<SkillController>();
    }
    //INPUTS

    public bool RequestAttack(AttackKind kind)
    {
        AttackAction action = kind == AttackKind.Melee ? meleeAttack : rangedAttack;
        return action != null && TryStart(action);
    }


    public void RequestSkillPress() { if (skills != null) skills.OnSkillPressed(); }
    public void RequestSkillRelease() { if (skills != null) skills.OnSkillReleased(); }
    public bool RequestBlockPress()
    {
        if (block == null) return false;

        var ctx = new ActionContext(this, Time.timeAsDouble);
        if (!block.CanExecute(ctx)) return false;

        currentAction = block;
        currentCtx = ctx;
        block.Execute(ctx);
        return true;
    }

    public void RequestBlockRelease() => EndBlock();

    //EJEC DE ACCIONES

    bool TryStart(PlayerAction action)
    {
        var ctx = new ActionContext(this, Time.timeAsDouble);
        if (!action.CanExecute(ctx)) return false;

        currentAction = action;
        currentCtx = ctx;
        actionStart = ctx.Now;
        actionDuration = action.GetTotalDuration(ctx);
        phase = ActionPhase.Startup;
        state = FighterState.Acting;

        action.Execute(ctx);
        return true;
    }

    void Update()
    {
        switch(state)
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
        if (Armored) return;

        Interrupt();
        state = FighterState.Stunned;
        stunEnd = Math.Max(stunEnd, Time.timeAsDouble + duration);
    }
    //CHANELLINIG

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

    //IFIGHTER

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

    public void AddBar(float amount) => SetBar(SkillBar + amount);
    
    void SetBar(float value)
    {
        SkillBar = Mathf.Clamp(value, 0f, maxSkillUses);
        SkillBarChanged?.Invoke(SkillBar);
    }

    public float ModifyAttackDamage(AttackKind kind, float baseDamage)
        => skills != null ? skills.ModifyAttackDamage(kind, baseDamage) : baseDamage;

    public IDamageable GetAutoTarget()
    {
        AttackSide bugSide = rivalSide == AttackSide.Left ? AttackSide.Right : AttackSide.Left;
        return GetTarget(bugSide) ?? GetTarget(rivalSide);
    }

    public IDamageable GetTarget(AttackSide side)
    {
        if (side == rivalSide)
            return IsAlive(Rival) ? Rival : null;

        List<IDamageable> lane = side == AttackSide.Left ? leftBugs : rightBugs;
        while (lane.Count > 0)
        {
            if (IsAlive(lane[0])) return lane[0];
            lane.RemoveAt(0);   
        }
        return null;
    }

    //RECIBIR GOLPES

    public BlockResult ReceiveHit(in HitData hit)
    {
        BlockResult result = BlockResult.None;
        float damage = hit.Damage;

        if (state == FighterState.Blocking && block != null)
        {
            result = block.Evaluate(blockPressTime, Time.timeAsDouble);
            if (result == BlockResult.Parried && !hit.Parryable) result = BlockResult.Blocked;

            damage = block.ModifyDamage(result, damage);
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
            if (!Armored)
            {
                if (hit.Hitstun > 0f) Stun(hit.Hitstun);
                else Interrupt();
            }
            PlayAnimation(hitTrigger);
        }

        HitReceived?.Invoke(new HitReport(this, hit.Source, hit.Damage, damage, result, hit.IsSkill));
        return result;
    }

    //BICHOS
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

    //no se que se fuma unity pero ponen de usarlo asi
    static bool IsAlive(IDamageable d)
        => d != null && (!(d is UnityEngine.Object o) || o);
}
