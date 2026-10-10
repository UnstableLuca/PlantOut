using UnityEngine;

[CreateAssetMenu(fileName = "NewPlantData3D", menuName = "Plant Selection/Plant Data 3D")]
public class PlantData3D : ScriptableObject
{
    [Header("Información Básica")]
    public string plantName;
    [TextArea] public string description;

    [Header("Modelo 3D y Texturas")]
    public GameObject bagPrefab;        
    public Texture2D frontLabelTexture;   
    public Texture2D backLabelTexture;    

    [Header("Combos")]
    public string[] combos;
}