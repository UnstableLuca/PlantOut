using UnityEngine;

[CreateAssetMenu(fileName = "NewPesonalityAsset", menuName = "Dialogue/Personality Asset")]
public class DialoguePersonalityAsset : ScriptableObject
{
    [Header("Identificador")]
    public string personalityName;
    public Sprite textBackground;

    [Header("Ritmo y Velocidad")]
    [Range(0.01f, 0.2f)] public float textSpeed = 0.05f;
    [Range(0.5f, 2.0f)] public float basePitch = 1.0f;

    [Header("Voz")]
    public AudioClip[] typingSounds;
}
