using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryScreen : MonoBehaviour
{
    [Header("Partida")]
    [SerializeField] MatchTimer matchTimer;
    [SerializeField] MatchSetupManager matchSetup;
    [Tooltip("Si se asigna, la victoria sale al acabar el outro del ganador. Vacio = sale al instante")]
    [SerializeField] CinematicDirector director;

    [Header("Pantalla de victoria")]
    [Tooltip("El objeto Victoria (empieza desactivado)")]
    [SerializeField] GameObject victoryRoot;
    [Tooltip("Caja que marca el sitio de la planta ganadora (se oculta)")]
    [SerializeField] Transform winnerPlaceholder;
    [Tooltip("Caja que marca el sitio de la planta perdedora (se oculta)")]
    [SerializeField] Transform loserPlaceholder;
    [Tooltip("Objeto al que se pega la planta ganadora y la mueve/gira con el (p. ej. su maceta). Vacio = el padre de la caja")]
    [SerializeField] Transform winnerFollow;
    [Tooltip("Igual para la perdedora (p. ej. la maceta que se cae)")]
    [SerializeField] Transform loserFollow;
    [Tooltip("Giro de las plantas para que miren a la camara (grados)")]
    [SerializeField] Vector3 modelRotation = new Vector3(0f, 180f, 0f); [Tooltip("Giro extra solo para la planta ganadora (grados)")]
    [SerializeField] Vector3 winnerRotation = Vector3.zero;
    [Tooltip("Giro extra solo para la planta perdedora (grados)")]
    [SerializeField] Vector3 loserRotation = Vector3.zero;
    [Tooltip("Escala de las plantas en la victoria. La misma para las dos, para que mantengan su tamano real")]
    [SerializeField, Min(0.01f)] float modelScale = 40f;
    [Tooltip("Desplazamiento comun a las dos (p. ej. la altura de la maceta). El ajuste de cada planta va en su CharacterUI")]
    [SerializeField] Vector3 positionOffset = Vector3.zero;
    [Tooltip("Oculta el tallo de las plantas en la pantalla de victoria (p. ej. para poner una maceta)")]
    [SerializeField] bool hideStem = true;

    [Header("Textos (opcional)")]
    [SerializeField] TMP_Text winnerText;
    [SerializeField] TMP_Text loserText;
    [Tooltip("Cambia 'Ganador' / 'Perdedor' por el nombre de cada planta")]
    [SerializeField] bool showPlantNames = true;
    [SerializeField] string drawText = "Empate"; 
    [Tooltip("Fondo detras del nombre del ganador: toma el color de su planta")]
    [SerializeField] Image winnerNameBox;
    [Tooltip("Fondo detras del nombre del perdedor: toma el color de su planta")]
    [SerializeField] Image loserNameBox;

    [Header("Ocultar al mostrar la victoria (opcional)")]
    [Tooltip("Ej: la interfaz de la partida o la camara principal")]
    [SerializeField] GameObject[] hideOnVictory;

    [Header("Botones")]
    [Tooltip("Nombre exacto de la escena del menu (tiene que estar en la lista de escenas del build)")]
    [SerializeField] string menuSceneName = "";
    [Tooltip("Boton que queda seleccionado al salir la victoria (para jugar con mando)")]
    [SerializeField] Button firstSelected;

    class Placed
    {
        public GameObject model;
        public CharacterUI plant;
        public Vector3 localBase;         // centro de la base de la caja, respecto al padre
        public Quaternion localRotation;  // giro de la caja, respecto al padre

        public bool isWinner;
    }

    Fighter pendingWinner;
    bool waitingOutro;
    Placed winnerPlaced;
    Placed loserPlaced;

    void OnEnable()
    {
        if (matchTimer != null) matchTimer.MatchEnded += OnMatchEnded;
        if (director != null) director.OutroFinished += OnOutroFinished;
    }

    void OnDisable()
    {
        if (matchTimer != null) matchTimer.MatchEnded -= OnMatchEnded;
        if (director != null) director.OutroFinished -= OnOutroFinished;
    }

    void Start()
    {
        if (victoryRoot != null) victoryRoot.SetActive(false);
    }

    void LateUpdate()
    {
        // Se recolocan cada frame: asi se pueden ajustar los valores en Play y ver el resultado al momento
        Apply(winnerPlaced);
        Apply(loserPlaced);
    }

    void OnMatchEnded(Fighter winner)
    {
        pendingWinner = winner;
        waitingOutro = director != null;
        if (!waitingOutro) Show(winner);
    }

    void OnOutroFinished()
    {
        if (!waitingOutro) return;
        waitingOutro = false;
        Show(pendingWinner);
    }

    /// <summary>Muestra la victoria. winner = null es empate.</summary>
    public void Show(Fighter winner)
    {
        CharacterUI left = matchSetup != null ? matchSetup.LeftCharacter : null;
        CharacterUI right = matchSetup != null ? matchSetup.RightCharacter : null;

        bool draw = winner == null;
        bool leftWins = draw || winner.isLeftPlayer;
        CharacterUI win = leftWins ? left : right;
        CharacterUI lose = leftWins ? right : left;

        if (hideOnVictory != null)
            foreach (GameObject go in hideOnVictory)
                if (go != null) go.SetActive(false);

        if (victoryRoot != null) victoryRoot.SetActive(true);

        ClearModels();
        winnerPlaced = Place(win, winnerPlaceholder, winnerFollow, true);
        loserPlaced = Place(lose, loserPlaceholder, loserFollow, false);

        SetText(winnerText, draw ? drawText : (showPlantNames && win != null ? win.plantName : null));
        SetText(loserText, draw ? drawText : (showPlantNames && lose != null ? lose.plantName : null));

        if (firstSelected != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);

        if (!draw)
        {
            SetBoxColor(winnerNameBox, win);
            SetBoxColor(loserNameBox, lose);
        }
    }

    /// <summary>Quita la victoria y deja todo como al principio (para reiniciar).</summary>
    public void Hide()
    {
        ClearModels();
        if (victoryRoot != null) victoryRoot.SetActive(false);

        if (hideOnVictory != null)
            foreach (GameObject go in hideOnVictory)
                if (go != null) go.SetActive(true);
    }

    void ClearModels()
    {
        if (winnerPlaced != null && winnerPlaced.model != null) Destroy(winnerPlaced.model);
        if (loserPlaced != null && loserPlaced.model != null) Destroy(loserPlaced.model);
        winnerPlaced = null;
        loserPlaced = null;

        if (winnerPlaceholder != null) winnerPlaceholder.gameObject.SetActive(true);
        if (loserPlaceholder != null) loserPlaceholder.gameObject.SetActive(true);
    }

    static void SetText(TMP_Text text, string value)
    {
        if (text != null && !string.IsNullOrEmpty(value)) text.text = value;
    }

    static void SetBoxColor(Image box, CharacterUI plant)
    {
        if (box == null || plant == null) return;

        Color c = plant.backgroundColor;
        c.a = box.color.a;
        box.color = c;
    }

    Placed Place(CharacterUI plant, Transform placeholder, Transform follow, bool isWinner)
    {

        if (plant == null || plant.characterPrefab == null || placeholder == null) return null;
        // Hueco que ocupaba la caja
        Renderer box = placeholder.GetComponentInChildren<Renderer>();
        Bounds slot = box != null ? box.bounds : new Bounds(placeholder.position, Vector3.zero);
        placeholder.gameObject.SetActive(false);

        Transform parent = follow != null ? follow : placeholder.parent;
        Vector3 worldBase = new Vector3(slot.center.x, slot.min.y, slot.center.z);

        Placed p = new Placed
        {
            model = CreateDisplayCopy(plant.characterPrefab, parent),
            plant = plant,
            // Respecto al padre: si el padre se mueve o gira (maceta que se cae), la planta va con el
            localBase = parent != null ? parent.InverseTransformPoint(worldBase) : worldBase,
            localRotation = parent != null ? Quaternion.Inverse(parent.rotation) * placeholder.rotation : placeholder.rotation,
            isWinner = isWinner
        };

        Apply(p);
        return p;
    }

    void Apply(Placed p)
    {
        if (p == null || p.model == null || p.plant == null) return;

        Transform t = p.model.transform;
        Transform parent = t.parent;
        Quaternion parentRotation = parent != null ? parent.rotation : Quaternion.identity;

        Vector3 slotRotation = p.isWinner ? winnerRotation : loserRotation;
        t.rotation = parentRotation * p.localRotation
                   * Quaternion.Euler(modelRotation)
                   * Quaternion.Euler(slotRotation)
                   * Quaternion.Euler(p.plant.victoryRotation);
        SetWorldScale(t, modelScale * Mathf.Max(0.01f, p.plant.victoryScale));

        // El pivote de las plantas esta en su base. positionOffset gira con el padre; victoryOffset va en unidades de la planta
        Vector3 worldBase = parent != null ? parent.TransformPoint(p.localBase) : p.localBase;
        t.position = worldBase + parentRotation * positionOffset + t.TransformVector(p.plant.victoryOffset);
    }

    GameObject CreateDisplayCopy(GameObject prefab, Transform parent)
    {
        GameObject holder = new GameObject("VictoryHolder");
        holder.SetActive(false);   // dentro de un objeto apagado no se ejecuta ningun Awake

        GameObject copy = Instantiate(prefab, holder.transform);

        if (hideStem) HideStem(copy);

        foreach (MonoBehaviour script in copy.GetComponentsInChildren<MonoBehaviour>(true))
            DestroyImmediate(script);

        copy.transform.SetParent(parent, false);
        Destroy(holder);
        return copy;
    }

    static void HideStem(GameObject plant)
    {
        // 1) El tallo asignado en el PlantCharacterModule
        PlantCharacterModule module = plant.GetComponentInChildren<PlantCharacterModule>(true);
        if (module != null && module.stemTransform != null)
        {
            module.stemTransform.gameObject.SetActive(false);
            return;
        }

        // 2) Si no tiene, cualquier hijo que se llame "stem" o "tallo"
        foreach (Transform child in plant.GetComponentsInChildren<Transform>(true))
        {
            string n = child.name.ToLowerInvariant();
            if (n.Contains("stem") || n.Contains("tallo")) child.gameObject.SetActive(false);
        }
    }

    // Escala uniforme en el mundo aunque el padre tenga una escala rara
    static void SetWorldScale(Transform t, float s)
    {
        Vector3 p = t.parent != null ? t.parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(s / p.x, s / p.y, s / p.z);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        GameSessionData.ClearSelection();

        if (string.IsNullOrEmpty(menuSceneName) || !Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogWarning($"[VictoryScreen] La escena del menu '{menuSceneName}' no existe o no esta en el build.");
            return;
        }
        SceneManager.LoadScene(menuSceneName);
    }

    public void Rematch()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}