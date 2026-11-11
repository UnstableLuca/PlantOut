using UnityEngine;

public enum ActionPhase { Startup, Active, Recovery }

public enum AttackSide { Left, Right }

public enum AttackKind { Melee, Ranged }

public enum BlockResult { None, Blocked, Parried, Ignored }


public interface IDamageable
{
    BlockResult ReceiveHit(in HitData hit);

    Transform AimPoint { get; }
}

public interface IShootable
{
    bool CanBeTargeted { get; }
}

public readonly struct HitData
{
    public readonly IDamageable Source;   // un Fighter o un bicho
    public readonly float Damage;
    public readonly float Hitstun;        // 0 si no quieres stun
    public readonly bool IsSkill;
    
    public readonly bool Parryable;       // false: se puede bloquear pero nunca parrear (p.ej. ticks de la ultimate)
    public readonly AttackKind Kind;      // los bichos solo mueren por disparos (Ranged)
    public readonly bool Interrupts;

    public HitData(IDamageable source, float damage, float hitstun, bool isSkill,
                   bool parryable = true, AttackKind kind = AttackKind.Melee, bool interrupts = true)
    {
        Source = source;
        Damage = damage;
        Hitstun = hitstun;
        IsSkill = isSkill;
        Parryable = parryable;
        Kind = kind;
        Interrupts = interrupts;
    }
}


public interface IFighter : IDamageable
{
    bool CanAct { get; }             // false en stun, canalizando, accion en curso, etc.

    float SkillBar { get; }          // 1.0 = una carga
    bool TrySpendUses(int uses);
    void AddBar(float amount);

    Transform ProjectileOrigin { get; }

    IDamageable GetAutoTarget(bool includeBugs);

    float ModifyAttackDamage(AttackKind kind, float baseDamage);

    void PlayAnimation(string trigger);

    // El Fighter guarda pressTime para comparar luego con BlockAction.Evaluate
    void BeginBlock(double pressTime);
    void EndBlock();
}

public readonly struct ActionContext
{
    public readonly IFighter Owner;
    public readonly double Now;          // Time.timeAsDouble en el momento del input

    public ActionContext(IFighter owner, double now)
    {
        Owner = owner;
        Now = now;
    }
    public IDamageable ResolveTarget(bool includeBugs) => Owner.GetAutoTarget(includeBugs);
}