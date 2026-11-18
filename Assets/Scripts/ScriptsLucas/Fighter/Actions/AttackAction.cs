using UnityEngine;

[CreateAssetMenu(menuName = "Fighter/Actions/Attack", fileName = "NewAttack")]
public class AttackAction : PlayerAction
{
    [Header("Golpe")]
    public AttackKind kind = AttackKind.Melee;
    public float damage = 10f;
    [Tooltip("0 = sin stun")]
    public float hitstun = 0.25f;

    [Header("Objetivo")]
    [Tooltip("Si esta activo, apunta a un bicho cuando haya uno disparable. Melee: desactivado. Ranged: activado.")]
    public bool canTargetBugs = true;

    [Header("Proyectil (opcional, para el rango)")]
    [Tooltip("Prefab del pincho (con el componente Projectile). Vacio = golpe instantaneo")]
    public Projectile projectilePrefab;
    public float projectileSpeed = 25f;

    [Header("Barra de habilidad")]
    [Tooltip("Barra que gana al conectar limpio con el rival (1.0 = una carga)")]
    public float barGainOnRivalHit = 0.15f;
    [Tooltip("Barra que gana al matar un bicho (valor bajo para que no se farmee)")]
    public float barGainOnBugHit = 0.05f;

    public override void OnPhaseEnter(in ActionContext ctx, ActionPhase phase)
    {
        if (phase != ActionPhase.Active) return;

        IDamageable target = ctx.ResolveTarget(canTargetBugs);
        if (target == null) return;   // disparo al aire: solo pagas el recovery

        // OJO: un parametro 'in' no se puede capturar en la lambda, por eso se copian a locales
        IFighter owner = ctx.Owner;
        float finalDamage = owner.ModifyAttackDamage(kind, damage);

        Transform aim = target.AimPoint;
        if (projectilePrefab == null || aim == null)
        {
            Resolve(owner, target, finalDamage);
            return;
        }

        Transform origin = owner.ProjectileOrigin;
        Projectile spike = Instantiate(projectilePrefab, origin.position, origin.rotation);
        spike.Launch(aim, projectileSpeed, () => Resolve(owner, target, finalDamage));
    }

    void Resolve(IFighter owner, IDamageable target, float finalDamage)
    {
        if (target is UnityEngine.Object t && !t) return;   // el objetivo desaparecio mientras volaba

        var hit = new HitData(owner, finalDamage, hitstun, isSkill: false, parryable: true, kind: kind);
        BlockResult result = target.ReceiveHit(hit);

        // Solo cargas barra si el golpe entra limpio (no bloqueado, parreado ni ignorado)
        if (result != BlockResult.None) return;
        if (owner is UnityEngine.Object o && !o) return;

        owner.AddBar(target is IFighter ? barGainOnRivalHit : barGainOnBugHit);
    }
}