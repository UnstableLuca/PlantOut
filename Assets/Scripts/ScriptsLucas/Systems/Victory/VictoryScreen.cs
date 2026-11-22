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
    [Tooltip("Giro de las plantas para que miren a la camara (grados)")]
    [SerializeField] Vector3 modelRotation = new Vector3(0f, 180f, 0f);
    [Tooltip("Escala de las plantas en la victoria. La misma para las dos, para que mantengan su tamano real")]
    [SerializeField, Min(0.01f)] float modelScale = 40f;
    [Tooltip("Desplazamiento extra (p. ej. para subirlas encima de la maceta)")]
    [SerializeField] Vector3 positionOffset = Vector3.zero;
    [Tooltip("Oculta el tallo de las plantas en la pantalla de victoria (p. ej. para poner una maceta)")]
    [SerializeField] bool hideStem = true;

    [Header("Textos (opcional)")]
    [SerializeField] TMP_Text winnerText;
    [SerializeField] TMP_Text loserText;
    [Tooltip("Cambia 'Ganador' / 'Perdedor' por el nombre de cada planta")]
    [SerializeField] bool showPlantNames = true;
    [SerializeField] string drawText = "Empate";

    [Header("Ocultar al mostrar la victoria (opcional)")]
    [Tooltip("Ej: la interfaz de la partida o la camara principal")]
    [SerializeField] GameObject[] hideOnVictory;


    [Header("Botones")]
    [Tooltip("Nombre exacto de la escena del menu (tiene que estar en la lista de escenas del build)")]
    [SerializeField] string menuSceneName = "";
    [Tooltip("Boton que queda seleccionado al salir la victoria (para jugar con mando)")]
    [SerializeField] Button firstSelected;
    Fighter pendingWinner;
    bool waitingOutro;
    GameObject winnerModel;
    GameObject loserModel;

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
        winnerModel = Place(win, winnerPlaceholder);
        loserModel = Place(lose, loserPlaceholder);

        SetText(winnerText, draw ? drawText : (showPlantNames && win != null ? win.plantName : null));
        SetText(loserText, draw ? drawText : (showPlantNames && lose != null ? lose.plantName : null));

        if (firstSelected != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
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
        if (winnerModel != null) Destroy(winnerModel);
        if (loserModel != null) Destroy(loserModel);
        winnerModel = null;
        loserModel = null;

        if (winnerPlaceholder != null) winnerPlaceholder.gameObject.SetActive(true);
        if (loserPlaceholder != null) loserPlaceholder.gameObject.SetActive(true);
    }

    static void SetText(TMP_Text text, string value)
    {
        if (text != null && !string.IsNullOrEmpty(value)) text.text = value;
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

    GameObject Place(CharacterUI plant, Transform placeholder)
    {
        if (plant == null || plant.characterPrefab == null || placeholder == null) return null;

        // Hueco que ocupaba la caja
        Renderer box = placeholder.GetComponentInChildren<Renderer>();
        Bounds slot = box != null ? box.bounds : new Bounds(placeholder.position, Vector3.zero);
        placeholder.gameObject.SetActive(false);

        GameObject model = CreateDisplayCopy(plant.characterPrefab, placeholder.parent);
        Transform t = model.transform;

        // El pivote de las plantas esta en su base: se apoya en el centro de la base de la caja
        t.position = new Vector3(slot.center.x, slot.min.y, slot.center.z) + positionOffset;
        t.rotation = placeholder.rotation * Quaternion.Euler(modelRotation);
        SetWorldScale(t, modelScale);

        return model;
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
        Debug.Log("Me voy al menu");
        SceneManager.LoadScene(menuSceneName);
    }

    public void Rematch()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}