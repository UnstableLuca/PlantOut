using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fighter : MonoBehaviour, IFighter
{
    #region Variables
    public enum FighterState { Idle, Acting, Blocking, Channeling, Stunned }
    
    [Header("Identidad y Combate")]
    [SerializeField] Fighter rival;
    public bool isLeftPlayer = true;

    [Header("Referencias por defecto")]
    [SerializeField] Transform defaultAimPoint;
    [SerializeField] Transform defaultProjectileOrigin;

    [Header("Barra de energía")]
    [SerializeField, Min(1)] int maxSkillUses = 5;
    [SerializeField, Range(0f, 5f)] float skillBar = 0f;

    // Acciones Base (Inyectadas por PlantCharacterModule desde el prefab hijo)
    private PlayerAction meleeAction;
    private PlayerAction rangedAction;
    private BlockAction blockAction;
    private SkillController skillController;

    // Referencias Visuales (Inyectadas por PlantCharacterModule)
    public Animator CharacterAnimator { get; private set; }
    public Transform AimPoint { get; private set; }
    public Transform ProjectileOrigin { get; private set; }
    public Transform StemTransform { get; private set; }

    // Estado interno
    private FighterState state = FighterState.Idle;
    private bool inputEnabled = true;

    // Bloqueo y Parry
    private bool blockHeld = false;
    private double blockPressTime = -10.0;

    // Acción en curso
    private PlayerAction currentAction;
    private ActionContext currentActionCtx;
    private Coroutine actionCoroutine;

    // Estados alterados
    private float stunTimer = 0f;
    private float slowTimer = 0f;
    private float actionSpeed = 1f;

    // Enjambre (Ultimate de Maxilón)
    private bool hasSwarm = false;
    private int swarmBugsRemaining = 0;
    private float swarmDpsPerBug = 0f;
    private float swarmTimer = 0f;
    private float swarmTickAccum = 0f;
    private AttackKind swarmShakeKind = AttackKind.Melee;
    private int swarmRemovePerPress = 1;
    private IDamageable swarmSource;

    // Sangrados activos (Aros de Rosaros)
    private readonly List<Coroutine> activeBleeds = new List<Coroutine>();

    // Bichos asignados a este lado
    private readonly List<IDamageable> registeredBugs = new List<IDamageable>();

    // Eventos para ScoreSystem, UI y Habilidades
    public event Action<HitReport> HitReceived;
    public event Action<float> OnSkillBarChanged;
    #endregion

    #region Public Properties & IFighter
    // =========================================================================
    // PROPIEDADES PÚBLICAS E INTERFAZ IFighter
    // =========================================================================
    public Fighter Rival => rival;
    public AttackSide RivalSide => isLeftPlayer ? AttackSide.Right : AttackSide.Left;
    public FighterState State => state;
    public bool InputEnabled { get => inputEnabled; set => inputEnabled = value; }

    public int MaxSkillUses => maxSkillUses;
    public float SkillBar => skillBar;
    public float ActionSpeed => actionSpeed;

    public bool CanAct =>
        inputEnabled &&
        state == FighterState.Idle;
    #endregion

    #region Awake, Start & Update
    void Awake()
    {
        AimPoint = defaultAimPoint != null ? defaultAimPoint : transform;
        ProjectileOrigin = defaultProjectileOrigin != null ? defaultProjectileOrigin : transform;
    }

    void Start()
    {
        OnSkillBarChanged?.Invoke(skillBar);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (state == FighterState.Stunned)
        {
            stunTimer -= dt;
            if (stunTimer <= 0f)
            {
                state = FighterState.Idle;
                if (CharacterAnimator != null) CharacterAnimator.SetBool("Stunned", true);
                if (blockHeld) TryStartBlock();
            }
        }

        if (slowTimer > 0f)
        {
            slowTimer -= dt;
            if (slowTimer <= 0f)
            {
                actionSpeed = 1f;
            }
        }

        if (hasSwarm)
        {
            UpdateSwarm(dt);
        }
    }
#endregion

    #region Child Link
    public void SetRival(Fighter newRival)
    {
        rival = newRival;
    }

    public void RegisterCharacter(
        PlayerAction melee,
        PlayerAction ranged,
        PlayerAction block,
        Animator animator,
        Transform aimPoint,
        Transform projectileOrigin,
        Transform stemTransform,
        SkillController childSkillController = null)
    {
        meleeAction = melee;
        rangedAction = ranged;
        blockAction = block as BlockAction;
        CharacterAnimator = animator;

        if (aimPoint != null) AimPoint = aimPoint;
        if (projectileOrigin != null) ProjectileOrigin = projectileOrigin;
        StemTransform = stemTransform;

        skillController = childSkillController != null
            ? childSkillController
            : GetComponentInChildren<SkillController>();

        PlantHeight heightSystem = GetComponent<PlantHeight>();
        if (heightSystem != null && stemTransform != null)
        {
            heightSystem.SendMessage("SetStemTransform", stemTransform, SendMessageOptions.DontRequireReceiver);
        }
    }
    #endregion

    #region Inputs
    public void RequestAttack(AttackKind kind)
    {
        if (!inputEnabled) return;

        // Si tiene encima el enjambre de Maxilón, pulsar el botón indicado quita bichos
        if (hasSwarm && kind == swarmShakeKind)
        {
            swarmBugsRemaining -= swarmRemovePerPress;
            if (swarmBugsRemaining <= 0)
            {
                ClearSwarm();
            }
        }

        // Dar prioridad a interceptaciones (ej: Maxilón comiendo nubes en modo caza)
        if (skillController != null && skillController.InterceptAttack(kind))
            return;

        PlayerAction chosen = kind == AttackKind.Melee ? meleeAction : rangedAction;
        if (chosen == null) return;

        ActionContext ctx = new ActionContext(this, Time.timeAsDouble);
        if (!chosen.CanExecute(ctx)) return;

        StartAction(chosen, ctx);
    }

    public void RequestBlockPress()
    {
        if (!inputEnabled) return;

        blockHeld = true;

        if (skillController != null && skillController.InterceptBlockPress())
            return;

        TryStartBlock();
    }

    public void RequestBlockRelease()
    {
        blockHeld = false;

        if (state == FighterState.Blocking)
        {
            EndBlock();
            state = FighterState.Idle;
        }
    }

    public void RequestSkillPress()
    {
        if (!inputEnabled) return;
        skillController?.OnSkillPressed();
    }

    public void RequestSkillRelease()
    {
        if (!inputEnabled) return;
        skillController?.OnSkillReleased();
    }
    #endregion

    #region Actions Phases
    // EJECUCIÓN DE ACCIONES POR FASES (Startup -> Active -> Recovery)
    private void TryStartBlock()
    {
        if (!CanAct) return;

        if (blockAction != null)
        {
            ActionContext ctx = new ActionContext(this, Time.timeAsDouble);
            if (!blockAction.CanExecute(ctx)) return;
            blockAction.Execute(ctx);
        }
        else
        {
            BeginBlock(Time.timeAsDouble);
        }
    }

    private void StartAction(PlayerAction action, ActionContext ctx)
    {
        InterruptCurrentAction();

        currentAction = action;
        currentActionCtx = ctx;
        state = FighterState.Acting;

        action.Execute(ctx);
        actionCoroutine = StartCoroutine(ActionRoutine(action, ctx));
    }

    private IEnumerator ActionRoutine(PlayerAction action, ActionContext ctx)
    {
        float spd = Mathf.Max(0.1f, actionSpeed);

        // Fase 1: Startup
        action.OnPhaseEnter(ctx, ActionPhase.Startup);
        if (action.startup > 0f)
            yield return new WaitForSeconds(action.startup / spd);

        // Fase 2: Active (aquí AttackAction lanza el proyectil o resuelve el impacto)
        action.OnPhaseEnter(ctx, ActionPhase.Active);
        if (action.active > 0f)
            yield return new WaitForSeconds(action.active / spd);

        // Fase 3: Recovery
        action.OnPhaseEnter(ctx, ActionPhase.Recovery);
        if (action.recovery > 0f)
            yield return new WaitForSeconds(action.recovery / spd);

        currentAction = null;
        actionCoroutine = null;
        if (state == FighterState.Acting)
        {
            state = FighterState.Idle;
            if (blockHeld) TryStartBlock();
        }
    }

    private void InterruptCurrentAction()
    {
        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }

        if (currentAction != null)
        {
            currentAction.OnInterrupted(currentActionCtx);
            currentAction = null;
        }
    }

    public void BeginBlock(double pressTime)
    {
        state = FighterState.Blocking;
        blockPressTime = pressTime;
        if (CharacterAnimator != null) CharacterAnimator.SetBool("Blocking", true);
    }

    public void EndBlock()
    {
        if (CharacterAnimator != null) CharacterAnimator.SetBool("Blocking", false);
    }
    #endregion

    #region Skill Channeling
    public bool BeginChannel()
    {
        if (!CanAct) return false;
        state = FighterState.Channeling;
        return true;
    }

    public void EndChannel()
    {
        if (state == FighterState.Channeling)
        {
            state = FighterState.Idle;
            if (blockHeld) TryStartBlock();
        }
    }
#endregion

    #region Skill Bar
    public bool TrySpendUses(int uses)
    {
        if (uses <= 0) return true;
        if (skillBar + 0.0001f < uses) return false;

        skillBar = Mathf.Max(0f, skillBar - uses);
        OnSkillBarChanged?.Invoke(skillBar);
        return true;
    }

    public void AddBar(float amount)
    {
        if (amount <= 0f) return;
        if (skillController != null && !skillController.AllowsBarGain) return;

        AddBarPassive(amount);
    }

    public void AddBarPassive(float amount)
    {
        skillBar = Mathf.Clamp(skillBar + amount, 0f, maxSkillUses);
        OnSkillBarChanged?.Invoke(skillBar);
    }
    #endregion

    #region Combat
    public IDamageable GetAutoTarget(bool includeBugs)
    {
        if (includeBugs)
        {
            for (int i = registeredBugs.Count - 1; i >= 0; i--)
            {
                IDamageable b = registeredBugs[i];
                if (b is UnityEngine.Object obj && !obj)
                {
                    registeredBugs.RemoveAt(i);
                    continue;
                }
                if (b is IShootable shootable && shootable.CanBeTargeted)
                    return b;
            }
        }

        return rival;
    }

    public float ModifyAttackDamage(AttackKind kind, float baseDamage)
    {
        return skillController != null
            ? skillController.ModifyAttackDamage(kind, baseDamage)
            : baseDamage;
    }

    public void PlayAnimation(string trigger)
    {
        if (CharacterAnimator != null && !string.IsNullOrEmpty(trigger))
        {
            CharacterAnimator.SetTrigger(trigger);
        }
    }

    public BlockResult ReceiveHit(in HitData hit)
    {
        BlockResult result = BlockResult.None;
        float finalDamage = hit.Damage;

        // Evaluar Bloqueo / Parry
        if (state == FighterState.Blocking)
        {
            double now = Time.timeAsDouble;
            float window = blockAction != null ? blockAction.parryWindow : 0.14f;
            bool isParry = hit.Parryable && (now - blockPressTime <= window);

            if (isParry)
            {
                result = BlockResult.Parried;
                float scale = hit.ParryScale >= 0f ? hit.ParryScale : 0f;
                finalDamage = hit.Damage * scale;

                float reward = blockAction != null ? blockAction.parryBarReward : 1f;
                AddBar(reward);
            }
            else
            {
                result = BlockResult.Blocked;
                float defaultScale = blockAction != null ? blockAction.blockDamageMultiplier : 0.2f;
                float scale = hit.BlockScale >= 0f ? hit.BlockScale : defaultScale;
                finalDamage = hit.Damage * scale;

                float reward = blockAction != null ? blockAction.blockBarReward : 0.1f;
                AddBar(reward);
            }
        }

        // Notificar a ScoreSystem
        HitReceived?.Invoke(new HitReport(hit.Source, this, finalDamage, result));

        // Si entró limpio, evaluar interrupciones y stun
        if (result == BlockResult.None)
        {
            bool hasSuperArmor = skillController != null && skillController.SuperArmor;

            if (hit.Interrupts && !hasSuperArmor)
            {
                if (state == FighterState.Acting)
                {
                    InterruptCurrentAction();
                    state = FighterState.Idle;
                }
                else if (state == FighterState.Channeling)
                {
                    skillController?.OnChannelInterrupted();
                    state = FighterState.Idle;
                }
            }

            if (hit.Hitstun > 0f && !hasSuperArmor)
            {
                Stun(hit.Hitstun);
            }
            else if (finalDamage > 0f && CharacterAnimator != null)
            {
                CharacterAnimator.SetTrigger("Hit");
            }
        }

        return result;
    }
#endregion

    #region Altered States
    public void Stun(float duration)
    {
        if (duration <= 0f)
        {
            CharacterAnimator.SetBool("Stunned", false);
            return;
        }

        if (state == FighterState.Acting) InterruptCurrentAction();
        else if (state == FighterState.Blocking) EndBlock();
        else if (state == FighterState.Channeling) skillController?.OnChannelInterrupted();

        state = FighterState.Stunned;
        stunTimer = Mathf.Max(stunTimer, duration);
        if (CharacterAnimator != null) CharacterAnimator.SetBool("Stunned", true);
    }

    public void ApplySlow(float factor, float duration)
    {
        actionSpeed = Mathf.Clamp(factor, 0.1f, 2f);
        slowTimer = Mathf.Max(slowTimer, duration);
    }

    public void ApplyBleed(IDamageable source, float dps, float duration, float endBurstDamage)
    {
        if (duration <= 0f && endBurstDamage <= 0f) return;
        Coroutine c = StartCoroutine(BleedRoutine(source, dps, duration, endBurstDamage));
        activeBleeds.Add(c);
    }

    private IEnumerator BleedRoutine(IDamageable source, float dps, float duration, float endBurst)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
            if (dps > 0f)
                ApplyDirectDotDamage(source, dps);
        }

        if (endBurst > 0f)
            ApplyDirectDotDamage(source, endBurst);
    }

    public void ApplySwarm(IDamageable source, int bugCount, float dpsPerBug, float duration,
                           AttackKind shakeButton, int removePerPress)
    {
        hasSwarm = true;
        swarmSource = source;
        swarmBugsRemaining = Mathf.Max(1, bugCount);
        swarmDpsPerBug = dpsPerBug;
        swarmTimer = duration;
        swarmTickAccum = 0f;
        swarmShakeKind = shakeButton;
        swarmRemovePerPress = Mathf.Max(1, removePerPress);
    }

    private void UpdateSwarm(float dt)
    {
        swarmTimer -= dt;
        swarmTickAccum += dt;

        while (swarmTickAccum >= 1f && swarmBugsRemaining > 0)
        {
            swarmTickAccum -= 1f;
            float tickDmg = swarmBugsRemaining * swarmDpsPerBug;
            if (tickDmg > 0f)
                ApplyDirectDotDamage(swarmSource, tickDmg);
        }

        if (swarmTimer <= 0f || swarmBugsRemaining <= 0)
        {
            ClearSwarm();
        }
    }

    private void ClearSwarm()
    {
        hasSwarm = false;
        swarmBugsRemaining = 0;
    }

    public void ClearEffects(bool resetBar)
    {
        InterruptCurrentAction();

        foreach (Coroutine c in activeBleeds)
            if (c != null) StopCoroutine(c);
        activeBleeds.Clear();

        ClearSwarm();

        stunTimer = 0f;
        slowTimer = 0f;
        actionSpeed = 1f;

        blockHeld = false;
        EndBlock();
        state = FighterState.Idle;
        if (CharacterAnimator != null) CharacterAnimator.SetBool("Stunned", false);

        skillController?.ResetState();

        if (resetBar)
        {
            skillBar = 0f;
            OnSkillBarChanged?.Invoke(skillBar);
        }
    }

    private void ApplyDirectDotDamage(IDamageable source, float amount)
    {
        if (amount <= 0f) return;
        HitReceived?.Invoke(new HitReport(source, this, amount, BlockResult.None));
    }
#endregion

    #region Bugs
    public void RegisterBug(AttackSide side, IDamageable bug)
    {
        if (bug != null && !registeredBugs.Contains(bug))
            registeredBugs.Add(bug);
    }

    public void UnregisterBug(IDamageable bug)
    {
        if (bug != null)
            registeredBugs.Remove(bug);
    }

    public void CollectBugs(List<IDamageable> buffer)
    {
        buffer.Clear();
        for (int i = registeredBugs.Count - 1; i >= 0; i--)
        {
            IDamageable b = registeredBugs[i];
            if (b is UnityEngine.Object obj && !obj)
            {
                registeredBugs.RemoveAt(i);
                continue;
            }
            buffer.Add(b);
        }
    }
    #endregion
}