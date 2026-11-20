using UnityEngine;
using TMPro;
using System;
using Unity.VisualScripting;

public class MatchTimer : MonoBehaviour
{
    public enum TieRule { Draw, SuddenDeath }

    [Header("Sistemas")]
    [SerializeField] CinematicDirector director;
    [SerializeField] ScoreSystem scoreSystem;
    [SerializeField] BugSpawner bugSpawner;
    [SerializeField] Fighter playerLeft;
    [SerializeField] Fighter playerRight;

    [Header("Partida")]
    [Tooltip("Duracion de la partida en segundos (300 = 5 minutos)")]
    [SerializeField, Min(1f)] float matchDuration = 300f;
    [Tooltip("Draw = empate. SuddenDeath = se sigue jugando hasta que alguien puntue")]
    [SerializeField] TieRule tieRule = TieRule.Draw;

    [Header("UI (opcional)")]
    [SerializeField] TMP_Text timerText;

    float remaining;
    bool running;
    bool suddenDeath;
    bool endPending;

    public float Duration => matchDuration;
    public float Remaining => remaining;
    public float Progress => 1f - Mathf.Clamp01(remaining / matchDuration);
    public bool IsRunning => running;
    public bool IsSuddenDeath => suddenDeath;
    public event Action<Fighter> MatchEnded;

    void Awake()
    {
        remaining = matchDuration;
        UpdateText();
    }

    void OnEnable()
    {
        if (director != null) director.IntroFinished += StartTimer;
        if (scoreSystem != null) scoreSystem.ScoreChanged += OnScoreChanged;
    }

    void OnDisable()
    {
        if (director != null) director.IntroFinished -= StartTimer;
        if (scoreSystem != null) scoreSystem.ScoreChanged -= OnScoreChanged;
    }

    void StartTimer()
    {
        remaining = matchDuration;
        suddenDeath = false;
        endPending = false;
        running = true;
        UpdateText();
    }

    void Update()
    {
        if (endPending)
        {
            endPending = false;
            suddenDeath = false;
            EndMatch();
            return;
        }

        if (!running) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            remaining = 0f;
            running = false;
            TimeUp();
        }
        UpdateText();
    }

    void TimeUp()
    {
        float delta = scoreSystem != null ? scoreSystem.ScoreDelta : 0f;

        if (Mathf.Approximately(delta, 0f) && tieRule == TieRule.SuddenDeath)
        {
            suddenDeath = true; // seguimos jugando hasta que alguien puntue
            return;
        }
        EndMatch();
    }

    void OnScoreChanged(float left, float right)
    {
        if (suddenDeath && !Mathf.Approximately(left - right, 0f))
            endPending = true;
    }

    void EndMatch()
    {
        float delta = scoreSystem != null ? scoreSystem.ScoreDelta : 0f;
        Fighter winner = delta > 0f ? playerLeft : (delta < 0f ? playerRight : null);

        if (playerLeft != null) playerLeft.ClearEffects(false);
        if (playerRight != null) playerRight.ClearEffects(false);
        if (bugSpawner != null) bugSpawner.ClearAll();

        MatchEnded?.Invoke(winner);
        if (director != null) director.PlayWinnerOutro(winner);
    }

    public void RestartMatch()
    {
        running = false;
        suddenDeath = false;
        endPending = false;
        remaining = matchDuration;
        UpdateText();

        if (playerLeft != null) playerLeft.ClearEffects(true);
        if (playerRight != null) playerRight.ClearEffects(true);
        if (bugSpawner != null)
        {
            bugSpawner.ClearAll();
            bugSpawner.RestartSchedule();
        }
        if (scoreSystem != null) scoreSystem.ResetScores();
        if (director != null) director.RestartMatch();
    }

    void UpdateText()
    {

    }
}
