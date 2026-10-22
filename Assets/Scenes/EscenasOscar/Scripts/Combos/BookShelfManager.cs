
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BookShelfManager : MonoBehaviour
{
    [Header("Libros")]
    public GameObject[] bookObjectsInScene;

    [Header("Punto")]
    public Transform spawnPointMesa;

    [Header("Escala")]
    public float transitionSpeed = 8f;
    public Vector3 shelfScale = Vector3.one;
    public Vector3 activeCenterScale = new Vector3(2.5f, 2.5f, 2.5f);

    [Header("Hover")]
    public float shelfHoverHeight = 0.15f;

    [Header("Controles abrir libro")]
    public Key teclaConfirmarYAbir = Key.Enter;
    public Key teclaVolverOCerrar = Key.Escape;

    [Header("DEV MANDO")]
    public bool forzarModoMando = false;

    private BookItem3D[] bookControllers;
    private Animator[] bookAnimators;
    private Vector3[] initialShelfPositions;

    private int currentIndex;
    private bool enMesa;
    private bool libroAbierto;
    private bool enTransicion;

    private bool estadoMandoAnterior;

    public bool UsarMandoActual =>
        forzarModoMando || Gamepad.current != null;


    private void Start()
    {
        CacheReferences();

        currentIndex = 0;

        estadoMandoAnterior = UsarMandoActual;

        ActualizarModoControl();

        ForcedInstantPosition();
    }


    private void Update()
    {
       
        bool mandoActualConectado = UsarMandoActual;

        if (mandoActualConectado != estadoMandoAnterior)
        {
            estadoMandoAnterior = mandoActualConectado;
            ActualizarModoControl();
        }

        if (enTransicion ||
            bookObjectsInScene == null ||
            bookObjectsInScene.Length == 0)
        {
            return;
        }

        if (enMesa)
        {
            ProcesarControlesMesa();
        }
        else
        {
            ProcesarControlesBalda();
            UpdateShelfPositions();
        }
    }

    

    public void ToggleModoMando()
    {
        forzarModoMando = !forzarModoMando;

        estadoMandoAnterior = UsarMandoActual;
        ActualizarModoControl();

       
    }



    private void ActualizarModoControl()
    {
        if (bookControllers == null)
            return;

        foreach (BookItem3D libro in bookControllers)
        {
            if (libro != null && libro.gameObject.activeInHierarchy)
            {
                libro.ActualizarIconosControl(UsarMandoActual);
            }
        }
    }


    private void ProcesarControlesBalda()
    {
        bool siguiente = false;
        bool anterior = false;
        bool confirmar = false;

        if (Keyboard.current != null)
        {
            Keyboard teclado = Keyboard.current;

            siguiente =
                teclado.rightArrowKey.wasPressedThisFrame ||
                teclado.dKey.wasPressedThisFrame ||
                siguiente;

            anterior =
                teclado.leftArrowKey.wasPressedThisFrame ||
                teclado.aKey.wasPressedThisFrame ||
                anterior;

            confirmar =
                teclado[teclaConfirmarYAbir].wasPressedThisFrame ||
                confirmar;
        }

        if (Gamepad.current != null)
        {
            Gamepad mando = Gamepad.current;

            siguiente =
                mando.dpad.right.wasPressedThisFrame ||
                mando.dpad.down.wasPressedThisFrame ||
                siguiente;

            anterior =
                mando.dpad.left.wasPressedThisFrame ||
                mando.dpad.up.wasPressedThisFrame ||
                anterior;

            confirmar =
                mando.buttonSouth.wasPressedThisFrame ||
                confirmar;
        }

        if (siguiente)
            NextBook();
        else if (anterior)
            PreviousBook();
        else if (confirmar)
            StartCoroutine(SecuenciaMoverALaMesa());
    }

    private void ProcesarControlesMesa()
    {
        bool confirmar = false;
        bool volver = false;

        if (Keyboard.current != null)
        {
            Keyboard teclado = Keyboard.current;

            confirmar =
                teclado[teclaConfirmarYAbir].wasPressedThisFrame;

            volver =
                teclado[teclaVolverOCerrar].wasPressedThisFrame;
        }

        if (Gamepad.current != null)
        {
            Gamepad mando = Gamepad.current;

            confirmar =
                mando.buttonSouth.wasPressedThisFrame ||
                confirmar;

            volver =
                mando.buttonEast.wasPressedThisFrame ||
                volver;
        }

        if (confirmar && !libroAbierto)
        {
            StartCoroutine(SecuenciaAbrirLibro());
        }
        else if (volver)
        {
            if (libroAbierto)
                StartCoroutine(SecuenciaCerrarYRegresarABalda());
            else
                StartCoroutine(SecuenciaRegresarABaldaDirecto());
        }
    }

    

    private void CacheReferences()
    {
        int count = bookObjectsInScene.Length;

        bookControllers = new BookItem3D[count];
        bookAnimators = new Animator[count];
        initialShelfPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            GameObject libro = bookObjectsInScene[i];

            if (libro == null)
                continue;

            initialShelfPositions[i] = libro.transform.position;
            bookControllers[i] = libro.GetComponent<BookItem3D>();
            bookAnimators[i] = libro.GetComponentInChildren<Animator>();

            if (bookControllers[i] != null)
                bookControllers[i].MostrarTextosPagina(false);
        }
    }

    private void ForcedInstantPosition()
    {
        if (bookObjectsInScene == null)
            return;

        for (int i = 0; i < bookObjectsInScene.Length; i++)
        {
            GameObject libro = bookObjectsInScene[i];

            if (libro == null)
                continue;

            bool esActual = i == currentIndex && !enMesa;

            Vector3 posicion = esActual
                ? initialShelfPositions[i] +
                  new Vector3(0f, shelfHoverHeight, 0f)
                : initialShelfPositions[i];

            Vector3 escala = esActual
                ? activeCenterScale
                : shelfScale;

            if (bookControllers[i] != null)
            {
                bookControllers[i].SetIsActive(
                    esActual, activeCenterScale, shelfHoverHeight);

                bookControllers[i].SetTargetTransform(
                    posicion, transitionSpeed, escala);
            }

            libro.transform.position = posicion;
            libro.transform.localScale = escala;
        }
    }

    private void UpdateShelfPositions()
    {
        if (enMesa || bookObjectsInScene == null)
            return;

        for (int i = 0; i < bookObjectsInScene.Length; i++)
        {
            if (bookObjectsInScene[i] == null)
                continue;

            bool esActual = i == currentIndex;

            Vector3 posicion = esActual
                ? initialShelfPositions[i] +
                  new Vector3(0f, shelfHoverHeight, 0f)
                : initialShelfPositions[i];

            Vector3 escala = esActual
                ? activeCenterScale
                : shelfScale;

            if (bookControllers[i] != null)
            {
                bookControllers[i].SetIsActive(
                    esActual, activeCenterScale, shelfHoverHeight);

                bookControllers[i].SetTargetTransform(
                    posicion, transitionSpeed, escala);
            }
        }
    }

   

    public void NextBook()
    {
        if (bookObjectsInScene == null ||
            bookObjectsInScene.Length == 0 ||
            enMesa || enTransicion)
            return;

        currentIndex =
            (currentIndex + 1) % bookObjectsInScene.Length;

        UpdateShelfPositions();
    }

    public void PreviousBook()
    {
        if (bookObjectsInScene == null ||
            bookObjectsInScene.Length == 0 ||
            enMesa || enTransicion)
            return;

        currentIndex =
            (currentIndex - 1 + bookObjectsInScene.Length) %
            bookObjectsInScene.Length;

        UpdateShelfPositions();
    }

    

    private IEnumerator SecuenciaMoverALaMesa()
    {
        if (spawnPointMesa == null || !HayLibroActual())
            yield break;

        enTransicion = true;
        enMesa = true;
        libroAbierto = false;

        if (bookControllers[currentIndex] != null)
        {
            bookControllers[currentIndex].SetTargetTransform(
                spawnPointMesa.position,
                transitionSpeed,
                activeCenterScale * 1.3f);
        }

        yield return new WaitForSeconds(0.4f);
        enTransicion = false;
    }

    private IEnumerator SecuenciaAbrirLibro()
    {
        if (!HayLibroActual() || spawnPointMesa == null)
            yield break;

        enTransicion = true;

        bookObjectsInScene[currentIndex].transform.position =
            spawnPointMesa.position;

        if (bookAnimators[currentIndex] != null)
            bookAnimators[currentIndex].SetTrigger("Abrir");

        yield return new WaitForSeconds(0.3f);

        libroAbierto = true;
        enTransicion = false;

        if (bookControllers[currentIndex] != null)
            bookControllers[currentIndex].MostrarTextosPagina(true);
    }

    private IEnumerator SecuenciaCerrarYRegresarABalda()
    {
        if (!HayLibroActual())
            yield break;

        enTransicion = true;
        libroAbierto = false;

        if (bookControllers[currentIndex] != null)
            bookControllers[currentIndex].MostrarTextosPagina(false);

        if (bookAnimators[currentIndex] != null)
            bookAnimators[currentIndex].SetTrigger("Cerrar");

        yield return new WaitForSeconds(0.5f);

        if (bookControllers[currentIndex] != null)
        {
            bookControllers[currentIndex].SetTargetTransform(
                initialShelfPositions[currentIndex],
                transitionSpeed,
                shelfScale);
        }

        yield return new WaitForSeconds(0.6f);

        enMesa = false;
        enTransicion = false;
        UpdateShelfPositions();
    }

    private IEnumerator SecuenciaRegresarABaldaDirecto()
    {
        if (!HayLibroActual())
            yield break;

        enTransicion = true;
        libroAbierto = false;

        if (bookControllers[currentIndex] != null)
        {
            bookControllers[currentIndex].MostrarTextosPagina(false);

            bookControllers[currentIndex].SetTargetTransform(
                initialShelfPositions[currentIndex],
                transitionSpeed,
                shelfScale);
        }

        yield return new WaitForSeconds(0.6f);

        enMesa = false;
        enTransicion = false;
        UpdateShelfPositions();
    }

    private bool HayLibroActual()
    {
        return bookObjectsInScene != null &&
               currentIndex >= 0 &&
               currentIndex < bookObjectsInScene.Length &&
               bookObjectsInScene[currentIndex] != null;
    }
}
