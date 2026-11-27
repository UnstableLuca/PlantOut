using System.Collections;
using System.Collections.Generic;
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

    [Header("Prioridad")]
    [SerializeField] private int interactionPriority = 0;

    [Header("Información")]
    [SerializeField] private CinemachineCamera dialogueCamera;
    [SerializeField] private DialogueAsset dialogueAsset;

    [Header("Interacción")]
    [SerializeField] private UnityEvent onInteract;

    private static readonly List<InteractableObject> all = new List<InteractableObject>();
    private static InteractableObject currentTarget;   
    private static bool anyInteracting;                // bloqueo del resto
    private static int lastEvaluatedFrame = -1;
    private const float SwitchHysteresis = 0.5f;       // evitar paerpadeo

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        all.Clear();
        currentTarget = null;
        anyInteracting = false;
        lastEvaluatedFrame = -1;
    }

    private Outline outline;
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
        if (player != null)
        {
            playerTransform = player.GetComponent<Transform>();
            currentPlayer = player.GetComponent<PlayerMovement>();
            playerRenderers = player.GetComponentsInChildren<Renderer>();
        }

        inputActions = new InputActions();
    }

    private void OnEnable()
    {
        all.Add(this);

        inputActions.Plaza.Enable();
        inputActions.Plaza.Interact.performed += OnInteractPressed;
    }

    private void OnDisable()
    {
        inputActions.Plaza.Interact.performed -= OnInteractPressed;
        inputActions.Plaza.Disable();

        all.Remove(this);
        if (currentTarget == this) currentTarget = null;

        if (isInteracting)
        {
            isInteracting = false;
            anyInteracting = false;
        }
    }

    private void OnInteractPressed(InputAction.CallbackContext context)
    {
        if (currentTarget != this || anyInteracting || isInteracting) return;

        if (PlazaManager.Instance != null && PlazaManager.Instance.CurrentState != GameState.PlazaGameplay) return;

        StartCoroutine(StartInteractionSequence());
    }

    private IEnumerator StartInteractionSequence()
    {
        isInteracting = true;
        anyInteracting = true;

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
        anyInteracting = false; 
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

    private float DistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue;
        return Vector3.Distance(transform.position, playerTransform.position);
    }
    private static void EvaluateTarget()
    {
        if (anyInteracting) return;

        InteractableObject best = null;
        float bestDist = float.MaxValue;

        foreach (InteractableObject item in all)
        {
            if (item == null || !item.isActiveAndEnabled) continue;

            float d = item.DistanceToPlayer();
            if (d > item.interactionDistance) continue;

            bool wins = best == null
                || item.interactionPriority > best.interactionPriority
                || (item.interactionPriority == best.interactionPriority && d < bestDist);

            if (wins)
            {
                best = item;
                bestDist = d;
            }
        }

        
        if (currentTarget != null && best != null && currentTarget != best && currentTarget.isActiveAndEnabled)
        {
            float currentDist = currentTarget.DistanceToPlayer();
            if (currentDist <= currentTarget.interactionDistance
                && currentTarget.interactionPriority == best.interactionPriority
                && currentDist <= bestDist + SwitchHysteresis)
            {
                best = currentTarget;
            }
        }

        currentTarget = best;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        if (lastEvaluatedFrame != Time.frameCount)
        {
            lastEvaluatedFrame = Time.frameCount;
            EvaluateTarget();
        }

        bool isTarget = currentTarget == this && !anyInteracting;

        if (outline != null && outline.enabled != isTarget)
        {
            outline.enabled = isTarget;
        }
    }
}
