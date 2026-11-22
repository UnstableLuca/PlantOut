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

    [Header("Prefab de Combate / Juego")]
    public GameObject plantPrefab;
    [Header("Disponible en la seleccion")]
    [Tooltip("Desmarcar para bloquear la planta aunque tenga prefab")]
    public bool disponible = true; public bool EsSeleccionable =>
        disponible && plantPrefab != null && plantPrefab.GetComponentInChildren<PlantCharacterModule>(true) != null;
}

