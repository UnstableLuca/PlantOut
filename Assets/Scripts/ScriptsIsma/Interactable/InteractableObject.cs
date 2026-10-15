using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class InteractableObject : MonoBehaviour
{
    [Header("Ajustes")]
    [SerializeField] private string interactionPrompt = "Interactuar";
    [SerializeField] private float interactionDistance = 4.0f;
    [SerializeField] private bool lockPlayerMovement = true;
    [SerializeField] private bool fadePlayerModel = true;

    [Header("Información")]
    [SerializeField] private CinemachineCamera dialogueCamera;
    [SerializeField] private DialogueAsset dialogueAsset;

    [Header("Interacción")]
    [SerializeField] private UnityEvent onInteract;

    private Outline outline;
    private bool playerIsNearby = false;
    private bool isInteracting = false;
    private PlayerMovement currentPlayer;
    private Transform playerTransform;
    private Renderer[] playerRenderers;

    private InputActions inputActions;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerTransform = player.GetComponent<Transform>();
        currentPlayer = player.GetComponent<PlayerMovement>();
        playerRenderers = player.GetComponentsInChildren<Renderer>();

        inputActions = new InputActions();
    }

    private void OnEnable()
    {
        inputActions.Plaza.Enable();
        inputActions.Plaza.Interact.performed += OnInteractPressed;
    }

    private void OnDisable()
    {
        inputActions.Plaza.Interact.performed -= OnInteractPressed;
        inputActions.Plaza.Disable();
    }

    private void OnInteractPressed(InputAction.CallbackContext context)
    {
        if (playerIsNearby && !isInteracting)
        {
            StartCoroutine(StartInteractionSequence());
        }
    }

    private IEnumerator StartInteractionSequence()
    {
        isInteracting = true;

        if (outline != null) outline.enabled = false;

        if (currentPlayer != null)
        {
            if (lockPlayerMovement) currentPlayer.DisableMovement();

            if (fadePlayerModel) yield return StartCoroutine(FadePlayerRoutine(1f, 0f));
        }

        if (DialogueManager.Instance != null && dialogueAsset != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueAsset, dialogueCamera, this);
        }

        onInteract?.Invoke();
    }

    public void NotifyInteractionEnded()
    {
        StartCoroutine(EndInteractionSequence());
    }

    private IEnumerator EndInteractionSequence()
    {
        if (currentPlayer != null)
        {
            if (fadePlayerModel) yield return StartCoroutine(FadePlayerRoutine(0f, 1f));

            if (lockPlayerMovement) currentPlayer.EnableMovement();
        }

        isInteracting = false;

        if (playerIsNearby && outline != null) outline.enabled = true;
    }

    private IEnumerator FadePlayerRoutine(float startAlpha, float targetAlpha)
    {
        if (playerRenderers == null || playerRenderers.Length == 0) yield break;

        float fadeDuration = 0.5f;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);

            foreach (Renderer rend in playerRenderers)
            {
                if (rend == null) continue;
                foreach (Material mat in rend.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color;
                        c.a = currentAlpha;
                        mat.color = c;
                    }
                }
            }
            yield return null;
        }
    }

    private void Update()
    {
        if (playerTransform == null || isInteracting) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        bool previousState = playerIsNearby;
        playerIsNearby = distance <= interactionDistance;

        if (playerIsNearby != previousState)
        {
            if (outline != null)
            {
                outline.enabled = playerIsNearby;
            }
        }
    }
}
