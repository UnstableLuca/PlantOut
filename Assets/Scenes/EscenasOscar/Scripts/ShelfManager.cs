using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

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

    private int turnoJugador = 1;
    private int seleccionJugador1 = -1;
    private bool estaDescartando = false; 

    void Start()
    {
        if (plants.Length > 0 && shelfSlots.Length >= plants.Length && spawnPoint3D != null)
        {
            InitializeBags();
            ForcedInstantPosition();
            UpdateUIAndStates();
        }
        

        if (objetoPapelJugador2 != null)
        {
            objetoPapelJugador2.SetActive(false);
        }

        if (botonConfirmarJ1 != null)
        {
            botonConfirmarJ1.interactable = true;
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
        if (Keyboard.current != null)
        {
            if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                NextPlant();
            }
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                PreviousPlant();
            }
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

            if (botonConfirmarJ1 != null)
            {
                botonConfirmarJ1.interactable = false;
            }

            if (objetoPapelJugador2 != null)
            {
                objetoPapelJugador2.SetActive(true);
            }

            if (animatorPapel2 != null)
            {
                animatorPapel2.enabled = true;
                animatorPapel2.SetTrigger("Salir");
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
            }

            if (animatorPapel2 != null)
            {
                animatorPapel2.enabled = false;
            }

            StartCoroutine(DeslizarHaciaPuntoYDesactivar());
        }
    }

    private IEnumerator DeslizarHaciaPuntoYDesactivar()
    {
        if (objetoPapelJugador2 == null) yield break;

        Transform transf = objetoPapelJugador2.transform;

        Vector3 posInicial = transf.position;
        Vector3 posFinal = (puntoDescarte != null) ? puntoDescarte.position : posInicial + new Vector3(10f, 0f, 0f);

        float tiempoTranscurrido = 0f;
        float duracionAnimacion = 0.4f; 

        while (tiempoTranscurrido < duracionAnimacion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float porcentaje = Mathf.Clamp01(tiempoTranscurrido / duracionAnimacion);

            transf.position = Vector3.Lerp(posInicial, posFinal, porcentaje);
            yield return null;
        }

        transf.position = posFinal;

        objetoPapelJugador2.SetActive(false);
        transf.position = posInicial;
        estaDescartando = false;
    }

    
    public void ConfirmarJugador2()
    {
        if (turnoJugador == 2 && !estaDescartando)
        {
            int seleccionJugador2 = currentIndex;
        }
    }
}