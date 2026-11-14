using System;
using System.Collections.Generic;
using UnityEngine;

public class MaxilonSkills : SkillController
{
    [Header("Invocar bichos (0 cargas)")]
    [Tooltip("Vacio = lo busca en la escena")]
    [SerializeField] BugSpawner spawner;
    [SerializeField, Min(0)] int ownBugs = 5;
    [SerializeField, Min(0)] int rivalBugs = 2;
    [Tooltip("Segundos entre un bicho y el siguiente")]
    [SerializeField, Min(0.1f)] float summonInterval = 1f;
    [Tooltip("Tiempo del eructo en el que no puede hacer otra cosa")]
    [SerializeField] float burpLockTime = 0.8f;

    [Header("Modo caza")]
    [SerializeField] Transform cloudLeft;
    [SerializeField] Transform cloudRight;
    [SerializeField] Transform cloudTop;
    [Tooltip("Radio de cada nube: los bichos dentro de este radio se pueden comer")]
    [SerializeField, Min(0.1f)] float cloudRadius = 1.2f;
    [Tooltip("Objetos de las nubes de olor: se activan en modo caza")]
    [SerializeField] GameObject[] cloudVisuals;
    [Tooltip("Segundos manteniendo la habilidad para salir del modo caza")]
    [SerializeField, Min(0.1f)] float exitHuntHold = 0.5f;
    [Tooltip("Cargas que da cada bicho comido")]
    [SerializeField, Min(0f)] float barPerBug = 1f;
    [Tooltip("Cada cuanto se manda a los bichos nuevos a una nube")]
    [SerializeField, Min(0.1f)] float attractInterval = 0.5f;

    [Header("Escupitajo (1-4 cargas)")]
    [Tooltip("Prefab del escupitajo (con el componente Projectile). Vacio = efecto instantaneo")]
    [SerializeField] Projectile spitPrefab;
    [Tooltip("Boca de Maxilon. Vacio = el Projectile Origin del Fighter")]
    [SerializeField] Transform mouthPoint;
    [SerializeField] float spitSpeed = 25f;
    [SerializeField, Min(1)] int maxLevels = 4;
    [SerializeField, Min(0.1f)] float holdPerLevel = 0.5f;
    [Tooltip("Segundos de stun al rival. Indice = nivel (el 0 no se usa)")]
    [SerializeField] float[] stunByLevel = { 0f, 1f, 2f, 3f, 4f };
    [Tooltip("Dano del escupitajo (0 = solo el stun)")]
    [SerializeField] float spitDamage = 0f;
    [SerializeField] float spitLockTime = 0.4f;

    [Header("Ultimate (5 cargas): bola de bichos")]
    [Tooltip("Prefab de la bola gigante (con el componente Projectile). Vacio = impacto instantaneo")]
    [SerializeField] Projectile giantBallPrefab;
    [SerializeField] float ballSpeed = 15f;
    [Tooltip("Segundos de preparacion antes de lanzarla")]
    [SerializeField] float ultimateWindup = 0.8f;
    [SerializeField, Min(1)] int swarmBugs = 10;
    [SerializeField] float swarmDpsPerBug = 20f;
    [SerializeField] float swarmDuration = 10f;
    [Tooltip("Boton con el que el rival se quita los bichos de encima")]
    [SerializeField] AttackKind swarmShakeButton = AttackKind.Melee;
    [Tooltip("Bichos que se quita el rival por pulsacion")]
    [SerializeField, Min(1)] int bugsRemovedPerPress = 1;

    [Header("Animator (vacio = no se usa)")]
    [Tooltip("Si se deja vacio busca el Animator en los hijos")]
    [SerializeField] Animator animator;
    [Tooltip("Bool: true mientras esta en modo caza")]
    [SerializeField] string huntingParameter = "Hunting";
    [Tooltip("Bool: true mientras mantienes la habilidad cargando el escupitajo")]
    [SerializeField] string chargingParameter = "Charging";
    [SerializeField] string burpTrigger = "Burp";
    [SerializeField] string eatLeftTrigger = "EatLeft";
    [SerializeField] string eatRightTrigger = "EatRight";
    [SerializeField] string eatUpTrigger = "EatUp";
    [SerializeField] string spitTrigger = "Spit";
    [SerializeField] string ultimateTrigger = "UltimateThrow";

    enum Phase { Idle, Holding, Casting, UltimateWindup }

    Phase phase;
    int level;
    float holdTimer;
    float timer;

    bool hunting;
    bool exitHolding;
    float exitTimer;
    float attractTimer;
    float summonBlockedUntil;   // Time.time

    readonly List<IDamageable> bugBuffer = new List<IDamageable>();
    readonly Dictionary<Bug, int> assigned = new Dictionary<Bug, int>();   // bicho -> nube a la que va
    readonly List<Bug> deadBuffer = new List<Bug>();

    // Estado publico (UI) 

    public bool Hunting => hunting;
    public int Level => level;

    public event Action<bool> HuntingChanged;
    public event Action<int> BugsEaten;

    protected override void Awake()
    {
        base.Awake();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        SetCloudVisuals(false);
    }

    //  INPUT
    public override void OnSkillPressed()
    {
        // En modo caza, mantener la habilidad sirve para salir
        if (hunting)
        {
            exitHolding = true;
            exitTimer = 0f;
            return;
        }

        if (phase != Phase.Idle || !fighter.CanAct) return;

        int charges = Mathf.FloorToInt(fighter.SkillBar + 0.0001f);

        if (charges >= fighter.MaxSkillUses)
        {
            StartUltimate();
            return;
        }

        if (charges == 0)
        {
            TrySummon();
            return;
        }

        // 1-4 cargas: empieza a mantener. Si sueltas antes del primer nivel, es la invocacion.
        if (!fighter.BeginChannel()) return;

        phase = Phase.Holding;
        holdTimer = 0f;
        level = 0;
        SetCharging(true);
    }

    public override void OnSkillReleased()
    {
        if (hunting)
        {
            exitHolding = false;
            return;
        }

        if (phase != Phase.Holding) return;

        SetCharging(false);

        if (level > 0)
        {
            Spit(level);
            level = 0;
        }
        else
        {
            EndPhase();      // toque rapido: invocacion
            TrySummon();
        }
    }

    public override bool InterceptAttack(AttackKind kind)
    {
        if (!hunting) return false;
        EatCloud(kind == AttackKind.Melee ? 0 : 1);
        return true;
    }
    public override bool InterceptBlockPress()
    {
        if (!hunting) return false;
        EatCloud(2);
        return true;
    }

    public override void OnChannelInterrupted()
    {
        // Te golpean: lo que estuvieras preparando se pierde (las cargas gastadas no se devuelven)
        if (phase == Phase.Holding)
        {
            SetCharging(false);
            level = 0;
        }

        phase = Phase.Idle;
    }

    //  UPDATE

    void Update()
    {
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
                    level++;
                }
                break;

            case Phase.Casting:
                timer -= dt;
                if (timer <= 0f) EndPhase();
                break;

            case Phase.UltimateWindup:
                timer -= dt;
                if (timer <= 0f)
                {
                    LaunchBall();
                    EndPhase();
                }
                break;
        }

        if (hunting)
        {
            attractTimer -= Time.deltaTime;
            if (attractTimer <= 0f)
            {
                attractTimer = attractInterval;
                AttractBugs();
            }

            if (exitHolding)
            {
                exitTimer += Time.deltaTime;
                if (exitTimer >= exitHuntHold) ExitHunt();
            }
        }
    }

    void EndPhase()
    {
        phase = Phase.Idle;
        fighter.EndChannel();
    }

    //  INVOCAR Y MODO CAZA

    bool TrySummon()
    {
        if (hunting || Time.time < summonBlockedUntil) return false;

        if (spawner == null) spawner = FindAnyObjectByType<BugSpawner>();
        if (spawner == null) return false;

        // Hasta que no haya bichos en pantalla no se puede volver a invocar
        if (spawner.AliveTotal > 0) return false;

        if (!fighter.BeginChannel()) return false;

        phase = Phase.Casting;
        timer = burpLockTime;
        Trigger(burpTrigger);

        spawner.SummonBugs(fighter, ownBugs, summonInterval);
        if (fighter.Rival != null) spawner.SummonBugs(fighter.Rival, rivalBugs, summonInterval);

        // Hasta que hayan salido todos (el ultimo sale a los N-1 intervalos)
        summonBlockedUntil = Time.time + Mathf.Max(ownBugs, rivalBugs) * summonInterval + 0.1f;

        EnterHunt();
        return true;
    }

    void EnterHunt()
    {
        hunting = true;
        exitHolding = false;
        attractTimer = 0f;
        SetCloudVisuals(true);
        SetBool(huntingParameter, true);
        HuntingChanged?.Invoke(true);
    }

    void ExitHunt()
    {
        hunting = false;
        exitHolding = false;

        // Los bichos vuelven a flotar en su sitio de siempre
        foreach (var kv in assigned)
            if (kv.Key != null) kv.Key.ResetHoverPoint();
        assigned.Clear();

        SetCloudVisuals(false);
        SetBool(huntingParameter, false);
        HuntingChanged?.Invoke(false);
    }

    Transform CloudAt(int index) => index == 0 ? cloudLeft : index == 1 ? cloudRight : cloudTop;

    void AttractBugs()
    {
        // Olvida los que ya no existen
        deadBuffer.Clear();
        foreach (var kv in assigned) if (kv.Key == null) deadBuffer.Add(kv.Key);
        foreach (var dead in deadBuffer) assigned.Remove(dead);

        fighter.CollectBugs(bugBuffer);
        foreach (var d in bugBuffer)
        {
            if (!(d is Bug bug) || assigned.ContainsKey(bug)) continue;

            int idx = LeastCrowdedCloud();
            Transform cloud = CloudAt(idx);
            if (cloud == null) continue;

            bug.SetHoverPoint(cloud.position);
            assigned[bug] = idx;
        }
    }

    int LeastCrowdedCloud()
    {
        var counts = new int[3];
        foreach (var kv in assigned) counts[kv.Value]++;

        int best = 0;
        for (int i = 1; i < 3; i++)
            if (CloudAt(i) != null && (CloudAt(best) == null || counts[i] < counts[best])) best = i;
        return best;
    }

    void EatCloud(int index)
    {
        Transform cloud = CloudAt(index);
        if (cloud == null || !fighter.InputEnabled) return;

        Trigger(index == 0 ? eatLeftTrigger : index == 1 ? eatRightTrigger : eatUpTrigger);

        fighter.CollectBugs(bugBuffer);
        float sqrRadius = cloudRadius * cloudRadius;
        int eaten = 0;

        foreach (var d in bugBuffer)
        {
            if (!(d is Bug bug) || bug == null) continue;
            if ((bug.transform.position - cloud.position).sqrMagnitude > sqrRadius) continue;

            assigned.Remove(bug);
            bug.Eat(fighter);
            eaten++;
        }

        if (eaten == 0) return;

        fighter.AddBar(eaten * barPerBug);
        BugsEaten?.Invoke(eaten);
    }

    //  ESCUPITAJO

    void Spit(int spitLevel)
    {
        // Mantiene el bloqueo un momento tras escupir
        phase = Phase.Casting;
        timer = spitLockTime;
        Trigger(spitTrigger);

        Fighter rival = fighter.Rival;
        if (rival == null) return;

        float stun = At(stunByLevel, spitLevel);
        Transform aim = rival.AimPoint;

        if (spitPrefab == null || aim == null)
        {
            HitWithSpit(rival, stun);
            return;
        }

        Transform origin = mouthPoint != null ? mouthPoint : fighter.ProjectileOrigin;
        Projectile spit = Instantiate(spitPrefab, origin.position, origin.rotation);
        spit.Launch(aim, spitSpeed, () => HitWithSpit(rival, stun));
    }

    void HitWithSpit(Fighter rival, float stun)
    {
        if (rival == null) return;
        rival.ReceiveHit(new HitData(fighter, spitDamage, stun, isSkill: true, parryable: true, kind: AttackKind.Ranged));
    }

    //  ULTIMATE

    void StartUltimate()
    {
        if (!fighter.BeginChannel()) return;
        fighter.TrySpendUses(fighter.MaxSkillUses);

        phase = Phase.UltimateWindup;
        timer = ultimateWindup;
        Trigger(ultimateTrigger);
    }

    void LaunchBall()
    {
        Fighter rival = fighter.Rival;
        if (rival == null) return;

        Transform aim = rival.AimPoint;
        if (giantBallPrefab == null || aim == null)
        {
            BallImpact(rival);
            return;
        }

        Transform origin = mouthPoint != null ? mouthPoint : fighter.ProjectileOrigin;
        Projectile ball = Instantiate(giantBallPrefab, origin.position, origin.rotation);
        ball.Launch(aim, ballSpeed, () => BallImpact(rival));
    }

    void BallImpact(Fighter rival)
    {
        if (rival == null) return;

        // Si lo bloquean o parrean no hay enjambre
        var hit = new HitData(fighter, 0f, 0f, isSkill: true, parryable: true,
                              kind: AttackKind.Ranged, interrupts: false);

        if (rival.ReceiveHit(hit) == BlockResult.None)
            rival.ApplySwarm(fighter, swarmBugs, swarmDpsPerBug, swarmDuration,
                             swarmShakeButton, bugsRemovedPerPress);
    }

    //  UTILIDADES

    public override void ResetState()
    {
        if (hunting) ExitHunt();

        phase = Phase.Idle;
        fighter.EndChannel();
        level = 0;
        summonBlockedUntil = 0f;
        SetCharging(false);
    }

    void SetCloudVisuals(bool on)
    {
        if (cloudVisuals == null) return;
        foreach (var go in cloudVisuals)
            if (go != null) go.SetActive(on);
    }

    void SetCharging(bool value) => SetBool(chargingParameter, value);

    void SetBool(string parameter, bool value)
    {
        if (animator != null && !string.IsNullOrEmpty(parameter)) animator.SetBool(parameter, value);
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
}