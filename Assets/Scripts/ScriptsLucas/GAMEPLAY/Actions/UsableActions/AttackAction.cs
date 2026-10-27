using UnityEngine;

[CreateAssetMenu(fileName = "NewAttack", menuName = "Fighter/Actions/Attack")]
public class AttackAction : PlayerAction
{
    [Header("Golpe")]
    public AttackKind kind = AttackKind.Melee;
    public float damage = 10f;
    [Tooltip("0 es sin stun")]
    public float hitstun = 0;

    [Header("Barra de habilidad")]
    [Tooltip("Lo que sube la barra al golpear 'limpio' al rival (1.0 es una)")]
    public float barGainOnRivalHit = 0.15f;
    [Tooltip("Lo que sube la barra por golpear bicho")]
    public float barGainOnBugHit = 0.05f;


    public override void OnPhaseEnter(in ActionContext ctx, ActionPhase phase)
    {
        if (phase != ActionPhase.Active) return;

        IDamageable target = ctx.ResolveTarget();
        if (target == null) return;

        float finalDamage = ctx.Owner.ModifyAttackDamage(kind, damage);
        var hit = new HitData(ctx.Owner, damage, hitstun, isSkill: false);
        BlockResult result = target.ReceiveHit(hit);

        if (result != BlockResult.None) return;

        ctx.Owner.AddBar(target is IFighter ? barGainOnRivalHit : barGainOnBugHit);
    }
}
