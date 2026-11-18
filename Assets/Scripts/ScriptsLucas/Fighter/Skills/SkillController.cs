using UnityEngine;

public class SkillController : MonoBehaviour
{
    protected Fighter fighter;

    protected virtual void Awake()
    {
        fighter = GetComponentInParent<Fighter>();
        if (fighter == null)
            fighter = GetComponent<Fighter>();
    }

    public virtual bool SuperArmor => false;

    public virtual bool AllowsBarGain => true;

    public virtual void OnSkillPressed() { }
    public virtual void OnSkillReleased() { }

    public virtual bool InterceptAttack(AttackKind kind) => false;
    public virtual bool InterceptBlockPress() => false;

    public virtual void OnChannelInterrupted() { }

    public virtual float ModifyAttackDamage(AttackKind kind, float baseDamage) => baseDamage;

    public virtual void ResetState() { }
}