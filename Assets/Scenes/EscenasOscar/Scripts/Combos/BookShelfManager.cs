using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BookShelfManager : MonoBehaviour
{
    [Header("Plantas ")]
    public PlantData3D[] plants;
    private int currentIndex = 0;

    [Header("Libros")]
    public GameObject[] bookObjectsInScene; 

    [Header("Punto Mesa")]
    public Transform spawnPointMesa;

    private BookItem3D[] bookControllers;
    private Animator[] bookAnimators;
    private Vector3[] initialShelfPositions; 

    [Header("Escala")]
    public float transitionSpeed = 8f;
    public Vector3 shelfScale = Vector3.one;
    public Vector3 activeCenterScale = new Vector3(2.5f, 2.5f, 2.5f);

    [Header("Hover")]
    public float shelfHoverHeight = 0.15f;

    [Header("Controler")]
    public Key teclaConfirmarYAbir = Key.Enter;
    public Key teclaVolverOCerrar = Key.Escape;

    [Header("Estados")]
    private bool enMesa = false;
    private bool libroAbierto = false;
    private bool enTransicion = false;

    void Start()
    {
        if (plants.Length > 0 && bookObjectsInScene.Length >= plants.Length && spawnPointMesa != null)
        {
            InitializeExistingBooks();
            ForcedInstantPosition();
        }
       
    }

    private void InitializeExistingBooks()
    {
        int count = plants.Length;
        bookControllers = new BookItem3D[count];
        bookAnimators = new Animator[count];
        initialShelfPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            if (bookObjectsInScene[i] != null)
            {
                initialShelfPositions[i] = bookObjectsInScene[i].transform.position;

                bookControllers[i] = bookObjectsInScene[i].GetComponent<BookItem3D>();
                bookAnimators[i] = bookObjectsInScene[i].GetComponentInChildren<Animator>();

                if (bookControllers[i] != null && plants[i] != null)
                {
                    bookControllers[i].SetupCoverTexture(plants[i].bookCoverTexture);
                }
            }
        }
    }

    private void ForcedInstantPosition()
    {
        if (bookObjectsInScene == null) return;

        for (int i = 0; i < bookObjectsInScene.Length; i++)
        {
            if (bookObjectsInScene[i] == null) continue;

            bool isCurrent = (i == currentIndex) && !enMesa;
            Vector3 targetPos = isCurrent ? initialShelfPositions[i] + new Vector3(0f, shelfHoverHeight, 0f) : initialShelfPositions[i];
            Vector3 targetScale = isCurrent ? activeCenterScale : shelfScale;

            if (bookControllers[i] != null)
            {
                bookControllers[i].SetIsActive(isCurrent);
                bookControllers[i].SetTargetTransform(targetPos, transitionSpeed, targetScale);
            }
            bookObjectsInScene[i].transform.position = targetPos;
            bookObjectsInScene[i].transform.localScale = targetScale;
        }
    }

    void Update()
    {
        if (enMesa)
        {
            bool inputAccionAbrir = false;
            bool inputVolverBalda = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current[teclaConfirmarYAbir].wasPressedThisFrame) inputAccionAbrir = true;
                if (Keyboard.current[teclaVolverOCerrar].wasPressedThisFrame) inputVolverBalda = true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame) inputAccionAbrir = true;
                if (Gamepad.current.buttonEast.wasPressedThisFrame) inputVolverBalda = true;
            }

            if (inputAccionAbrir && !libroAbierto && !enTransicion)
            {
                StartCoroutine(SecuenciaAbrirLibro());
            }
            else if (inputVolverBalda && !enTransicion)
            {
                if (libroAbierto)
                {
                    StartCoroutine(SecuenciaCerrarYRegresarABalda());
                }
                else
                {
                    StartCoroutine(SecuenciaRegresarABaldaDirecto());
                }
            }
            return;
        }

        if (enTransicion) return;

        bool inputSiguiente = false;
        bool inputAnterior = false;
        bool inputConfirmar = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame) inputSiguiente = true;
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame) inputAnterior = true;
            if (Keyboard.current[teclaConfirmarYAbir].wasPressedThisFrame) inputConfirmar = true;
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.dpad.down.wasPressedThisFrame) inputSiguiente = true;
            if (Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.dpad.up.wasPressedThisFrame) inputAnterior = true;
            if (Gamepad.current.buttonSouth.wasPressedThisFrame) inputConfirmar = true;
        }

        if (inputSiguiente) NextBook();
        else if (inputAnterior) PreviousBook();
        else if (inputConfirmar) StartCoroutine(SecuenciaMoverALaMesa());

        UpdateShelfPositions();
    }

    public void NextBook()
    {
        if (plants.Length == 0) return;
        currentIndex = (currentIndex + 1) % plants.Length;
    }

    public void PreviousBook()
    {
        if (plants.Length == 0) return;
        currentIndex = (currentIndex - 1 + plants.Length) % plants.Length;
    }

    private void UpdateShelfPositions()
    {
        if (enMesa || bookObjectsInScene == null) return;

        for (int i = 0; i < bookObjectsInScene.Length; i++)
        {
            if (bookObjectsInScene[i] == null) continue;

            bool isCurrent = (i == currentIndex);

            Vector3 targetPos = initialShelfPositions[i];
            if (isCurrent)
            {
                targetPos += new Vector3(0f, shelfHoverHeight, 0f);
            }

            Vector3 targetScale = isCurrent ? activeCenterScale : shelfScale;

            if (bookControllers[i] != null)
            {
                bookControllers[i].SetIsActive(isCurrent);
                bookControllers[i].SetTargetTransform(targetPos, transitionSpeed, targetScale);
            }
        }
    }

    private IEnumerator SecuenciaMoverALaMesa()
    {
        enTransicion = true;
        enMesa = true;
        libroAbierto = false;

        if (bookControllers[currentIndex] != null)
        {
            bookControllers[currentIndex].SetTargetTransform(spawnPointMesa.position, transitionSpeed, activeCenterScale * 1.3f);
        }

        yield return new WaitForSeconds(0.4f);
        enTransicion = false;
    }

    private IEnumerator SecuenciaAbrirLibro()
    {
        enTransicion = true;

        if (bookObjectsInScene[currentIndex] != null && spawnPointMesa != null)
        {
            bookObjectsInScene[currentIndex].transform.position = spawnPointMesa.position;
        }

        if (bookAnimators[currentIndex] != null)
        {
            bookAnimators[currentIndex].SetTrigger("Abrir");
        }

        yield return new WaitForSeconds(0.3f);
        libroAbierto = true;
        enTransicion = false;
    }

    private IEnumerator SecuenciaCerrarYRegresarABalda()
    {
        enTransicion = true;
        libroAbierto = false;

        if (bookAnimators[currentIndex] != null)
        {
            bookAnimators[currentIndex].SetTrigger("Cerrar");
        }

        yield return new WaitForSeconds(0.5f);

        if (bookControllers[currentIndex] != null)
        {
            bookControllers[currentIndex].SetTargetTransform(initialShelfPositions[currentIndex], transitionSpeed, shelfScale);
        }

        yield return new WaitForSeconds(0.6f);

        enMesa = false;
        enTransicion = false;
        UpdateShelfPositions();
    }

    private IEnumerator SecuenciaRegresarABaldaDirecto()
    {
        enTransicion = true;
        libroAbierto = false;

        if (bookControllers[currentIndex] != null)
        {
            bookControllers[currentIndex].SetTargetTransform(initialShelfPositions[currentIndex], transitionSpeed, shelfScale);
        }

        yield return new WaitForSeconds(0.6f);

        enMesa = false;
        enTransicion = false;
        UpdateShelfPositions();
    }
}