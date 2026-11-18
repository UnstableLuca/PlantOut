using System.Collections;
using UnityEngine;

public class CinematicDirector : MonoBehaviour
{
    [Header("Jugadores")]
    [SerializeField] Fighter playerLeft;
    [SerializeField] Fighter playerRight;

    [Header("Sistemas de Escena (opcional)")]
    [SerializeField] BugSpawner bugSpawner;

    [Header("Tiempos de Cinemática")]
    [SerializeField] float introDuration = 2.0f;
    [SerializeField] float outroDuration = 3.0f;

    private PlantHeight heightLeft;
    private PlantHeight heightRight;
    private bool matchActive = false;

    public bool MatchActive => matchActive;

    void Awake()
    {
        if (playerLeft != null) heightLeft = playerLeft.GetComponent<PlantHeight>();
        if (playerRight != null) heightRight = playerRight.GetComponent<PlantHeight>();
    }

    void Start()
    {
        StartCoroutine(IntroSequence());
    }

    public IEnumerator IntroSequence()
    {
        matchActive = false;
        SetGameplayEnabled(false);

        heightLeft?.GoToStage();
        heightRight?.GoToStage();

        yield return new WaitForSeconds(introDuration);

        heightLeft?.LeaveStage();
        heightRight?.LeaveStage();

        SetGameplayEnabled(true);
        matchActive = true;
    }

    public void PlayWinnerOutro(Fighter winner)
    {
        if (!matchActive) return;
        matchActive = false;
        StartCoroutine(OutroSequence(winner));
    }

    private IEnumerator OutroSequence(Fighter winner)
    {
        SetGameplayEnabled(false);

        if (winner != null)
        {
            PlantHeight winnerHeight = winner.GetComponent<PlantHeight>();
            winnerHeight?.GoToStage();

            if (winner.Rival != null)
            {
                PlantHeight loserHeight = winner.Rival.GetComponent<PlantHeight>();
                loserHeight?.LeaveStage();
            }
        }

        yield return new WaitForSeconds(outroDuration);
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (playerLeft != null) playerLeft.InputEnabled = enabled;
        if (playerRight != null) playerRight.InputEnabled = enabled;
        if (bugSpawner != null) bugSpawner.Running = enabled;
    }
}