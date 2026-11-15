using System;
using UnityEngine;

public class SunSystem : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] ScoreSystem scoreSystem;
    [SerializeField] Fighter playerLeft;
    [SerializeField] Fighter playerRight;

    [Header("Ciclo Solar")]
    [Tooltip("Nivel base de sol cuando el combate está empatado (0..1)")]
    [SerializeField, Range(0f, 1f)] float baseSunLevel = 0.5f;
    [Tooltip("Amplitud de oscilación sinusoidal del ciclo día/nube")]
    [SerializeField, Range(0f, 0.5f)] float cycleAmplitude = 0.25f;
    [Tooltip("Duración en segundos de un ciclo solar completo")]
    [SerializeField, Min(1f)] float cycleDuration = 30f;

    [Header("Comeback Solar")]
    [Tooltip("Bonificación extra de sol para el jugador que va perdiendo")]
    [SerializeField, Range(0f, 0.5f)] float trailingBonus = 0.25f;
    [Tooltip("Penalización de sol para el jugador que va ganando")]
    [SerializeField, Range(0f, 0.5f)] float leadingPenalty = 0.15f;

    [Header("Visual (opcional)")]
    [SerializeField] Light directionalSun;
    [SerializeField] float minLightIntensity = 0.6f;
    [SerializeField] float maxLightIntensity = 1.6f;

    private float cycleTimer;

    public float CurrentCycleFactor =>
        0.5f + 0.5f * Mathf.Sin((cycleTimer / Mathf.Max(0.1f, cycleDuration)) * Mathf.PI * 2f);

    public event Action<float, float> SunLevelsChanged;

    void Update()
    {
        cycleTimer += Time.deltaTime;

        float leftLevel = GetLevel(playerLeft);
        float rightLevel = GetLevel(playerRight);

        if (directionalSun != null)
        {
            float avg = (leftLevel + rightLevel) * 0.5f;
            directionalSun.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, avg);
        }

        SunLevelsChanged?.Invoke(leftLevel, rightLevel);
    }

    public float GetLevel(Fighter plant)
    {
        float cycleOffset = (CurrentCycleFactor - 0.5f) * 2f * cycleAmplitude;
        float level = baseSunLevel + cycleOffset;

        if (scoreSystem != null && plant != null)
        {
            float ownLead = scoreSystem.GetLead(plant);
            float rivalLead = plant.Rival != null ? scoreSystem.GetLead(plant.Rival) : 0f;

            level -= ownLead * leadingPenalty;
            level += rivalLead * trailingBonus;
        }

        return Mathf.Clamp01(level);
    }
}