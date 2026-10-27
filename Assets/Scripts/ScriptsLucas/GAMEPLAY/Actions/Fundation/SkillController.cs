using UnityEngine;

[RequireComponent(typeof(Fighter))]
public abstract class SkillController : MonoBehaviour
{
    protected Fighter fighter;

    protected virtual void Awake() => fighter = GetComponent<Fighter>();

    public virtual bool SuperArmor => false;

    public abstract void OnSkillPressed();
    public abstract void OnSkillReleased();

    public virtual void OnChannelInterrupted() { }

    public virtual float ModifyAttackDamage(AttackKind kind, float baseDamage) => baseDamage;

    public virtual void ResetState() { }
}
