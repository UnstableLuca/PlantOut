using System;
using System.Collections;
using UnityEngine;
public class ArandinoSkills : SkillController
{
    [Header("Palillo (0 cargas / toque rapido)")]
    [Tooltip("Prefab del palillo (con el componente Projectile). Vacio = el efecto es instantaneo")]
    [SerializeField] Projectile toothpickPrefab;
    [Tooltip("Boca de Arandino. Vacio = el Projectile Origin del Fighter")]
    [SerializeField] Transform mouthPoint;
    [SerializeField] float toothpickSpeed = 25f;
    [SerializeField] float slowDuration = 2f;
    [Tooltip("Velocidad de las acciones del rival mientras dura: 0.5 = la mitad")]
    [SerializeField, Range(0.1f, 1f)] float slowFactor = 0.5f;
    [SerializeField] float toothpickCooldown = 4f;
    [Tooltip("Tiempo que Arandino no puede hacer otra cosa tras lanzarlo")]
    [SerializeField] float toothpickLockTime = 0.4f;

    [Header("Arandanos (mantener con 1-4 cargas)")]
    [SerializeField, Min(1)] int maxBerries = 4;
    [SerializeField, Min(0.1f)] float holdPerBerry = 0.5f;
    [Tooltip("Dano segun arandanos lanzados. Indice = numero de arandanos (el 0 no se usa)")]
    [SerializeField] float[] damageByBerries = { 0f, 20f, 40f, 80f, 120f };
    [Tooltip("Stun del rival en segundos. Indice = numero de arandanos (el 0 no se usa)")]
    [SerializeField] float[] stunByBerries = { 0f, 0f, 0f, 0.5f, 1f };
    [Tooltip("Segundos de comba hasta que los arandanos salen disparados")]
    [SerializeField] float swingLaunchDelay = 0.35f;
    [Tooltip("Duracion total del movimiento (Arandino no puede actuar)")]
    [SerializeField] float swingDuration = 0.7f;
    [Tooltip("Prefab del arandano volador (con el componente Projectile). Vacio = golpe instantaneo")]
    [SerializeField] Projectile berryPrefab;
    [Tooltip("Cabeza de Arandino. Vacio = el Projectile Origin del Fighter")]
    [SerializeField] Transform berryOrigin;
    [SerializeField] float berrySpeed = 20f;
    [Tooltip("Segundos entre un arandano y el siguiente al lanzarlos")]
    [SerializeField, Min(0f)] float berryStagger = 0.06f;

    [Header("Ultimate (5 cargas)")]
    [SerializeField] float ultimateDamage = 800f;
    [Tooltip("Segundos que tardan los arandanos en hacerse gigantes antes de explotar")]
    [SerializeField] float ultimateGrowTime = 2f;
    [SerializeField] float ultimateHitstun = 0.5f;
    [Tooltip("Stun propio al explotar, mientras vuelven a crecer")]
    [SerializeField] float selfStunDuration = 4f;
    [Tooltip("Si esta activo, un golpe limpio mientras crecen cancela la ultimate (y pierdes las 5 cargas)")]
    [SerializeField] bool ultimateInterruptible = true;

    [Header("Animator (vacio = no se usa)")]
    [Tooltip("Si se deja vacio busca el Animator en los hijos")]
    [SerializeField] Animator animator;
    [Tooltip("Int: arandanos crecidos (0-4)")]
    [SerializeField] string berriesParameter = "Berries";
    [Tooltip("Bool: true mientras mantienes la habilidad cargando arandanos")]
    [SerializeField] string chargingParameter = "Charging";
    [SerializeField] string toothpickTrigger = "Toothpick";
    [SerializeField] string swingTrigger = "BerrySwing";
    [SerializeField] string ultimateGrowTrigger = "UltimateGrow";
    [SerializeField] string ultimateExplodeTrigger = "UltimateExplode";

    enum Phase { Idle, Holding, Swinging, Casting, UltimateGrow }

    Phase phase;
    int berries;
    int swingBerries;
    float holdTimer;
    float timer;
    bool launched;
    double toothpickReadyAt;

    //  Estado publico (UI, animacion) 

    public int Berries => berries;
    public bool IsGrowingUltimate => phase == Phase.UltimateGrow;
    public float ToothpickCooldownLeft => Mathf.Max(0f, (float)(toothpickReadyAt - Time.timeAsDouble));

    public event Action<int> BerriesChanged;
    public event Action UltimateExploded;

    public override bool SuperArmor => phase == Phase.UltimateGrow && !ultimateInterruptible;

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

        // Sin cargas: directo al palillo
        if (charges == 0)
        {
            TryToothpick();
            return;
        }

        // 1-4 cargas: empieza a mantener. Si sueltas antes del primer arandano, es el palillo.
        if (!fighter.BeginChannel()) return;

        phase = Phase.Holding;
        holdTimer = 0f;
        SetBerries(0);
        SetCharging(true);
    }

    public override void OnSkillReleased()
    {
        if (phase != Phase.Holding) return;

        SetCharging(false);

        if (berries > 0)
        {
            StartSwing();
        }
        else
        {
            EndPhase();      // toque rapido: palillo
            TryToothpick();
        }
    }

    public override void OnChannelInterrupted()
    {
        // Te golpean: lo que estuviera creciendo se pierde (las cargas ya gastadas no se devuelven)
        switch (phase)
        {
            case Phase.Holding:
                SetCharging(false);
                SetBerries(0);
                break;

            case Phase.Swinging:
                if (!launched) SetBerries(0);   // si ya salieron disparados, siguen su camino
                break;
        }

        phase = Phase.Idle;
    }

    //  UPDATE

    void Update()
    {
        float dt = Time.deltaTime * fighter.ActionSpeed;   // ralentizado = todo tarda mas

        switch (phase)
        {
            case Phase.Holding:
                holdTimer += dt;
                while (phase == Phase.Holding && holdTimer >= holdPerBerry)
                {
                    holdTimer -= holdPerBerry;

                    // Tope de arandanos o sin barra: espera a que sueltes
                    if (berries >= maxBerries || !fighter.TrySpendUses(1))
                    {
                        holdTimer = 0f;
                        break;
                    }
                    SetBerries(berries + 1);
                }
                break;

            case Phase.Swinging:
                timer += dt;
                if (!launched && timer >= swingLaunchDelay)
                {
                    launched = true;
                    LaunchBerries();
                }
                if (timer >= swingDuration) EndPhase();
                break;

            case Phase.Casting:
                timer -= dt;
                if (timer <= 0f) EndPhase();
                break;

            case Phase.UltimateGrow:
                timer -= dt;
                if (timer <= 0f) ExplodeUltimate();
                break;
        }
    }

    void EndPhase()
    {
        phase = Phase.Idle;
        fighter.EndChannel();
    }

    //  PALILLO

    bool TryToothpick()
    {
        if (Time.timeAsDouble < toothpickReadyAt) return false;
        if (!fighter.BeginChannel()) return false;

        phase = Phase.Casting;
        timer = toothpickLockTime;
        toothpickReadyAt = Time.timeAsDouble + toothpickCooldown;
        Trigger(toothpickTrigger);

        Fighter rival = fighter.Rival;
        if (rival == null) return true;   // lo lanzas al aire

        Transform aim = rival.AimPoint;
        if (toothpickPrefab == null || aim == null)
        {
            HitWithToothpick(rival);
            return true;
        }

        Transform origin = mouthPoint != null ? mouthPoint : fighter.ProjectileOrigin;
        Projectile pick = Instantiate(toothpickPrefab, origin.position, origin.rotation);
        pick.Launch(aim, toothpickSpeed, () => HitWithToothpick(rival));
        return true;
    }

    void HitWithToothpick(Fighter rival)
    {
        if (rival == null) return;

        // Sin dano y sin cortar la accion del rival: solo aplica la ralentizacion si entra limpio
        var hit = new HitData(fighter, 0f, 0f, isSkill: true, parryable: true,
                              kind: AttackKind.Ranged, interrupts: false);

        if (rival.ReceiveHit(hit) == BlockResult.None)
            rival.ApplySlow(slowFactor, slowDuration);
    }

    //  ARANDANOS

    void StartSwing()
    {
        phase = Phase.Swinging;
        swingBerries = berries;
        timer = 0f;
        launched = false;
        Trigger(swingTrigger);
    }

    void LaunchBerries()
    {
        int n = swingBerries;
        float damage = At(damageByBerries, n);
        float stun = At(stunByBerries, n);

        SetBerries(0);   // los arandanos salen disparados

        Fighter rival = fighter.Rival;
        if (rival == null || n <= 0) return;

        StartCoroutine(FlyBerries(n, rival, damage, stun));
    }

    IEnumerator FlyBerries(int n, Fighter rival, float damage, float stun)
    {
        Transform aim = rival.AimPoint;
        if (berryPrefab == null || aim == null)
        {
            HitWithBerries(rival, damage, stun);
            yield break;
        }

        Transform origin = berryOrigin != null ? berryOrigin : fighter.ProjectileOrigin;

        for (int i = 0; i < n; i++)
        {
            bool last = i == n - 1;
            Projectile berry = Instantiate(berryPrefab, origin.position, origin.rotation);

            // El golpe (dano + stun) se aplica cuando llega el ultimo arandano
            berry.Launch(aim, berrySpeed, last ? (Action)(() => HitWithBerries(rival, damage, stun)) : null);

            if (!last && berryStagger > 0f) yield return new WaitForSeconds(berryStagger);
        }
    }

    void HitWithBerries(Fighter rival, float damage, float stun)
    {
        if (rival == null) return;
        rival.ReceiveHit(new HitData(fighter, damage, stun, isSkill: true, parryable: true, kind: AttackKind.Ranged));
    }

    //  ULTIMATE

    void StartUltimate()
    {
        if (!fighter.BeginChannel()) return;
        fighter.TrySpendUses(fighter.MaxSkillUses);

        phase = Phase.UltimateGrow;
        timer = ultimateGrowTime;
        Trigger(ultimateGrowTrigger);
    }

    void ExplodeUltimate()
    {
        phase = Phase.Idle;
        fighter.EndChannel();
        Trigger(ultimateExplodeTrigger);

        Fighter rival = fighter.Rival;
        if (rival != null)
            rival.ReceiveHit(new HitData(fighter, ultimateDamage, ultimateHitstun, isSkill: true, parryable: true));

        UltimateExploded?.Invoke();

        // El coste: Arandino queda aturdido mientras vuelven a crecer los arandanos
        fighter.Stun(selfStunDuration);
    }

    //  UTILIDADES

    public override void ResetState()
    {
        StopAllCoroutines();
        phase = Phase.Idle;
        fighter.EndChannel();
        toothpickReadyAt = 0;
        SetBerries(0);
        SetCharging(false);
    }

    void SetBerries(int value)
    {
        berries = Mathf.Clamp(value, 0, maxBerries);
        if (animator != null && !string.IsNullOrEmpty(berriesParameter))
            animator.SetInteger(berriesParameter, berries);
        BerriesChanged?.Invoke(berries);
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
}