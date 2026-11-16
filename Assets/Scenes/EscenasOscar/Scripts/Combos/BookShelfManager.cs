using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class BookShelfManager : MonoBehaviour
{
    [Header("Libros (Soporta 6 en escena - Arrastra los Slots)")]
    public GameObject[] bookObjectsInScene;

    [Header("Punto de Mesa")]
    public Transform spawnPointMesa;

    [Header("Escala")]
    public float transitionSpeed = 8f;
    public Vector3 shelfScale = Vector3.one;
    public Vector3 activeCenterScale = new Vector3(2.5f, 2.5f, 2.5f);

    [Header("Hover")]
    public float shelfHoverHeight = 0.15f;

    [Header("Controles generales")]
    public Key teclaConfirmarYAbir = Key.Enter;
    public Key teclaVolverOCerrar = Key.Escape;

    [Header("DEV MANDO")]
    public bool forzarModoMando = false;

    [Header("Cámaras e Interacción")]
    public CinemachineCamera vcamLibros;
    public float tiempoBlendCamara = 1.5f;
    public InteractableObject interactableReference;

    [Header("Canvas de Salida (UI)")]
    public GameObject canvasSalirEstanteria;

    private BookItem3D[] bookControllers;
    private Animator[] bookAnimators;
    private Vector3[] initialShelfPositions;

    private int currentIndex;
    private bool enMesa;
    private bool libroAbierto;
    private bool enTransicion;
    private bool sistemaIniciado = false;

    private bool estadoMandoAnterior;

    public bool UsarMandoActual => forzarModoMando || Gamepad.current != null;

    private void Start()
    {
        CacheReferences();

        currentIndex = 0;
        estadoMandoAnterior = UsarMandoActual;
        ActualizarModoControl();

        ForcedInstantPosition();

        if (vcamLibros != null) vcamLibros.Priority = 0;
        if (canvasSalirEstanteria != null) canvasSalirEstanteria.SetActive(false);

        sistemaIniciado = false;
    }

    private void Update()
    {
        if (!sistemaIniciado) return;

        bool mandoActualConectado = UsarMandoActual;

        if (mandoActualConectado != estadoMandoAnterior)
        {
            estadoMandoAnterior = mandoActualConectado;
            ActualizarModoControl();
        }

        if (enTransicion || bookObjectsInScene == null || bookObjectsInScene.Length == 0)
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

    public void IniciarSistemaLibros()
    {
        if (sistemaIniciado) return;
        sistemaIniciado = true;

        if (vcamLibros != null) vcamLibros.Priority = 20;
        if (canvasSalirEstanteria != null) canvasSalirEstanteria.SetActive(true);

        ForcedInstantPosition();
    }

    public void SalirDeLaEstanteriaLibros()
    {
        StartCoroutine(SecuenciaSalirDeEstanteriaLibros());
    }

    private IEnumerator SecuenciaSalirDeEstanteriaLibros()
    {
        enTransicion = true;

        if (canvasSalirEstanteria != null)
            canvasSalirEstanteria.SetActive(false);

        if (enMesa)
        {
            if (bookControllers[currentIndex] != null)
                bookControllers[currentIndex].MostrarTextosPagina(false);

            DispararAnimacion(currentIndex, "Cerrar");

            yield return new WaitForSeconds(0.5f);
        }

        if (bookObjectsInScene != null)
        {
            for (int i = 0; i < bookObjectsInScene.Length; i++)
            {
                if (bookObjectsInScene[i] == null) continue;

                if (bookControllers[i] != null)
                {
                    bookControllers[i].MostrarTextosPagina(false);
                }
            }
        }

        yield return new WaitForSeconds(0.6f);

        if (vcamLibros != null) vcamLibros.Priority = 0;
        yield return new WaitForSeconds(tiempoBlendCamara);

        enMesa = false;
        libroAbierto = false;
        currentIndex = 0;
        sistemaIniciado = false;
        enTransicion = false;

        ForcedInstantPosition();

        if (interactableReference != null)
        {
            interactableReference.NotifyInteractionEnded();
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
        if (bookControllers == null) return;

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
        bool volver = false;

        if (Keyboard.current != null)
        {
            Keyboard teclado = Keyboard.current;
            siguiente = teclado.rightArrowKey.wasPressedThisFrame || teclado.dKey.wasPressedThisFrame;
            anterior = teclado.leftArrowKey.wasPressedThisFrame || teclado.aKey.wasPressedThisFrame;
            confirmar = teclado[teclaConfirmarYAbir].wasPressedThisFrame;
            volver = teclado[teclaVolverOCerrar].wasPressedThisFrame;
        }

        if (Gamepad.current != null)
        {
            Gamepad mando = Gamepad.current;
            siguiente = mando.dpad.right.wasPressedThisFrame || mando.dpad.down.wasPressedThisFrame || siguiente;
            anterior = mando.dpad.left.wasPressedThisFrame || mando.dpad.up.wasPressedThisFrame || anterior;
            confirmar = mando.buttonSouth.wasPressedThisFrame || confirmar;
            volver = mando.buttonEast.wasPressedThisFrame || volver;
        }

        if (siguiente)
            NextBook();
        else if (anterior)
            PreviousBook();
        else if (confirmar)
            StartCoroutine(SecuenciaMoverYAbrirEnMesa());
        else if (volver)
            SalirDeLaEstanteriaLibros();
    }

    private void ProcesarControlesMesa()
    {
        bool siguientePagina = false;
        bool paginaAnterior = false;
        bool volver = false;

        if (Keyboard.current != null)
        {
            Keyboard teclado = Keyboard.current;
            siguientePagina = teclado.rightArrowKey.wasPressedThisFrame || teclado.dKey.wasPressedThisFrame;
            paginaAnterior = teclado.leftArrowKey.wasPressedThisFrame || teclado.aKey.wasPressedThisFrame;
            volver = teclado[teclaVolverOCerrar].wasPressedThisFrame;
        }

        if (Gamepad.current != null)
        {
            Gamepad mando = Gamepad.current;
            siguientePagina = mando.dpad.right.wasPressedThisFrame || mando.rightShoulder.wasPressedThisFrame || siguientePagina;
            paginaAnterior = mando.dpad.left.wasPressedThisFrame || mando.leftShoulder.wasPressedThisFrame || paginaAnterior;
            volver = mando.buttonEast.wasPressedThisFrame || volver;
        }

        if (siguientePagina && libroAbierto)
        {
            IntentarAvanzarPaginaDesdeUI();
        }
        else if (paginaAnterior && libroAbierto)
        {
            bool cambioExitoso = bookControllers[currentIndex].CambiarPagina(-1);
            if (cambioExitoso) DispararAnimacion(currentIndex, "PasarPaginaAtras");
        }
        else if (volver)
        {
            if (libroAbierto)
                StartCoroutine(SecuenciaCerrarYRegresarABalda());
            else
                StartCoroutine(SecuenciaRegresarABaldaDirecto());
        }
    }

    public void IntentarAvanzarPaginaDesdeUI()
    {
        if (!enMesa || !libroAbierto) return;

        bool cambioExitoso = bookControllers[currentIndex].CambiarPagina(1);
        if (cambioExitoso) DispararAnimacion(currentIndex, "PasarPaginaAdelante");
    }

  
    private static readonly string[] triggersLibro = { "Abrir", "Cerrar", "PasarPaginaAdelante", "PasarPaginaAtras" };

    private void DispararAnimacion(int indice, string trigger)
    {
        if (bookAnimators == null || indice < 0 || indice >= bookAnimators.Length) return;
        Animator anim = bookAnimators[indice];
        if (anim == null) return;

        foreach (string t in triggersLibro) anim.ResetTrigger(t);
        anim.SetTrigger(trigger);
    }

    private void CacheReferences()
    {
        int count = bookObjectsInScene.Length;

        bookControllers = new BookItem3D[count];
        bookAnimators = new Animator[count];
        initialShelfPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            GameObject slotObj = bookObjectsInScene[i];
            if (slotObj == null) continue;

            initialShelfPositions[i] = slotObj.transform.position;

            bookControllers[i] = slotObj.GetComponentInChildren<BookItem3D>(true);
            bookAnimators[i] = slotObj.GetComponentInChildren<Animator>(true);

            if (bookAnimators[i] == null)
                Debug.LogWarning($"[BookShelfManager] El slot '{slotObj.name}' no tiene Animator en sus hijos.", slotObj);
            if (bookControllers[i] == null)
                Debug.LogWarning($"[BookShelfManager] El slot '{slotObj.name}' no tiene BookItem3D en sus hijos.", slotObj);

            if (bookControllers[i] != null)
                bookControllers[i].MostrarTextosPagina(false);
        }
    }

    private void ForcedInstantPosition()
    {
        if (bookObjectsInScene == null) return;

        for (int i = 0; i < bookObjectsInScene.Length; i++)
        {
            GameObject slotObj = bookObjectsInScene[i];
            if (slotObj == null) continue;

            bool esActual = i == currentIndex && !enMesa && sistemaIniciado;

            Vector3 posicion = esActual ? initialShelfPositions[i] + new Vector3(0f, shelfHoverHeight, 0f) : initialShelfPositions[i];

            slotObj.transform.position = posicion;
            slotObj.transform.localScale = shelfScale;
        }
    }

    private void UpdateShelfPositions()
    {
        if (enMesa || bookObjectsInScene == null) return;

        for (int i = 0; i < bookObjectsInScene.Length; i++)
        {
            GameObject slotObj = bookObjectsInScene[i];
            if (slotObj == null) continue;

            bool esActual = i == currentIndex;

            Vector3 posicion = esActual ? initialShelfPositions[i] + new Vector3(0f, shelfHoverHeight, 0f) : initialShelfPositions[i];

            slotObj.transform.position = Vector3.Lerp(slotObj.transform.position, posicion, Time.deltaTime * transitionSpeed);
            slotObj.transform.localScale = Vector3.Lerp(slotObj.transform.localScale, shelfScale, Time.deltaTime * transitionSpeed);
        }
    }

    public void NextBook()
    {
        if (bookObjectsInScene == null || bookObjectsInScene.Length == 0 || enMesa || enTransicion) return;

        currentIndex = (currentIndex + 1) % bookObjectsInScene.Length;
        UpdateShelfPositions();
    }

    public void PreviousBook()
    {
        if (bookObjectsInScene == null || bookObjectsInScene.Length == 0 || enMesa || enTransicion) return;

        currentIndex = (currentIndex - 1 + bookObjectsInScene.Length) % bookObjectsInScene.Length;
        UpdateShelfPositions();
    }

    private IEnumerator SecuenciaMoverYAbrirEnMesa()
    {
        if (spawnPointMesa == null || !HayLibroActual()) yield break;

        enTransicion = true;
        enMesa = true;
        libroAbierto = false;

        GameObject slotObj = bookObjectsInScene[currentIndex];

        float tiempoTranscurrido = 0f;
        Vector3 posInicial = slotObj.transform.position;
        Vector3 escalaInicial = slotObj.transform.localScale;

        while (tiempoTranscurrido < 0.4f)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / 0.4f;

            slotObj.transform.position = Vector3.Lerp(posInicial, spawnPointMesa.position, t);
            slotObj.transform.localScale = Vector3.Lerp(escalaInicial, activeCenterScale, t);
            yield return null;
        }

        slotObj.transform.position = spawnPointMesa.position;
        slotObj.transform.localScale = activeCenterScale;

        DispararAnimacion(currentIndex, "Abrir");

        yield return new WaitForSeconds(0.3f);

        libroAbierto = true;
        enTransicion = false;

        if (bookControllers[currentIndex] != null)
            bookControllers[currentIndex].MostrarTextosPagina(true);
    }

    private IEnumerator SecuenciaCerrarYRegresarABalda()
    {
        if (!HayLibroActual()) yield break;

        enTransicion = true;
        libroAbierto = false;

        if (bookControllers[currentIndex] != null)
            bookControllers[currentIndex].MostrarTextosPagina(false);

        DispararAnimacion(currentIndex, "Cerrar");

        yield return new WaitForSeconds(0.5f);

        GameObject slotObj = bookObjectsInScene[currentIndex];

        float tiempoTranscurrido = 0f;
        Vector3 posInicial = slotObj.transform.position;
        Vector3 escalaInicial = slotObj.transform.localScale;

        while (tiempoTranscurrido < 0.6f)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / 0.6f;

            slotObj.transform.position = Vector3.Lerp(posInicial, initialShelfPositions[currentIndex], t);
            slotObj.transform.localScale = Vector3.Lerp(escalaInicial, shelfScale, t);
            yield return null;
        }

        slotObj.transform.position = initialShelfPositions[currentIndex];
        slotObj.transform.localScale = shelfScale;

        enMesa = false;
        enTransicion = false;
        UpdateShelfPositions();
    }

    private IEnumerator SecuenciaRegresarABaldaDirecto()
    {
        if (!HayLibroActual()) yield break;

        enTransicion = true;
        libroAbierto = false;

        if (bookControllers[currentIndex] != null)
        {
            bookControllers[currentIndex].MostrarTextosPagina(false);
        }

        GameObject slotObj = bookObjectsInScene[currentIndex];
        slotObj.transform.position = initialShelfPositions[currentIndex];
        slotObj.transform.localScale = shelfScale;

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