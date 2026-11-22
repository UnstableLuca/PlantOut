using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class BookShelfManager : MonoBehaviour
{
    [Header("Libros")]
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

    [Header("Cámaras")]
    public CinemachineCamera vcamLibros;
    public float tiempoBlendCamara = 1.5f;
    public InteractableObject interactableReference;

    [Header("Canvas de Salida")]
    public GameObject canvasSalirEstanteria;
    [Tooltip("Desmarcado: no se puede abrir ningún libro (todos salen con candado)")]
    public bool permitirAbrirLibros = false;
    [Header("Libros bloqueados")]
    [Tooltip("UI de candado que se enseña al señalar en la balda el libro de una planta bloqueada (opcional)")]
    public GameObject candadoLibroUI;

    [Header("Animator Triggers")]
    public string triggerAbrir = "Abrir";
    public string triggerCerrar = "Cerrar";
    public string triggerPaginaAdelante = "PasarPaginaAdelante";
    public string triggerPaginaAtras = "PasarPaginaAtras";

    [Header("Animator Tiempos")]
    public int capaAnimator = 0;
    public float esperaTrasAbrir = 0.3f;
    public float esperaTrasCerrar = 0.5f;

    public float duracionFadeUI = 0.15f;
    public float tiempoMaxInicioAnimacion = 0.3f;
    [Range(0.3f, 1f)] public float fraccionAnimacionParaMostrarUI = 0.85f;
    public float duracionMaximaPagina = 2f;

    private BookItem3D[] bookControllers;
    private Animator[][] bookAnimators;
    private Vector3[] initialShelfPositions;

    private readonly Dictionary<Animator, HashSet<string>> triggersPorAnimator = new Dictionary<Animator, HashSet<string>>();
    private int hashEstadoPrevioReferencia;
    private bool ultimaAnimacionReacciono = true;

    private int currentIndex;
    private bool enMesa;
    private bool libroAbierto;
    private bool enTransicion;
    private bool enTransicionPagina = false;
    private bool sistemaIniciado = false;

    private bool estadoMandoAnterior;

    public bool UsarMandoActual => forzarModoMando || Gamepad.current != null;

    private void Start()
    {
        CacheReferences();
        ValidarTriggersDelAnimator();

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
        ActualizarCandadoLibro();

        if (!sistemaIniciado) return;

        bool mandoActualConectado = UsarMandoActual;

        if (mandoActualConectado != estadoMandoAnterior)
        {
            estadoMandoAnterior = mandoActualConectado;
            ActualizarModoControl();
        }

        if (enTransicion || bookObjectsInScene == null || bookObjectsInScene.Length == 0)
            return;

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
        if (enTransicion || !sistemaIniciado) return;
        StartCoroutine(SecuenciaSalirDeEstanteriaLibros());
    }

    private IEnumerator SecuenciaSalirDeEstanteriaLibros()
    {
        enTransicion = true;

        if (canvasSalirEstanteria != null)
            canvasSalirEstanteria.SetActive(false);

        while (enTransicionPagina) yield return null;

        if (enMesa)
        {
            if (bookControllers[currentIndex] != null)
                bookControllers[currentIndex].MostrarTextosPagina(false);

            DispararAnimacion(currentIndex, triggerCerrar);

            yield return new WaitForSeconds(esperaTrasCerrar);
        }

        if (bookObjectsInScene != null)
        {
            for (int i = 0; i < bookObjectsInScene.Length; i++)
            {
                if (bookObjectsInScene[i] == null) continue;

                if (bookControllers[i] != null)
                    bookControllers[i].MostrarTextosPagina(false);
            }
        }

        yield return new WaitForSeconds(0.6f);

        if (vcamLibros != null) vcamLibros.Priority = 0;
        yield return new WaitForSeconds(tiempoBlendCamara);

        enMesa = false;
        libroAbierto = false;
        enTransicionPagina = false;
        currentIndex = 0;
        sistemaIniciado = false;
        enTransicion = false;

        ForcedInstantPosition();

        if (interactableReference != null)
            interactableReference.NotifyInteractionEnded();
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
                libro.ActualizarIconosControl(UsarMandoActual);
        }
    }

    private void ProcesarControlesBalda()
    {
        bool siguiente = false;
        bool anterior = false;
        bool arriba = false;
        bool abajo = false;
        bool confirmar = false;
        bool volver = false;

        if (Keyboard.current != null)
        {
            Keyboard teclado = Keyboard.current;
            siguiente = teclado.dKey.wasPressedThisFrame || teclado.rightArrowKey.wasPressedThisFrame;
            anterior = teclado.aKey.wasPressedThisFrame || teclado.leftArrowKey.wasPressedThisFrame;
            arriba = teclado.wKey.wasPressedThisFrame || teclado.upArrowKey.wasPressedThisFrame;
            abajo = teclado.sKey.wasPressedThisFrame || teclado.downArrowKey.wasPressedThisFrame;
            confirmar = teclado[teclaConfirmarYAbir].wasPressedThisFrame;
            volver = teclado[teclaVolverOCerrar].wasPressedThisFrame;
        }

        if (Gamepad.current != null)
        {
            Gamepad mando = Gamepad.current;
            siguiente = mando.dpad.right.wasPressedThisFrame || siguiente;
            anterior = mando.dpad.left.wasPressedThisFrame || anterior;
            arriba = mando.dpad.up.wasPressedThisFrame || arriba;
            abajo = mando.dpad.down.wasPressedThisFrame || abajo;
            confirmar = mando.buttonSouth.wasPressedThisFrame || confirmar;
            volver = mando.buttonEast.wasPressedThisFrame || volver;
        }

        if (siguiente)
            NextBook();
        else if (anterior)
            PreviousBook();
        else if (arriba)
            NavegarVertical(1);
        else if (abajo)
            NavegarVertical(-1);
        else if (confirmar)
            StartCoroutine(SecuenciaMoverYAbrirEnMesa());
        else if (volver)
            SalirDeLaEstanteriaLibros();
    }

    private void ProcesarControlesMesa()
    {
        if (enTransicionPagina) return;

        bool volver = false;

        if (Keyboard.current != null)
            volver = Keyboard.current[teclaVolverOCerrar].wasPressedThisFrame;

        if (Gamepad.current != null)
        {
            Gamepad mando = Gamepad.current;

            if (libroAbierto)
            {
                if (mando.rightShoulder.wasPressedThisFrame) IntentarAvanzarPaginaDesdeUI();
                else if (mando.leftShoulder.wasPressedThisFrame) IntentarRetrocederPaginaDesdeUI();
            }

            volver = mando.buttonEast.wasPressedThisFrame || volver;
        }

        if (volver && !enTransicionPagina)
        {
            if (libroAbierto)
                StartCoroutine(SecuenciaCerrarYRegresarABalda());
            else
                StartCoroutine(SecuenciaRegresarABaldaDirecto());
        }
    }

    

    public void IntentarAvanzarPaginaDesdeUI()
    {
        if (!PuedeIniciarCambioPagina(1)) return;
        StartCoroutine(RutinaCambioPagina(1, triggerPaginaAdelante));
    }

    public void IntentarRetrocederPaginaDesdeUI()
    {
        if (!PuedeIniciarCambioPagina(-1)) return;
        StartCoroutine(RutinaCambioPagina(-1, triggerPaginaAtras));
    }

    private bool PuedeIniciarCambioPagina(int direccion)
    {
        if (!enMesa || !libroAbierto || enTransicion || enTransicionPagina) return false;
        if (!HayLibroActual()) return false;

        BookItem3D libro = bookControllers[currentIndex];
        return libro != null && libro.PuedeCambiarPagina(direccion); // límites: primera / última página
    }

    
    private IEnumerator RutinaCambioPagina(int direccion, string triggerAnim)
    {
        enTransicionPagina = true; 

        try
        {
            BookItem3D libro = bookControllers[currentIndex];

            yield return libro.OcultarUIAnimado(duracionFadeUI);

            Animator referencia = DispararAnimacion(currentIndex, triggerAnim);
            yield return EsperarFinAnimacion(referencia, triggerAnim);

          
            if (!ultimaAnimacionReacciono)
            {
                LimpiarTriggersPagina(currentIndex);
                yield return libro.MostrarUIAnimado(duracionFadeUI);
                yield break;
            }

            libro.CambiarPagina(direccion);

            yield return libro.MostrarUIAnimado(duracionFadeUI);
        }
        finally
        {
            enTransicionPagina = false;
        }
    }

    private string[] TodosLosTriggers()
    {
        return new[] { triggerAbrir, triggerCerrar, triggerPaginaAdelante, triggerPaginaAtras };
    }

    private HashSet<string> ObtenerTriggers(Animator anim)
    {
        if (anim == null) return null;

        if (!triggersPorAnimator.TryGetValue(anim, out HashSet<string> set))
        {
            set = new HashSet<string>();
            if (anim.runtimeAnimatorController != null)
            {
                foreach (AnimatorControllerParameter p in anim.parameters)
                {
                    if (p.type == AnimatorControllerParameterType.Trigger)
                        set.Add(p.name);
                }
            }
            triggersPorAnimator[anim] = set;
        }

        return set;
    }

  
    private Animator DispararAnimacion(int indice, string trigger)
    {
        if (string.IsNullOrEmpty(trigger) || bookAnimators == null || indice < 0 || indice >= bookAnimators.Length) return null;
        Animator[] animators = bookAnimators[indice];
        if (animators == null) return null;

        Animator referencia = null;
        string[] todos = TodosLosTriggers();

        foreach (Animator anim in animators)
        {
            if (anim == null || !anim.isActiveAndEnabled) continue;

            HashSet<string> disponibles = ObtenerTriggers(anim);
            if (disponibles == null || !disponibles.Contains(trigger)) continue;

            if (referencia == null)
            {
                referencia = anim;
                hashEstadoPrevioReferencia = anim.GetCurrentAnimatorStateInfo(capaAnimator).fullPathHash;
            }

            foreach (string t in todos)
            {
                if (!string.IsNullOrEmpty(t) && disponibles.Contains(t)) anim.ResetTrigger(t);
            }

            anim.SetTrigger(trigger);
        }

        

        return referencia;
    }

    
    private IEnumerator EsperarFinAnimacion(Animator anim, string trigger)
    {
        ultimaAnimacionReacciono = true;
        if (anim == null) yield break;

        yield return null;

        float t = 0f;

        while (!anim.IsInTransition(capaAnimator) && t < tiempoMaxInicioAnimacion)
        {
            t += Time.deltaTime;
            yield return null;
        }

        t = 0f;
        while (anim.IsInTransition(capaAnimator) && t < duracionMaximaPagina)
        {
            t += Time.deltaTime;
            yield return null;
        }

        int hashAnimacion = anim.GetCurrentAnimatorStateInfo(capaAnimator).fullPathHash;
        float tEnEstado = 0f;
        t = 0f;

        while (t < duracionMaximaPagina)
        {
            AnimatorStateInfo s = anim.GetCurrentAnimatorStateInfo(capaAnimator);

            if (s.fullPathHash != hashAnimacion || anim.IsInTransition(capaAnimator))
                break;

            bool terminado = s.speed < 0f
                ? (s.normalizedTime <= (1f - fraccionAnimacionParaMostrarUI) || tEnEstado > 0.5f)
                : s.normalizedTime >= fraccionAnimacionParaMostrarUI;

            if (terminado) break;

            t += Time.deltaTime;
            tEnEstado += Time.deltaTime;
            yield return null;
        }
    }

    private void LimpiarTriggersPagina(int indice)
    {
        if (bookAnimators == null || indice < 0 || indice >= bookAnimators.Length || bookAnimators[indice] == null) return;

        foreach (Animator anim in bookAnimators[indice])
        {
            if (anim == null) continue;
            HashSet<string> disponibles = ObtenerTriggers(anim);
            if (disponibles == null) continue;

            if (disponibles.Contains(triggerPaginaAdelante)) anim.ResetTrigger(triggerPaginaAdelante);
            if (disponibles.Contains(triggerPaginaAtras)) anim.ResetTrigger(triggerPaginaAtras);
        }
    }

    private void ValidarTriggersDelAnimator()
    {
        if (bookAnimators == null) return;

        for (int i = 0; i < bookAnimators.Length; i++)
        {
            if (bookAnimators[i] == null || bookAnimators[i].Length == 0)
            { 
                continue;
            }

            foreach (Animator anim in bookAnimators[i])
            {
                HashSet<string> disponibles = ObtenerTriggers(anim);
                if (disponibles == null) continue;

                
            }
        }
    }

   

    private void CacheReferences()
    {
        int count = bookObjectsInScene != null ? bookObjectsInScene.Length : 0;

        bookControllers = new BookItem3D[count];
        bookAnimators = new Animator[count][];
        initialShelfPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            GameObject slotObj = bookObjectsInScene[i];
            if (slotObj == null) continue;

            initialShelfPositions[i] = slotObj.transform.position;

            bookControllers[i] = slotObj.GetComponentInChildren<BookItem3D>(true);
            bookAnimators[i] = slotObj.GetComponentsInChildren<Animator>(true);

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

    private bool SlotValido(int i)
    {
        return bookObjectsInScene != null && i >= 0 && i < bookObjectsInScene.Length &&
               bookObjectsInScene[i] != null && bookObjectsInScene[i].activeInHierarchy;
    }

    private int BuscarSlotSecuencial(int paso)
    {
        int n = bookObjectsInScene.Length;
        int idx = currentIndex;

        for (int k = 0; k < n; k++)
        {
            idx = (idx + paso + n) % n;
            if (SlotValido(idx)) return idx;
        }
        return currentIndex;
    }

    public void NextBook()
    {
        if (bookObjectsInScene == null || bookObjectsInScene.Length == 0 || enMesa || enTransicion) return;
        currentIndex = BuscarSlotSecuencial(1);
        UpdateShelfPositions();
    }

    public void PreviousBook()
    {
        if (bookObjectsInScene == null || bookObjectsInScene.Length == 0 || enMesa || enTransicion) return;
        currentIndex = BuscarSlotSecuencial(-1);
        UpdateShelfPositions();
    }

    public void NavegarVertical(int signo)
    {
        if (bookObjectsInScene == null || bookObjectsInScene.Length == 0 || enMesa || enTransicion) return;
        if (!SlotValido(currentIndex)) { currentIndex = BuscarSlotSecuencial(1); return; }

        Vector3 origen = initialShelfPositions[currentIndex];
        int mejor = -1;
        float mejorPuntuacion = float.MaxValue;

        for (int i = 0; i < bookObjectsInScene.Length; i++)
        {
            if (i == currentIndex || !SlotValido(i)) continue;

            Vector3 d = initialShelfPositions[i] - origen;
            float dy = d.y * signo;
            if (dy < 0.05f) continue; 

            float horizontal = new Vector3(d.x, 0f, d.z).magnitude;
            float puntuacion = dy + horizontal;

            if (puntuacion < mejorPuntuacion)
            {
                mejorPuntuacion = puntuacion;
                mejor = i;
            }
        }

        if (mejor >= 0)
        {
            currentIndex = mejor;
            UpdateShelfPositions();
        }
    }


    private IEnumerator SecuenciaMoverYAbrirEnMesa()
    {
        if (spawnPointMesa == null || !HayLibroActual() || LibroBloqueado(currentIndex)) yield break;

        enTransicion = true;
        enMesa = true;
        libroAbierto = false;
        enTransicionPagina = false;

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

        DispararAnimacion(currentIndex, triggerAbrir);

        yield return new WaitForSeconds(esperaTrasAbrir);

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

        while (enTransicionPagina) yield return null;

        if (bookControllers[currentIndex] != null)
            bookControllers[currentIndex].MostrarTextosPagina(false);

        DispararAnimacion(currentIndex, triggerCerrar);

        yield return new WaitForSeconds(esperaTrasCerrar);

        GameObject slotObj = bookObjectsInScene[currentIndex];

        float tiempoTranscurrido = 0f;
        Vector3 posInicial = slotObj.transform.position;
        Vector3 escalaInicial = slotObj.transform.localScale;

        while (tiempoTranscurrido < .6f)
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
        enTransicionPagina = false;

        if (bookControllers[currentIndex] != null)
            bookControllers[currentIndex].MostrarTextosPagina(false);

        GameObject slotObj = bookObjectsInScene[currentIndex];
        slotObj.transform.position = initialShelfPositions[currentIndex];
        slotObj.transform.localScale = shelfScale;

        yield return new WaitForSeconds(0.6f);

        enMesa = false;
        enTransicion = false;
        UpdateShelfPositions();
    }
    private bool LibroBloqueado(int indice)
    {
        if (!permitirAbrirLibros) return true;

        return bookControllers != null && indice >= 0 && indice < bookControllers.Length &&
               bookControllers[indice] != null && bookControllers[indice].Bloqueado;
    }

    private void ActualizarCandadoLibro()
    {
        if (candadoLibroUI == null) return;

        bool mostrar = sistemaIniciado && !enMesa && LibroBloqueado(currentIndex);
        if (candadoLibroUI.activeSelf != mostrar) candadoLibroUI.SetActive(mostrar);
    }

    private bool HayLibroActual()
    {
        return bookObjectsInScene != null &&
               bookControllers != null &&
               currentIndex >= 0 &&
               currentIndex < bookObjectsInScene.Length &&
               bookObjectsInScene[currentIndex] != null;
    }
}
