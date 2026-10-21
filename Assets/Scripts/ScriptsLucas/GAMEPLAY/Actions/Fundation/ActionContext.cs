public enum ActionPhase { Startup, Active, Recovery }

public enum AttackSide { Left, Right }

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

    public HitData(IDamageable source, float damage, float hitstun, bool isSkill)
    {
        Source = source;
        Damage = damage;
        Hitstun = hitstun;
        IsSkill = isSkill;
    }
}
public interface IFighter : IDamageable
{
    bool CanAct { get; }

    float SkillBar { get; }
    bool TrySpendUses(int uses);
    void AddBar(float amount);

    IDamageable GetTarget(AttackSide side);

    void PlayAnimation(string trigger);

    void BeginBlock(double pressTime);
    void EndBlock();
}

public readonly struct ActionContext
{
    public readonly IFighter Owner;
    public readonly double Now; //Time.timeAsDouble
    public readonly int ComboLenght;
    public readonly AttackSide Side;

    public ActionContext(IFighter owner, double now,
                        int comboLength = 1, AttackSide side = AttackSide.Right)
    {
        Owner = owner;
        Now = now;
        ComboLenght = comboLength;
        Side = side;
    }

    public IDamageable ResolveTarget() => Owner.GetTarget(Side);

}

