using UnityEngine;

[System.Serializable]
public class ComboVisual
{
    [Header("Teclado")]
    public Sprite[] botonesTeclado;

    [Header("Mando")]
    public Sprite[] botonesMando;
}

[CreateAssetMenu(fileName = "NewPlantData3D", menuName = "Plant Selection/Plant Data 3D")]
public class PlantData3D : ScriptableObject
{
    [Header("Información")]
    public string plantName;
    [TextArea] public string description;

    [Header("Estadísticas")]
    [TextArea] public string estadisticas; 

    [Header("Modelo 3D y Texturas Bolsa")]
    public GameObject bagPrefab;
    public Texture2D frontLabelTexture;
    public Texture2D backLabelTexture;

    [Header("Modelo 3D Libro")]
    public GameObject bookPrefab;
    

    [Header("Combos Visuales")]
    public ComboVisual[] combosVisuales;
}