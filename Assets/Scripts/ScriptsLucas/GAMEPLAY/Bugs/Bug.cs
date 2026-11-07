using System;
using UnityEngine;

public class Bug : MonoBehaviour, IDamageable, IShootable
{
    [Header("Idle (flota cerca de la planta)")]
    [SerializeField, Min(0.1f)] float idleMin = 2f;
    [SerializeField, Min(0.1f)] float idleMax = 5f;
    [SerializeField] float bobAmplitude = 0.25f;
    [SerializeField] float bobSpeed = 2f;
    [Tooltip("Velocidad con la que vuelve a su sitio tras picar")]
    [SerializeField] float hoverSpeed = 6f;

    [Header("Aviso (ventana para matarlo)")]
    [SerializeField, Min(0.1f)] float telegraphDuration = 0.7f;
    [Tooltip("Margen extra: un disparo ya lanzado antes de que acabe el aviso aun lo mata si llega justo despues")]
    [SerializeField, Min(0f)] float killGrace = 0.15f;
    [Tooltip("Parpadeos por segundo del destello blanco")]
    [SerializeField, Min(1f)] float flashRate = 12f;
    [Tooltip("Si esta activo, solo muere por disparos (Ranged). Si no, cualquier golpe en la ventana lo mata")]
    [SerializeField] bool onlyRangedKills = true;

    [Header("Picado")]
    [SerializeField] float swoopSpeed = 14f;
    [Tooltip("Dano del picado. Los puntos que quita = dano final x bugPenaltyScale del ScoreSystem")]
    [SerializeField] float damage = 8f;
    [SerializeField] float hitstun = 0.3f;
    [Tooltip("Aturdimiento del bicho cuando le parreas el picado")]
    [SerializeField] float parryStun = 1.2f;

    [Header("Llegada")]
    [SerializeField] float arriveSpeed = 4f;

    [Header("Muerte")]
    [Tooltip("Puntos que se lleva quien lo mata")]
    public float killPoints = 5f;
    [SerializeField] float deathDelay = 0.2f;

    [Header("Visual (opcional)")]
    [Tooltip("Hijo blanco que se enciende y apaga durante el aviso (silueta blanca o sprite blanco)")]
    [SerializeField] GameObject flashOverlay;
    [Tooltip("Parametros: bool Telegraph; triggers Swoop, Hurt, Die")]
    [SerializeField] Animator animator;

    enum BugState { Arriving, Idle, Telegraph, Swoop, Stunned, Dead }

    BugState state = BugState.Arriving;
    Fighter target;
    AttackSide side;
    Vector3 hoverPos;
    Vector3 strikePos;
    float timer;
    float stateTime;
    float bobPhase;
    double vulnerableUntil;
    bool flashOn;

    public event Action<Bug> TelegraphStarted;

    public event Action<Bug, IDamageable> Died;

    // ---------- IDamageable / IShootable ----------

    public Transform AimPoint => transform;

    public bool CanBeTargeted =>
        (state == BugState.Telegraph || state == BugState.Swoop) && Time.timeAsDouble <= vulnerableUntil;

    public void Init(Fighter targetPlant, AttackSide bugSide, Vector3 hoverPosition, Vector3 strikePosition)
    {
        target = targetPlant;
        side = bugSide;
        hoverPos = hoverPosition;
        strikePos = strikePosition;
        bobPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        state = BugState.Arriving;
        SetFlash(false);
    }

    void Update()
    {
        if (state == BugState.Dead) return;

        if (target == null)   // la planta ya no existe (fin de ronda, escena descargada)
        {
            Destroy(gameObject);
            return;
        }

        float dt = Time.deltaTime;
        stateTime += dt;

        switch (state)
        {
            case BugState.Arriving:
                transform.position = Vector3.MoveTowards(transform.position, hoverPos, arriveSpeed * dt);
                if ((transform.position - hoverPos).sqrMagnitude < 0.0001f)
                {
                    target.RegisterBug(side, this);
                    BeginIdle();
                }
                break;

            case BugState.Idle:
                Hover(dt);
                timer -= dt;
                if (timer <= 0f) BeginTelegraph();
                break;

            case BugState.Telegraph:
                Hover(dt);
                SetFlash(((int)(stateTime * flashRate * 2f)) % 2 == 0);   // parpadeo blanco
                timer -= dt;
                if (timer <= 0f) BeginSwoop();
                break;

            case BugState.Swoop:
                transform.position = Vector3.MoveTowards(transform.position, strikePos, swoopSpeed * dt);
                if ((transform.position - strikePos).sqrMagnitude < 0.0001f) Strike();
                break;

            case BugState.Stunned:
                Hover(dt);
                timer -= dt;
                if (timer <= 0f) BeginIdle();
                break;
        }
    }

    //  FASES

    void BeginIdle()
    {
        state = BugState.Idle;
        stateTime = 0f;
        timer = UnityEngine.Random.Range(idleMin, Mathf.Max(idleMin, idleMax));   // duracion aleatoria
        SetFlash(false);
        if (animator != null) animator.SetBool("Telegraph", false);
    }

    void BeginTelegraph()
    {
        state = BugState.Telegraph;
        stateTime = 0f;
        timer = telegraphDuration;
        vulnerableUntil = Time.timeAsDouble + telegraphDuration + killGrace;

        if (animator != null) animator.SetBool("Telegraph", true);
        TelegraphStarted?.Invoke(this);
    }

    void BeginSwoop()
    {
        state = BugState.Swoop;
        stateTime = 0f;
        SetFlash(false);
        if (animator != null)
        {
            animator.SetBool("Telegraph", false);
            animator.SetTrigger("Swoop");
        }
    }

    void Strike()
    {
        BlockResult result = target.ReceiveHit(new HitData(this, damage, hitstun, isSkill: false));

        if (result == BlockResult.Parried)
        {
            state = BugState.Stunned;
            stateTime = 0f;
            timer = parryStun;
            if (animator != null) animator.SetTrigger("Hurt");
        }
        else
        {
            BeginIdle();   // vuelve flotando a su sitio y repite
        }
    }

    void Hover(float dt)
    {
        Vector3 desired = hoverPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobAmplitude);
        transform.position = Vector3.MoveTowards(transform.position, desired, hoverSpeed * dt);
    }

    void SetFlash(bool on)
    {
        if (flashOn == on) return;
        flashOn = on;
        if (flashOverlay != null) flashOverlay.SetActive(on);
    }

    //  IDamageable

    public BlockResult ReceiveHit(in HitData hit)
    {
        // Fuera de la ventana el bicho esta fuera de alcance: el disparo no hace nada
        if (!CanBeTargeted) return BlockResult.Ignored;
        if (onlyRangedKills && hit.Kind != AttackKind.Ranged) return BlockResult.Ignored;

        // Solo lo puede matar la planta que tiene al lado (a la que ataca)
        if (!ReferenceEquals(hit.Source, target)) return BlockResult.Ignored;

        Die(hit.Source);
        return BlockResult.None;
    }

    void Die(IDamageable killer)
    {
        state = BugState.Dead;
        SetFlash(false);
        if (animator != null) animator.SetTrigger("Die");

        target?.UnregisterBug(this);
        Died?.Invoke(this, killer);
        Destroy(gameObject, deathDelay);
    }

    void OnDestroy()
    {
        // Por si lo destruye otra cosa (cambio de escena...) sin pasar por Die
        if (state != BugState.Dead && target != null) target.UnregisterBug(this);
    }
}