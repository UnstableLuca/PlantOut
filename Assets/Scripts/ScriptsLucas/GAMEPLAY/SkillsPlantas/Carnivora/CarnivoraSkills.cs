using System;
using UnityEngine;

public class CarnivorousSkills : SkillController
{
    [Header("Boca / Trampa (1-4 cargas)")]
    [SerializeField, Min(1)] int maxBiteCharges = 4;
    [SerializeField, Min(0.1f)] float chewPerCharge = 0.5f;
    [SerializeField] float[] trapDurationByCount = { 0f, 4f, 5f, 7f, 10f };

    [Header("Daño y Efectos")]
    [SerializeField] float poisonDamagePerCharge = 6f;
    [SerializeField] float trapCooldown = 3.5f;

    [Header("Ultimate (5 cargas) - Devorar")]
    [SerializeField] float ultimateHeal = 50f;
    [SerializeField] float ultimateDamage = 150f;
    [SerializeField] float ultimateDuration = 3f;

    [Header("Animaciones y Visuales")]
    [SerializeField] string chewTrigger = "Chew";
    [SerializeField] string trapTrigger = "Trap";
    [SerializeField] string ultimateTrigger = "CarnivorousUlt";
    [SerializeField] GameObject[] trapVisuals;

    enum Phase { Idle, Chewing, Ultimate }
    Phase phase;

    int chargesHeld;
    float chewTimer;
    int addedThisCast;

    float trapTimer;
    double trapReadyAt;
    double skillsReadyAt;

    float ultimateTimer;

    public override bool SuperArmor => phase == Phase.Ultimate;

    public override void OnSkillPressed()
    {
        if (phase != Phase.Idle || !fighter.CanAct) return;
        if (Time.timeAsDouble < skillsReadyAt) return;

        int charges = Mathf.FloorToInt(fighter.SkillBar + 0.0001f);

        if (charges >= fighter.MaxSkillUses)
        {
            StartUltimate();
            return;
        }

        if (charges == 0 || chargesHeld >= maxBiteCharges)
        {
            TryCastTrap();
            return;
        }

        if (!fighter.BeginChannel()) return;

        phase = Phase.Chewing;
        chewTimer = 0f;
        addedThisCast = 0;
    }

    public override void OnSkillReleased()
    {
        if (phase == Phase.Chewing) FinishChew(allowTap: true);
    }

    public override void OnChannelInterrupted()
    {
        if (phase == Phase.Chewing) FinishChew(allowTap: false);
        else if (phase == Phase.Ultimate) EndUltimate();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (trapTimer > 0f)
        {
            trapTimer -= dt;
            if (trapTimer <= 0f) EndTrap();
        }

        switch (phase)
        {
            case Phase.Chewing:
                chewTimer += dt;
                while (phase == Phase.Chewing && chewTimer >= chewPerCharge)
                {
                    chewTimer -= chewPerCharge;
                    GrowCharge();
                }
                break;

            case Phase.Ultimate:
                ultimateTimer -= dt;
                if (ultimateTimer <= 0f) EndUltimate();
                break;
        }
    }

    void GrowCharge()
    {
        if (!fighter.TrySpendUses(1))
        {
            FinishChew(allowTap: false);
            return;
        }

        chargesHeld = Mathf.Clamp(chargesHeld + 1, 0, maxBiteCharges);
        addedThisCast++;
        fighter.PlayAnimation(chewTrigger);

        if (chargesHeld >= maxBiteCharges || fighter.SkillBar < 1f) FinishChew(allowTap: false);
    }

    void FinishChew(bool allowTap)
    {
        phase = Phase.Idle;
        fighter.EndChannel();

        int added = addedThisCast;
        addedThisCast = 0;

        if (added == 0 && allowTap) TryCastTrap();
    }

    bool TryCastTrap()
    {
        if (Time.timeAsDouble < trapReadyAt) return false;

        trapTimer = At(trapDurationByCount, chargesHeld);
        trapReadyAt = Time.timeAsDouble + trapCooldown;

        SetTrapVisuals(true);
        fighter.PlayAnimation(trapTrigger);
        return true;
    }

    void EndTrap()
    {
        trapTimer = 0f;
        SetTrapVisuals(false);
        chargesHeld = 0;
    }

    void StartUltimate()
    {
        if (!fighter.BeginChannel()) return;
        fighter.TrySpendUses(fighter.MaxSkillUses);

        phase = Phase.Ultimate;
        ultimateTimer = ultimateDuration;

        fighter.PlayAnimation(ultimateTrigger);

        // Aplicar daño directo al rival y curación al propio luchador
        if (fighter.Rival != null)
        {
            var hit = new HitData(fighter, ultimateDamage, 0.5f, isSkill: true, parryable: false);
            fighter.Rival.ReceiveHit(hit);
        }
    }

    void EndUltimate()
    {
        phase = Phase.Idle;
        fighter.EndChannel();
        skillsReadyAt = Time.timeAsDouble + 3f;
    }

    public override float ModifyAttackDamage(AttackKind kind, float baseDamage)
    {
        float bonus = baseDamage + (poisonDamagePerCharge * chargesHeld);
        return bonus;
    }

    public override void ResetState()
    {
        phase = Phase.Idle;
        fighter.EndChannel();
        chargesHeld = 0;
        trapTimer = 0f;
        SetTrapVisuals(false);
    }

    static float At(float[] values, int index, float fallback = 0f)
    {
        if (values == null || values.Length == 0) return fallback;
        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }

    void SetTrapVisuals(bool visible)
    {
        if (trapVisuals == null) return;
        foreach (var go in trapVisuals)
            if (go != null) go.SetActive(visible);
    }
}