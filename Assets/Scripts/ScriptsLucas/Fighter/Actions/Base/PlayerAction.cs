using UnityEngine;

public abstract class PlayerAction : ScriptableObject
{
    [Header("Timing (esta en secs)")]
    [Min(0f)] public float startup = 0.08f;
    [Min(0f)] public float active = 0.10f;
    [Min(0f)] public float recovery = 0.20f;

    [Header("Animation")]
    public string animationTrigger;

    public float TotalDuration => startup + active + recovery;

    public virtual float GetTotalDuration(in ActionContext ctx) => TotalDuration;

    public virtual bool CanExecute(in ActionContext ctx)
    {
        return ctx.Owner != null && ctx.Owner.CanAct;
    }

    public virtual void Execute(in ActionContext ctx)
    {
        if (!string.IsNullOrEmpty(animationTrigger))
            ctx.Owner.PlayAnimation(animationTrigger);
    }

    public virtual void OnPhaseEnter(in ActionContext ctx, ActionPhase phase) { }

    public virtual void OnInterrupted(in ActionContext ctx) { }
}
