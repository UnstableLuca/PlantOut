using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

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
    [SerializeField] private PauseController pauseController;

    [Header("Camaras")]
    [SerializeField] private CinemachineCamera vcamSplash;
    [SerializeField] private CinemachineCamera vcamPause;
    [SerializeField] private CinemachineCamera vcamPlaza;

    public GameState CurrentState { get; private set; }

    private InputActions inputActions;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inputActions = new InputActions();
    }

    private void OnEnable()
    {
        inputActions.Plaza.Enable();
        inputActions.Plaza.Pause.performed += OnPausePressed;
    }

    private void OnDisable()
    {
        inputActions.Plaza.Pause.performed -= OnPausePressed;
        inputActions.Plaza.Disable();
    }

    private void Start()
    {
        SetState(GameState.Splash);
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        if (CurrentState == GameState.PlazaGameplay)
        {
            SetState(GameState.MainMenu);
        }
        else if (CurrentState == GameState.MainMenu)
        {
            if (pauseController != null)
            {
                pauseController.HideMenu(() =>
                {
                    SetState(GameState.PlazaGameplay);
                });
            }
            else
            {
                SetState(GameState.PlazaGameplay);
            }
        }
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
                if (vcamSplash) vcamSplash.Priority = 20;

                splashUI.SetActive(true);

                if (playerMovement) playerMovement.DisableMovement();
                break;

            case GameState.MainMenu:
                if (vcamPause) vcamPause.Priority = 20;

                if (playerMovement) playerMovement.DisableMovement();

                if (pauseController) pauseController.ShowMenu();
                break;

            case GameState.PlazaGameplay:
                if (vcamPlaza) vcamPlaza.Priority = 20;

                if (playerMovement) playerMovement.EnableMovement();
                break;
        }
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