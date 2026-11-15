using System;
using UnityEngine;

public class ScoreSystem : MonoBehaviour
{
    [Header("Jugadores")]
    [SerializeField] Fighter playerLeft;
    [SerializeField] Fighter playerRight;

    [Header("Puntuación")]
    [Tooltip("Diferencia máxima de puntos para normalizar altura y ventajas de comeback")]
    [SerializeField, Min(1f)] float maxScoreSpread = 500f;
    [Tooltip("Multiplicador de penalización cuando te golpea un bicho")]
    [SerializeField, Min(0f)] float bugPenaltyScale = 1f;

    [Header("Altura visual (opcional)")]
    [SerializeField] PlantHeight heightLeft;
    [SerializeField] PlantHeight heightRight;

    private float scoreLeft;
    private float scoreRight;

    public float ScoreLeft => scoreLeft;
    public float ScoreRight => scoreRight;

    /// <summary>Positivo = gana izquierda, Negativo = gana derecha.</summary>
    public float ScoreDelta => scoreLeft - scoreRight;

    public event Action<float, float> ScoreChanged;

    void OnEnable()
    {
        if (playerLeft != null) playerLeft.HitReceived += OnHitReceived;
        if (playerRight != null) playerRight.HitReceived += OnHitReceived;
    }

    void OnDisable()
    {
        if (playerLeft != null) playerLeft.HitReceived -= OnHitReceived;
        if (playerRight != null) playerRight.HitReceived -= OnHitReceived;
    }

    void Start()
    {
        UpdateVisualHeights();
        ScoreChanged?.Invoke(scoreLeft, scoreRight);
    }

    private void OnHitReceived(HitReport report)
    {
        if (report.FinalDamage <= 0f) return;

        // Si el atacante es un Fighter, suma puntos al atacante
        if (report.Source is Fighter attacker)
        {
            AddPoints(attacker, report.FinalDamage);
        }
        // Si el atacante es un bicho, resta puntos a la víctima
        else if (report.Source is Bug && report.Victim != null)
        {
            AddPoints(report.Victim, -report.FinalDamage * bugPenaltyScale);
        }
    }

    public void AddPoints(Fighter target, float points)
    {
        if (target == null || Mathf.Approximately(points, 0f)) return;

        if (target == playerLeft) scoreLeft += points;
        else if (target == playerRight) scoreRight += points;

        UpdateVisualHeights();
        ScoreChanged?.Invoke(scoreLeft, scoreRight);
    }

    /// <summary>
    /// Devuelve 0..1 indicando cuánto va ganando 'plant' respecto a su rival (0 = empatado o perdiendo).
    /// </summary>
    public float GetLead(Fighter plant)
    {
        if (plant == null) return 0f;
        float diff = plant == playerLeft ? (scoreLeft - scoreRight) : (scoreRight - scoreLeft);
        if (diff <= 0f) return 0f;
        return Mathf.Clamp01(diff / maxScoreSpread);
    }

    private void UpdateVisualHeights()
    {
        float normalized = Mathf.Clamp((scoreLeft - scoreRight) / maxScoreSpread, -1f, 1f);
        if (heightLeft != null) heightLeft.SetNormalizedHeight(normalized);
        if (heightRight != null) heightRight.SetNormalizedHeight(-normalized);
    }

    public void ResetScores()
    {
        scoreLeft = 0f;
        scoreRight = 0f;
        UpdateVisualHeights();
        ScoreChanged?.Invoke(scoreLeft, scoreRight);
    }
}