using UnityEngine;

[CreateAssetMenu(fileName = "NewBlock", menuName = "Fighter/Actions/Block")]
public class BlockAction : PlayerAction
{
    [Header("Parry")]
    [Tooltip("Ventana desde que pulsas bloquear( en secs)")]
    public float parryWindow = 0.14f;

    [Header("Bloqueo normal")]
    [Range(0f, 1f)] public float blockDamageMultiplier = 0.2f;

    [Header("Recompensa de barra")]
    public float parryBarReward = 1f;
    public float blockBarReward = 0.1f;

    private void Reset()
    {
        startup = 0f;
        active = 0f;
        recovery = 0.15f;
    }

    public override void Execute(in ActionContext ctx)
    {
        base.Execute(ctx);
        ctx.Owner.BeginBlock(ctx.Now);
    }

    public override void OnInterrupted(in ActionContext ctx)
    {
        ctx.Owner.EndBlock();
    }

    public BlockResult Evaluate(double blockPressTime, double hitTime)
    {
        double elapsed = hitTime - blockPressTime;
        return elapsed <= parryWindow ? BlockResult.Parried : BlockResult.Blocked;
    }

    public float ModifyDamage(BlockResult result, float incomingDamage)
    {
        switch (result)
        {
            case BlockResult.Parried: return 0f;
            case BlockResult.Blocked: return incomingDamage * blockDamageMultiplier;
            default: return incomingDamage;
        }
    }

    public float BarReward(BlockResult result)
    {
        switch (result)
        {
            case BlockResult.Parried: return parryBarReward;
            case BlockResult.Blocked: return blockBarReward;
            default: return 0f;
        }
    }
}
