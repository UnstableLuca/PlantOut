using UnityEngine;

[System.Serializable]
public class ComboVisual
{
    [Header("Imágenes para Teclado / Ratón")]
    public Sprite[] botonesTeclado;

    [Header("Imágenes para Mando (Gamepad)")]
    public Sprite[] botonesMando;
}

[CreateAssetMenu(fileName = "NewPlantData3D", menuName = "Plant Selection/Plant Data 3D")]
public class PlantData3D : ScriptableObject
{
    [Header("Información Básica")]
    public string plantName;
    [TextArea] public string description;

    [Header("Modelo 3D y Texturas (Escena de Selección / Bolsas)")]
    public GameObject bagPrefab;
    public Texture2D frontLabelTexture;
    public Texture2D backLabelTexture;

    [Header("Modelo 3D y Portada (Escena del Libro / Enciclopedia)")]
    public GameObject bookPrefab;
    public Texture2D bookCoverTexture;

    [Header("Combos Visuales (Iconos Dinámicos)")]
    public ComboVisual[] combosVisuales;
}