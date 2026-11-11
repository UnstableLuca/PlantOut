using UnityEngine;

[RequireComponent(typeof(Fighter))]
public abstract class SkillController : MonoBehaviour
{
    protected Fighter fighter;

    protected virtual void Awake() => fighter = GetComponent<Fighter>();

    public virtual bool SuperArmor => false;

    public virtual bool AllowsBarGain => true;

    public abstract void OnSkillPressed();
    public abstract void OnSkillReleased();

    public virtual bool InterceptAttack(AttackKind kind) => false;
    public virtual bool InterceptBlockPress() => false;

    public virtual void OnChannelInterrupted() { }

    public virtual float ModifyAttackDamage(AttackKind kind, float baseDamage) => baseDamage;

    public virtual void ResetState() { }
}
