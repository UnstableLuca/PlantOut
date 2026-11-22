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

    public Sprite image;
    [Header("Palabras de golpe a distancia")]
    [Tooltip("Imagenes que pueden salir al acertar un disparo (se elige una al azar)")]
    public Sprite[] hitWords;
    [Tooltip("Posicion en el Canvas cuando la planta esta a la IZQUIERDA (anclas en el centro). A la derecha se refleja en X")]
    public Vector2 hitWordPosition = new Vector2(-800f, 15f);
    [Tooltip("Tamano del recuadro de la palabra (la imagen mantiene su proporcion)")]
    public Vector2 hitWordSize = new Vector2(100f, 100f);
    [Header("Pantalla de victoria")]
    [Tooltip("Ajuste de esta planta en la victoria, en unidades de la planta (p. ej. Y negativo para bajarla hasta la tierra de la maceta)")]
    public Vector3 victoryOffset = Vector3.zero;
    [Tooltip("Multiplicador de tamano de esta planta en la victoria")]
    [Min(0.01f)] public float victoryScale = 1f;
    [Tooltip("Giro extra de esta planta en la victoria (grados)")]
    public Vector3 victoryRotation = Vector3.zero;
}
