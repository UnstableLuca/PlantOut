using System.Collections;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Cinemachine;
using Unity.Collections.Tests.CoreCLR.TestJobs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    
    [Header("Referencias")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image textBackgroundImage;
    [SerializeField] private Image namePanelImage;

    [Header("Camara")]
    [SerializeField] private CinemachineBrain cinemachineBrain;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private DialogueAsset currentDialogue;
    private InteractableObject currentInteractable;
    private CinemachineCamera activeDialogueCamera;

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool isDialogueActive = false;
    private Coroutine typingCoroutine;
    private InputActions inputActions;

    private void OnEnable()
    {
        inputActions.Plaza.Enable();
        inputActions.Plaza.DialogueSkip.performed += OnSkipPressed;
    }

    private void OnDisable()
    {
        inputActions.Plaza.DialogueSkip.performed -= OnSkipPressed;
        inputActions.Plaza.Disable();
    }

    private void OnSkipPressed(InputAction.CallbackContext context)
    {
        if (isDialogueActive)
        {
            DisplayNextLine();
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inputActions = new InputActions();

        if (dialogueCanvasGroup == null && dialoguePanel != null) dialogueCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();

        if (cinemachineBrain == null) cinemachineBrain = FindFirstObjectByType<CinemachineBrain>();

        HideUIImmediate();
    }

    public void StartDialogue(DialogueAsset dialogue)
    {
        StartDialogue(dialogue, null, null);
    }

    public void StartDialogue(DialogueAsset dialogue, CinemachineCamera targetCamera = null, InteractableObject interactable = null)
    {
        if (dialogue == null || dialogue.dialogueLines == null || dialogue.dialogueLines.Length == 0) return;

        currentDialogue = dialogue;
        currentInteractable = interactable;
        activeDialogueCamera = targetCamera;
        currentLineIndex = 0;
        isDialogueActive = true;

        StartCoroutine(StartDialogueSequence());
    }

    private IEnumerator StartDialogueSequence()
    {
        HideUIImmediate();

        if (activeDialogueCamera != null) activeDialogueCamera.Priority = 40;

        yield return new WaitForEndOfFrame();

        if (cinemachineBrain != null)
        {
            while (cinemachineBrain.IsBlending) yield return null;
        }
        else yield return new WaitForSeconds(0.8f);

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (dialogueCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                dialogueCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.25f);
                yield return null;
            }
            dialogueCanvasGroup.alpha = 1f;
        }

        DisplayNextLine();
    }

    public void DisplayNextLine()
    {
        if (currentDialogue == null) return;

        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            DialogueAsset.DialogueLine previousLine = currentDialogue.dialogueLines[currentLineIndex - 1];
            dialogueText.text = previousLine.sentence;
            isTyping = false;
            return;
        }

        if (currentLineIndex < currentDialogue.dialogueLines.Length)
        {
            DialogueAsset.DialogueLine currentLine = currentDialogue.dialogueLines[currentLineIndex];
            if (speakerNameText != null)
            {
                speakerNameText.text = currentLine.speakerName;
                speakerNameText.color = currentLine.nameColor;
            }

            if (namePanelImage != null) namePanelImage.color = currentLine.bgColor;

            DialoguePersonalityAsset personality = currentLine.linePersonality;

            if (personality != null)
            {
                if (textBackgroundImage != null && personality.textBackground != null) textBackgroundImage.sprite = personality.textBackground;

                audioSource.pitch = personality.basePitch;
            }

            typingCoroutine = StartCoroutine(TypeLineRoutine(currentLine, personality));
            currentLineIndex++;
        }
        else EndDialogue();
    }

    private IEnumerator TypeLineRoutine(DialogueAsset.DialogueLine line,  DialoguePersonalityAsset personality)
    {
        isTyping = true;
        dialogueText.text = "";

        float speed = (personality != null) ? personality.textSpeed : 0.05f;

        foreach (char letter in line.sentence.ToCharArray())
        {
            dialogueText.text += letter;

            if (personality != null && personality.typingSounds != null && personality.typingSounds.Length > 0 && !char.IsWhiteSpace(letter))
            {
                AudioClip clip = personality.typingSounds[Random.Range(0, personality.typingSounds.Length)];
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip);
                }
            }

            yield return new WaitForSeconds(speed);
        }

        isTyping = false;
    }

    public void EndDialogue()
    {
        StartCoroutine(EndDialogueSequence());
    }

    private IEnumerator EndDialogueSequence()
    {
        if (dialogueCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                dialogueCanvasGroup.alpha = Mathf.Lerp(1f, 0f , elapsed / 0.2f);
                yield return null;
            }
        }

        HideUIImmediate();

        if(activeDialogueCamera != null) activeDialogueCamera.Priority = 10;

        isDialogueActive = false;

        if (currentInteractable != null) currentInteractable.NotifyInteractionEnded();
    }

    private void HideUIImmediate()
    {
        if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = 0f;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }
}
