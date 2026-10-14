using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueAsset", menuName = "Dialogue/Dialogue Asset")]
public class DialogueAsset : ScriptableObject
{
    [System.Serializable]
    public struct DialogueLine
    {
        [Header("Información del hablante")]
        public string speakerName;
        public Color nameColor;
        public Color bgColor;

        [Header("Personalidad")]
        public DialoguePersonalityAsset linePersonality;

        [Header("Contenido")]
        [TextArea(3, 10)]
        public string sentence;
    }

    [Header("Líneas de diálogo")]
    public DialogueLine[] dialogueLines;
}
