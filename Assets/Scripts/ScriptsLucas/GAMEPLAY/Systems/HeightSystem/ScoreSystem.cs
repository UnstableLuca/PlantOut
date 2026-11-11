using System;
using UnityEngine;


public class ScoreSystem : MonoBehaviour
{
    [SerializeField] Fighter player1;
    [SerializeField] Fighter player2;

    [Header("Puntos")]
    [SerializeField] float parryBonus = 5f;
    [Tooltip("Puntos que pierde una planta por cada punto de dano de un bicho (0 = los bichos no quitan puntos)")]
    [SerializeField, Min(0f)] float bugPenaltyScale = 1f;

    [Header("Comeback")]
    [Tooltip("Multiplicador de puntos del que va perdiendo cuando el deficit es maximo")]
    [SerializeField, Min(1f)] float maxMultiplier = 1.5f;
    [Tooltip("Puntos 'virtuales' que amortiguan el inicio: con pocos puntos un solo golpe no dispara el deficit")]
    [SerializeField, Min(0f)] float softStart = 20f;

    float score1, score2;

    public event Action ScoresChanged;

    void OnEnable()
    {
        if (player1 != null) player1.HitReceived += OnHit;
        if (player2 != null) player2.HitReceived += OnHit;
    }

    void OnDisable()
    {
        if (player1 != null) player1.HitReceived -= OnHit;
        if (player2 != null) player2.HitReceived -= OnHit;
    }

    //  PUNTOS

    void OnHit(HitReport r)
    {
        if (r.Source is Bug)
        {
            // Un bicho no puntua para nadie: le quita puntos a la planta que golpea
            if (r.FinalDamage > 0f && bugPenaltyScale > 0f)
                RemovePoints(r.Victim, r.FinalDamage * bugPenaltyScale);
        }
        else if (r.Source is Fighter attacker && r.FinalDamage > 0f)
        {
            AddPoints(attacker, r.FinalDamage);
        }

        if (r.Result == BlockResult.Parried && parryBonus > 0f) AddPoints(r.Victim, parryBonus);
    }

    public void RemovePoints(Fighter f, float points)
    {
        if (f == player1) score1 = Mathf.Max(0f, score1 - points);
        else if (f == player2) score2 = Mathf.Max(0f, score2 - points);
        else return;

        ScoresChanged?.Invoke();
    }

    public void AddPoints(Fighter f, float rawPoints)
    {
        float gained = rawPoints * GetMultiplier(f);   // multiplicador con el marcador de ANTES de sumar

        if (f == player1) score1 += gained;
        else if (f == player2) score2 += gained;
        else return;

        ScoresChanged?.Invoke();
    }


    public float GetPoints(Fighter f) => f == player1 ? score1 : score2;

    Fighter Other(Fighter f) => f == player1 ? player2 : player1;

    float Ratio(Fighter f)
    {
        if (f != player1 && f != player2) return 0.5f;

        float mine = GetPoints(f);
        float theirs = GetPoints(Other(f));
        float denom = mine + theirs + 2f * softStart;
        return denom <= 0f ? 0.5f : (mine + softStart) / denom;
    }

    public float GetDeficit(Fighter f) => Mathf.Clamp01((0.5f - Ratio(f)) * 2f);

    public float GetLead(Fighter f) => Mathf.Clamp01((Ratio(f) - 0.5f) * 2f);

    public float GetMultiplier(Fighter f) => Mathf.Lerp(1f, maxMultiplier, GetDeficit(f));

    public float Advantage => Ratio(player1) * 2f - 1f;
}