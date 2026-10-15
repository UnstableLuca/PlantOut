using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShelfCenterSelectManager : MonoBehaviour
{
    [Header("Plantas")]
    public PlantData3D[] plants;
    private int currentIndex = 0;

    [Header("Baldas")]
    public Transform[] shelfSlots;

    [Header("Punto de Inspección")]
    public Transform spawnPoint3D;

    private GameObject[] bagInstances;
    private SeedBag3D[] bagControllers;

    [Header("Movimiento y Escala")]
    public float transitionSpeed = 8f;
    public Vector3 shelfScale = Vector3.one;
    public Vector3 activeCenterScale = new Vector3(2.9887f, 2.9887f, 2.9887f);

    [Header("PAPEL Y UI JUGADOR 1")]
    public Button botonConfirmarJ1;
    public GameObject objetoPapelJugador1;
    public TextMeshProUGUI txtPlantNameJ1;
    public TextMeshProUGUI txtDescriptionJ1;
    public TextMeshProUGUI txtCombosJ1;

    [Header("PAPEL Y UI JUGADOR 2")]
    public GameObject objetoPapelJugador2;
    public Animator animatorPapel2;
    public TextMeshProUGUI txtPlantNameJ2;
    public TextMeshProUGUI txtDescriptionJ2;
    public TextMeshProUGUI txtCombosJ2;

    [Header("DESCARTE")]
    public Transform puntoDescarte;

    [Header("PANTALLA VERSUS")]
    public CinemachineCamera vcamBalda;
    public CinemachineCamera vcamMesa;
    public GameObject panelPantallaVS;
    public Animator animatorPantallaVS;
    public float tiempoBlendCamara = 1.5f;

    [Header("VS Jugador 1")]
    public TextMeshProUGUI txtNameVS1;
    public TextMeshProUGUI txtDescVS1;
    public TextMeshProUGUI txtCombosVS1;
    public TextMeshProUGUI txtStatsVS1;

    [Header("VS Jugador 2")]
    public TextMeshProUGUI txtNameVS2;
    public TextMeshProUGUI txtDescVS2;
    public TextMeshProUGUI txtCombosVS2;
    public TextMeshProUGUI txtStatsVS2;

    [Header("Botones Mando")]
    public Button botonConfirmarJ2;
    public Button botonVolverJ2;
    public Button botonJugarVS;

    [Header("Combate")]
    public string nombreEscenaCombate = "cocos";

    private int turnoJugador = 1;
    private int seleccionJugador1 = -1;
    private int seleccionJugador2 = -1;
    private bool estaDescartando = false;
    private bool enPantallaVS = false;
    private bool enTransicion = false;

    void Start()
    {
        if (plants.Length > 0 && shelfSlots.Length >= plants.Length && spawnPoint3D != null)
        {
            InitializeBags();
            ForcedInstantPosition();
            UpdateUIAndStates();
        }

        if (objetoPapelJugador2 != null) objetoPapelJugador2.SetActive(false);
        if (panelPantallaVS != null) panelPantallaVS.SetActive(false);

        if (vcamBalda != null) vcamBalda.Priority = 20;
        if (vcamMesa != null) vcamMesa.Priority = 0;

        StartCoroutine(EstablecerFocoInicialJ1());
    }

    private IEnumerator EstablecerFocoInicialJ1()
    {
        yield return null;

        if (botonConfirmarJ1 != null)
        {
            botonConfirmarJ1.interactable = true;

            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(botonConfirmarJ1.gameObject);
        }
    }

    private void InitializeBags()
    {
        bagInstances = new GameObject[plants.Length];
        bagControllers = new SeedBag3D[plants.Length];

        for (int i = 0; i < plants.Length; i++)
        {
            if (plants[i].bagPrefab != null && shelfSlots[i] != null)
            {
                GameObject bag = Instantiate(plants[i].bagPrefab, shelfSlots[i].position, shelfSlots[i].rotation);
                bagInstances[i] = bag;

                SeedBag3D controller = bag.GetComponent<SeedBag3D>();
                bagControllers[i] = controller;

                if (controller != null)
                {
                    controller.SetupTextures(plants[i].frontLabelTexture, plants[i].backLabelTexture);
                }
            }
        }
    }

    private void ForcedInstantPosition()
    {
        if (bagInstances == null) return;

        for (int i = 0; i < bagInstances.Length; i++)
        {
            if (bagInstances[i] == null) continue;

            bool isCurrent = (i == currentIndex);
            Vector3 targetPos = isCurrent ? spawnPoint3D.position : shelfSlots[i].position;
            Vector3 targetScale = isCurrent ? activeCenterScale : shelfScale;

            if (bagControllers[i] != null)
            {
                bagControllers[i].SetIsActive(isCurrent);
                bagControllers[i].SetTargetTransform(targetPos, transitionSpeed, targetScale);
            }
            bagInstances[i].transform.position = targetPos;
            bagInstances[i].transform.localScale = targetScale;
        }
    }

    void Update()
    {
        if (enPantallaVS || enTransicion || turnoJugador > 2) return;

        bool inputSiguiente = false;
        bool inputAnterior = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                inputSiguiente = true;
            }
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                inputAnterior = true;
            }
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.down.wasPressedThisFrame || Gamepad.current.dpad.right.wasPressedThisFrame)
            {
                inputSiguiente = true;
            }
            if (Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.dpad.left.wasPressedThisFrame)
            {
                inputAnterior = true;
            }
        }

        if (inputSiguiente)
        {
            NextPlant();
        }
        else if (inputAnterior)
        {
            PreviousPlant();
        }

        UpdateShelfPositions();
    }

    public void NextPlant()
    {
        if (turnoJugador == 2 && estaDescartando) return;
        if (plants.Length == 0) return;
        currentIndex = (currentIndex + 1) % plants.Length;
        UpdateUIAndStates();
    }

    public void PreviousPlant()
    {
        if (turnoJugador == 2 && estaDescartando) return;
        if (plants.Length == 0) return;
        currentIndex = (currentIndex - 1 + plants.Length) % plants.Length;
        UpdateUIAndStates();
    }

    private void UpdateUIAndStates()
    {
        PlantData3D currentPlant = plants[currentIndex];

        if (turnoJugador == 1)
        {
            if (txtPlantNameJ1 != null) txtPlantNameJ1.text = currentPlant.plantName;
            if (txtDescriptionJ1 != null) txtDescriptionJ1.text = currentPlant.description;
            if (txtCombosJ1 != null)
            {
                txtCombosJ1.text = "COMBOS:\n• " + string.Join("\n• ", currentPlant.combos);
            }
        }
        else if (turnoJugador == 2)
        {
            if (txtPlantNameJ2 != null) txtPlantNameJ2.text = currentPlant.plantName;
            if (txtDescriptionJ2 != null) txtDescriptionJ2.text = currentPlant.description;
            if (txtCombosJ2 != null)
            {
                txtCombosJ2.text = "COMBOS:\n• " + string.Join("\n• ", currentPlant.combos);
            }
        }
    }

    private void UpdateShelfPositions()
    {
        if (bagInstances == null) return;

        for (int i = 0; i < bagInstances.Length; i++)
        {
            if (bagInstances[i] == null) continue;

            bool isCurrent = (i == currentIndex);

            Vector3 targetPos = isCurrent ? spawnPoint3D.position : shelfSlots[i].position;
            Vector3 targetScale = isCurrent ? activeCenterScale : shelfScale;

            if (bagControllers[i] != null)
            {
                bagControllers[i].SetIsActive(isCurrent);
                bagControllers[i].SetTargetTransform(targetPos, transitionSpeed, targetScale);
            }
        }
    }

    public void ConfirmarJugador1()
    {
        if (turnoJugador == 1)
        {
            seleccionJugador1 = currentIndex;
            turnoJugador = 2;
            estaDescartando = false;

            if (botonConfirmarJ1 != null) botonConfirmarJ1.interactable = false;

            UpdateUIAndStates();

            if (objetoPapelJugador2 != null)
            {
                objetoPapelJugador2.SetActive(true);
            }

            if (animatorPapel2 != null)
            {
                animatorPapel2.enabled = true;
                animatorPapel2.SetTrigger("Salir");
            }

            if (botonConfirmarJ2 != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(botonConfirmarJ2.gameObject);
            }
        }
    }

    public void RegresarAJugador1()
    {
        if (turnoJugador == 2 && !estaDescartando)
        {
            turnoJugador = 1;
            estaDescartando = true;

            if (botonConfirmarJ1 != null)
            {
                botonConfirmarJ1.interactable = true;
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(botonConfirmarJ1.gameObject);
            }

            if (animatorPapel2 != null) animatorPapel2.enabled = false;

            StartCoroutine(DeslizarHaciaPuntoYDesactivar());
        }
    }

    private IEnumerator DeslizarHaciaPuntoYDesactivar()
    {
        if (objetoPapelJugador2 == null) yield break;

        Transform transf = objetoPapelJugador2.transform;
        Vector3 posInicial = transf.position;
        Vector3 posFinal = (puntoDescarte != null) ? puntoDescarte.position : posInicial + new Vector3(10f, 0f, 0f);

        float tiempo = 0f;
        while (tiempo < 0.4f)
        {
            tiempo += Time.deltaTime;
            transf.position = Vector3.Lerp(posInicial, posFinal, Mathf.Clamp01(tiempo / 0.4f));
            yield return null;
        }

        objetoPapelJugador2.SetActive(false);
        transf.position = posInicial;
        estaDescartando = false;
    }

    public void ConfirmarJugador2()
    {
        if (turnoJugador == 2 && !estaDescartando && !enPantallaVS && !enTransicion)
        {
            seleccionJugador2 = currentIndex;
            StartCoroutine(SecuenciaLlegadaMesaVS());
        }
    }

    private IEnumerator SecuenciaLlegadaMesaVS()
    {
        enTransicion = true;
        turnoJugador = 3;

        for (int i = 0; i < bagControllers.Length; i++)
        {
            if (bagControllers[i] != null && shelfSlots[i] != null)
            {
                bagControllers[i].ReturnToShelf(shelfSlots[i].position, shelfScale);
            }
        }

        if (objetoPapelJugador1 != null) objetoPapelJugador1.SetActive(false);
        if (objetoPapelJugador2 != null) objetoPapelJugador2.SetActive(false);

        yield return new WaitForSeconds(0.1f);

        if (vcamBalda != null) vcamBalda.Priority = 0;
        if (vcamMesa != null) vcamMesa.Priority = 20;

        yield return new WaitForSeconds(tiempoBlendCamara);

        RellenarDatosPantallaVS();

        if (panelPantallaVS != null) panelPantallaVS.SetActive(true);
        yield return null;

        if (animatorPantallaVS != null)
        {
            animatorPantallaVS.SetTrigger("MostrarVS");
        }

        if (botonJugarVS != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(botonJugarVS.gameObject);
        }

        enPantallaVS = true;
        enTransicion = false;
    }

    private void RellenarDatosPantallaVS()
    {
        PlantData3D planta1 = plants[seleccionJugador1];
        PlantData3D planta2 = plants[seleccionJugador2];

        if (txtNameVS1) txtNameVS1.text = planta1.plantName;
        if (txtDescVS1) txtDescVS1.text = planta1.description;
        if (txtCombosVS1) txtCombosVS1.text = "COMBOS:\n• " + string.Join("\n• ", planta1.combos);

        if (txtNameVS2) txtNameVS2.text = planta2.plantName;
        if (txtDescVS2) txtDescVS2.text = planta2.description;
        if (txtCombosVS2) txtCombosVS2.text = "COMBOS:\n• " + string.Join("\n• ", planta2.combos);
    }

    public void BotonJugarPartida()
    {
        SceneManager.LoadScene(nombreEscenaCombate);
    }

    public void BotonReiniciarSeleccion()
    {
        if (enTransicion) return;
        StartCoroutine(SecuenciaReiniciarSeleccion());
    }

    private IEnumerator SecuenciaReiniciarSeleccion()
    {
        enTransicion = true;
        enPantallaVS = false;

        if (panelPantallaVS != null) panelPantallaVS.SetActive(false);

        if (vcamMesa != null) vcamMesa.Priority = 0;
        if (vcamBalda != null) vcamBalda.Priority = 20;

        yield return new WaitForSeconds(tiempoBlendCamara);

        for (int i = 0; i < bagControllers.Length; i++)
        {
            if (bagControllers[i] != null)
            {
                bagControllers[i].SetIsActive(false);
            }
        }

        turnoJugador = 1;
        currentIndex = 0;
        seleccionJugador1 = -1;
        seleccionJugador2 = -1;
        estaDescartando = false;

        if (objetoPapelJugador1 != null)
        {
            objetoPapelJugador1.SetActive(true);
        }
        if (botonConfirmarJ1 != null)
        {
            botonConfirmarJ1.interactable = true;
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(botonConfirmarJ1.gameObject);
        }

        if (objetoPapelJugador2 != null)
        {
            objetoPapelJugador2.SetActive(false);
        }
        if (animatorPapel2 != null)
        {
            animatorPapel2.enabled = false;
        }

        ForcedInstantPosition();
        UpdateUIAndStates();

        enTransicion = false;
    }
}