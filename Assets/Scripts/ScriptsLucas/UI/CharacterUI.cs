using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Fighter/UI/Name", fileName = "NewName")]
public class CharacterUI : ScriptableObject
{
    public Color backgroundColor;
    public string plantName;
    [System.NonSerialized] public GameObject leftEnergyBarPrefab;
    [System.NonSerialized] public GameObject rightEnergyBarPrefab;

    public GameObject characterPrefab;
}
