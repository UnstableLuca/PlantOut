using UnityEngine;
using Unity.Cinemachine;

public enum GameState
{
    Splash,
    MainMenu,
    PlazaGameplay
}

public class PlazaManager : MonoBehaviour
{
    public static PlazaManager Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private GameObject splashUI;
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject plazaUI;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Camaras")]
    [SerializeField] private CinemachineCamera vcamSplash;
    [SerializeField] private CinemachineCamera vcamPause;
    [SerializeField] private CinemachineCamera vcamPlaza;

    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        SetState(GameState.Splash);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        if (vcamSplash) vcamSplash.Priority = 10;
        if (vcamPause) vcamPause.Priority = 10;
        if (vcamPlaza) vcamPause.Priority = 10;

        switch (newState)
        {
            case GameState.Splash:
                if (splashUI) splashUI.SetActive(true);
                if (pauseUI) pauseUI.SetActive(false);
                if (plazaUI) plazaUI.SetActive(false);

                if (vcamSplash) vcamSplash.Priority = 20;

                if (playerMovement) playerMovement.DisableMovement();
                break;

            case GameState.MainMenu:
                if (splashUI) splashUI.SetActive(false);
                if (pauseUI) pauseUI.SetActive(true);
                if (plazaUI) plazaUI.SetActive(false);

                if (vcamPause) vcamPause.Priority = 20;

                if (playerMovement) playerMovement.DisableMovement();
                break;

            case GameState.PlazaGameplay:
                if (splashUI) splashUI.SetActive(false);
                if (pauseUI) pauseUI.SetActive(false);
                if (plazaUI) plazaUI.SetActive(true);

                if (vcamPlaza) vcamPlaza.Priority = 20;

                if (playerMovement) playerMovement.EnableMovement();
                break;
        }
    }
    
    public void Button_EnterPlaza()
    {
        SetState(GameState.PlazaGameplay);
    }

    public void SetCustomCamera(CinemachineCamera targetCamera)
    {
        if (vcamSplash) vcamSplash.Priority = 10;
        if (vcamPause) vcamPause.Priority = 10;
        if (vcamPlaza) vcamPlaza.Priority = 10;

        if (targetCamera != null)
        {
            targetCamera.Priority = 20;
        }
    }
}