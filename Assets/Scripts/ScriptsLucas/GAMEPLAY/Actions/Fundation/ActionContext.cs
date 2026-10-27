public enum ActionPhase { Startup, Active, Recovery }

public enum AttackSide { Left, Right }

public enum AttackKind { Melee, Ranged }

public enum BlockResult { None, Blocked, Parried }


public interface IDamageable
{
    BlockResult ReceiveHit(in HitData hit);
}

public readonly struct HitData
{
    public readonly IDamageable Source; //las plantas o los bichos
    public readonly float Damage;
    public readonly float Hitstun; // 0 para no stun
    public readonly bool IsSkill;
    public readonly bool Parryable; // false = se puede bloquear pero no parrear
    public HitData(IDamageable source, float damage, float hitstun, bool isSkill, bool parryable = true)
    {
        Source = source;
        Damage = damage;
        Hitstun = hitstun;
        IsSkill = isSkill;
        Parryable = parryable;
    }
}
public interface IFighter : IDamageable
{
    bool CanAct { get; }

    float SkillBar { get; }
    bool TrySpendUses(int uses);
    void AddBar(float amount);

    IDamageable GetAutoTarget();

    float ModifyAttackDamage(AttackKind kind, float baseDamage);

    void PlayAnimation(string trigger);

    void BeginBlock(double pressTime);
    void EndBlock();
}

public readonly struct ActionContext
{
    public readonly IFighter Owner;
    public readonly double Now; //Time.timeAsDouble

    public ActionContext(IFighter owner, double now)
    {
        Owner = owner;
        Now = now;
    }

    public IDamageable ResolveTarget() => Owner.GetAutoTarget();

}

