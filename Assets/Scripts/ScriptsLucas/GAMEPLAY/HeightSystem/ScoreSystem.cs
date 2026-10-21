using System;
using Unity.VisualScripting;
using UnityEngine;

public class ScoreSystem : MonoBehaviour
{
        [SerializeField] Fighter player1;
        [SerializeField] Fighter player2;

    [Header("Puntos")]
    [SerializeField] float parryBonus = 5f;
    [Tooltip("Si un bicho golpea se lo lleva el rival los puntos")]
    [SerializeField] bool bugDamageScoresForRival = true;

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

    //PUNTOS
    void OnHit(HitReport r)
    {
        Fighter scorer = null;
        if (r.Source is Fighter attacker) scorer = attacker;
        else if (bugDamageScoresForRival) scorer = r.Victim.Rival;

        if (scorer != null && r.FinalDamage > 0f) AddPoints(scorer, r.FinalDamage);

        if (r.Result == BlockResult.Parried && parryBonus > 0f) AddPoints(r.Victim, parryBonus);
    }

    /// <summary>Suma puntos aplicando el multiplicador de comeback del jugador.</summary>
    public void AddPoints(Fighter f, float rawPoints)
    {
        float gained = rawPoints * GetMultiplier(f);   // multiplicador con el marcador de ANTES de sumar

        if (f == player1) score1 += gained;
        else if (f == player2) score2 += gained;
        else return;

        ScoresChanged?.Invoke();
    }

    //CONSULTAR
    public float GetPoints(Fighter f) => f == player1 ? score1 : score2;

    Fighter Other(Fighter f) => f == player1 ? player2 : player1;

    /// <summary>Proporcion de puntos de f (0..1, 0.5 = empate), suavizada con softStart.</summary>
    float Ratio(Fighter f)
    {
        if (f != player1 && f != player2) return 0.5f;

        float mine = GetPoints(f);
        float theirs = GetPoints(Other(f));
        float denom = mine + theirs + 2f * softStart;
        return denom <= 0f ? 0.5f : (mine + softStart) / denom;
    }
    public float GetDeficit(Fighter f) => Mathf.Clamp01((0.5f - Ratio(f)) * 2f);

    /// <summary> 0 = vas perdiendo o empatado, 1 = ventaja maxima.</summary>
    public float GetLead(Fighter f) => Mathf.Clamp01((Ratio(f) - 0.5f) * 2f);

    /// <summary>1 = sin bonus, hasta maxMultiplier con deficit maximo (continuo, sin escalones).</summary>
    public float GetMultiplier(Fighter f) => Mathf.Lerp(1f, maxMultiplier, GetDeficit(f));

    /// <summary>-1 (gana el jugador 2 de lejos) .. 0 (empate) .. +1 (gana el jugador 1). Para el mapa de progresion.</summary>
    public float Advantage => Ratio(player1) * 2f - 1f;
}
