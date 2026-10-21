using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Fighter/Actions/Skill")]
public class SkillAction : PlayerAction
{
    [Header("Coste")]
    [Range(1, 5)] public int maxUses = 5;

    [Header("Golpe base")]
    public float baseDamage = 8f;
    [Tooltip("X = usos gastados (1..5), Y = multiplicador de daño")]
    public AnimationCurve damageByUses = new AnimationCurve(
        new Keyframe(1, 1f), new Keyframe(2, 2.5f), new Keyframe(3, 4.5f),
        new Keyframe(4, 7f), new Keyframe(5, 10f));
    public float hitstun = 0.4f;

    [Header("Riesgo")]
    public float extraRecoveryPerUse = 0.06f;

    int UsesFor(in ActionContext ctx) => Mathf.Clamp(ctx.ComboLenght, 1, maxUses);

    public override float GetTotalDuration(in ActionContext ctx)
        => TotalDuration + extraRecoveryPerUse * (UsesFor(ctx) - 1);

    public override bool CanExecute(in ActionContext ctx)
    {
        return base.CanExecute(ctx) && ctx.Owner.SkillBar >= UsesFor(ctx);
    }

    public override void Execute(in ActionContext ctx)
    {
        ctx.Owner.TrySpendUses(UsesFor(ctx));
        base.Execute(ctx);
    }

    public override void OnPhaseEnter(in ActionContext ctx, ActionPhase phase)
    {
        if (phase != ActionPhase.Active) return;

        IDamageable target = ctx.ResolveTarget();
        if (target == null) return;   // fallado?

        float damage = baseDamage * damageByUses.Evaluate(UsesFor(ctx));
        target.ReceiveHit(new HitData(ctx.Owner, damage, hitstun, isSkill: true));
    }
}
