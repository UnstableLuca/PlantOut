using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Video;

[System.Serializable]
public struct PaginaLibro
{
    [Header("Cara Izquierda")]
    public string tituloCaraIzquierda;
    [TextArea(2, 4)] public string descripcionCaraIzquierda;

    [Header("Soporte de Vídeo - Cara Izquierda")]
    public bool mostrarVideoIzquierda;
    public VideoClip clipVideoIzquierda;

    [Header("Cara Derecha")]
    public string tituloCaraDerecha;
    [TextArea(2, 4)] public string descripcionCaraDerecha;

    [Header("Soporte de Vídeo - Cara Derecha")]
    public bool mostrarVideoDerecha;
    public VideoClip clipVideoDerecha;
}

public class BookItem3D : MonoBehaviour
{
    private Renderer[] bookRenderers;

    [Header("Configuración de Páginas (3 Páginas = 6 Caras)")]
    public PaginaLibro[] paginas = new PaginaLibro[3];
    private int paginaActual = 0;

    [Header("UI - Izquierda (Cara 1, 3, 5)")]
    public TextMeshProUGUI textoNombreIzquierda;
    public TextMeshProUGUI textoDescripcionIzquierda;
    public VideoPlayer videoPlayerIzquierda;
    public GameObject rawImageVideoIzquierda;
    public Button botonRetrocederPaginaIzquierda;

    [Header("UI - Derecha (Cara 2, 4, 6)")]
    public TextMeshProUGUI textoNombreDerecha;
    public TextMeshProUGUI textoDescripcionDerecha;
    public VideoPlayer videoPlayerDerecha;
    public GameObject rawImageVideoDerecha;
    public Button botonPasarPaginaDerecha;

    [Header("Referencias Globales")]
    public GameObject panelInfoPaginas;
    public GameObject iconoBotonPrefab;
    public bool forzarModoMando = false;

    [Header("Transición de UI")]
    public CanvasGroup grupoCanvasInfo;
    public bool reiniciarPaginaAlAbrir = true;

    private bool modoMandoActual = false;
    private bool modoControlRecibidoDelManager = false;
    private int tokenFade = 0;

    [Header("Planta del libro")]
    [Tooltip("Planta a la que pertenece este libro. Si esa planta no se puede seleccionar, el libro no se puede abrir. Vacío = siempre se puede leer")]
    public PlantData3D planta;

    public bool Bloqueado => planta != null && !planta.EsSeleccionable;

    private BookShelfManager shelfManagerRef;

    public int PaginaActual => paginaActual;
    public int TotalPaginas => paginas != null ? paginas.Length : 0;

    void Awake()
    {
        bookRenderers = GetComponentsInChildren<Renderer>();

        ResolverCanvasGroup();

        if (panelInfoPaginas != null)
            panelInfoPaginas.SetActive(false);

        shelfManagerRef = Object.FindAnyObjectByType<BookShelfManager>();

        if (botonPasarPaginaDerecha != null)
            botonPasarPaginaDerecha.onClick.AddListener(OnBotonPasarPaginaClicked);

        if (botonRetrocederPaginaIzquierda != null)
            botonRetrocederPaginaIzquierda.onClick.AddListener(OnBotonRetrocederPaginaClicked);
    }

    void OnDestroy()
    {
        if (botonPasarPaginaDerecha != null)
            botonPasarPaginaDerecha.onClick.RemoveListener(OnBotonPasarPaginaClicked);

        if (botonRetrocederPaginaIzquierda != null)
            botonRetrocederPaginaIzquierda.onClick.RemoveListener(OnBotonRetrocederPaginaClicked);
    }

    void Start()
    {
        if (paginas != null && paginas.Length > 0)
        {
            paginaActual = 0;
            ConfigurarContenidoPagina();
        }
    }

    private void ResolverCanvasGroup()
    {
        if (grupoCanvasInfo != null) return;

        GameObject raiz = panelInfoPaginas;

        if (raiz == null && textoNombreIzquierda != null && textoNombreIzquierda.transform.parent != null)
            raiz = textoNombreIzquierda.transform.parent.gameObject;

        if (raiz == null) return;

        grupoCanvasInfo = raiz.GetComponent<CanvasGroup>();
        if (grupoCanvasInfo == null)
            grupoCanvasInfo = raiz.AddComponent<CanvasGroup>();
    }

    public void SetupBookTextures(Texture2D coverTex, Texture2D spineTex, Texture2D pagesTex)
    {
        if (bookRenderers == null || bookRenderers.Length == 0)
            bookRenderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in bookRenderers)
        {
            if (rend == null) continue;

            Material[] mats = rend.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;

                Material uniqueMat = new Material(mats[i]);
                string matName = mats[i].name.ToLower();

                if ((matName.Contains("cover") || matName.Contains("cubierta")) && coverTex != null)
                {
                    uniqueMat.mainTexture = coverTex;
                    uniqueMat.SetTexture("_BaseMap", coverTex);
                }
                else if ((matName.Contains("binding") || matName.Contains("lomo")) && spineTex != null)
                {
                    uniqueMat.mainTexture = spineTex;
                    uniqueMat.SetTexture("_BaseMap", spineTex);
                }
                else if ((matName.Contains("page") || matName.Contains("pages") || matName.Contains("interior")) && pagesTex != null)
                {
                    uniqueMat.mainTexture = pagesTex;
                    uniqueMat.SetTexture("_BaseMap", pagesTex);
                }

                mats[i] = uniqueMat;
            }

            rend.materials = mats;
        }
    }

  

    public void ConfigurarContenidoPagina()
    {
        if (paginas == null || paginas.Length == 0) return;

        paginaActual = Mathf.Clamp(paginaActual, 0, paginas.Length - 1);
        PaginaLibro pData = paginas[paginaActual];

        if (textoNombreIzquierda != null) textoNombreIzquierda.text = pData.tituloCaraIzquierda;
        if (textoDescripcionIzquierda != null) textoDescripcionIzquierda.text = pData.descripcionCaraIzquierda;

        if (videoPlayerIzquierda != null)
        {
            if (pData.mostrarVideoIzquierda && pData.clipVideoIzquierda != null)
            {
                if (rawImageVideoIzquierda != null) rawImageVideoIzquierda.SetActive(true);
                videoPlayerIzquierda.clip = pData.clipVideoIzquierda;
                videoPlayerIzquierda.Play();
            }
            else
            {
                videoPlayerIzquierda.Stop();
                if (rawImageVideoIzquierda != null) rawImageVideoIzquierda.SetActive(false);
            }
        }

        if (textoNombreDerecha != null) textoNombreDerecha.text = pData.tituloCaraDerecha;
        if (textoDescripcionDerecha != null) textoDescripcionDerecha.text = pData.descripcionCaraDerecha;

        if (videoPlayerDerecha != null)
        {
            if (pData.mostrarVideoDerecha && pData.clipVideoDerecha != null)
            {
                if (rawImageVideoDerecha != null) rawImageVideoDerecha.SetActive(true);
                videoPlayerDerecha.clip = pData.clipVideoDerecha;
                videoPlayerDerecha.Play();
            }
            else
            {
                videoPlayerDerecha.Stop();
                if (rawImageVideoDerecha != null) rawImageVideoDerecha.SetActive(false);
            }
        }

        if (!modoControlRecibidoDelManager)
            modoMandoActual = Gamepad.current != null || forzarModoMando;


        ActualizarVisibilidadBotonesPagina();
    }

    private void ActualizarVisibilidadBotonesPagina()
    {
        if (botonPasarPaginaDerecha != null)
            botonPasarPaginaDerecha.gameObject.SetActive(paginaActual < TotalPaginas - 1);

        if (botonRetrocederPaginaIzquierda != null)
            botonRetrocederPaginaIzquierda.gameObject.SetActive(paginaActual > 0);
    }

   

    public bool PuedeCambiarPagina(int direccion)
    {
        if (paginas == null || paginas.Length <= 1) return false;
        int nueva = paginaActual + direccion;
        return nueva >= 0 && nueva < paginas.Length;
    }

    public bool CambiarPagina(int direccion)
    {
        if (!PuedeCambiarPagina(direccion)) return false;

        paginaActual += direccion;
        ConfigurarContenidoPagina();
        return true;
    }

    private void OnBotonPasarPaginaClicked()
    {
        if (shelfManagerRef == null) shelfManagerRef = Object.FindAnyObjectByType<BookShelfManager>();
        if (shelfManagerRef != null) shelfManagerRef.IntentarAvanzarPaginaDesdeUI();
    }

    private void OnBotonRetrocederPaginaClicked()
    {
        if (shelfManagerRef == null) shelfManagerRef = Object.FindAnyObjectByType<BookShelfManager>();
        if (shelfManagerRef != null) shelfManagerRef.IntentarRetrocederPaginaDesdeUI();
    }

   

    private void FijarAlpha(float alpha)
    {
        if (grupoCanvasInfo == null) return;
        grupoCanvasInfo.alpha = alpha;
        bool visible = alpha >= 1f;
        grupoCanvasInfo.interactable = visible;
        grupoCanvasInfo.blocksRaycasts = visible;
    }

    private IEnumerator FadeUI(float objetivo, float duracion)
    {
        if (grupoCanvasInfo == null) yield break;

        int miToken = ++tokenFade; 

        grupoCanvasInfo.interactable = false;
        grupoCanvasInfo.blocksRaycasts = false;

        float inicio = grupoCanvasInfo.alpha;
        float t = 0f;

        while (t < duracion)
        {
            if (miToken != tokenFade) yield break;
            t += Time.deltaTime;
            grupoCanvasInfo.alpha = Mathf.Lerp(inicio, objetivo, t / duracion);
            yield return null;
        }

        if (miToken != tokenFade) yield break;
        FijarAlpha(objetivo);
    }

    public IEnumerator OcultarUIAnimado(float duracion)
    {
        yield return FadeUI(0f, duracion);

        if (videoPlayerIzquierda != null) videoPlayerIzquierda.Stop();
        if (videoPlayerDerecha != null) videoPlayerDerecha.Stop();
    }

    public IEnumerator MostrarUIAnimado(float duracion)
    {
        yield return FadeUI(1f, duracion);
    }

   

    public void ActualizarIconosControl(bool usandoMando)
    {
        modoControlRecibidoDelManager = true;
        modoMandoActual = usandoMando;

        if (paginas != null && paginas.Length > 0)
        {
            PaginaLibro pData = paginas[Mathf.Clamp(paginaActual, 0, paginas.Length - 1)];
        }
    }

    private void GenerarIconosCombos(bool usandoMando, ComboVisual[] combosPagina, Transform contenedor)
    {
        if (contenedor == null) return;

        foreach (Transform child in contenedor)
            Destroy(child.gameObject);

        if (combosPagina == null || iconoBotonPrefab == null) return;

        foreach (ComboVisual combo in combosPagina)
        {
            if (combo == null) continue;

            Sprite[] sprites = usandoMando ? combo.botonesMando : combo.botonesTeclado;
            if (sprites == null) continue;

            foreach (Sprite sprite in sprites)
            {
                if (sprite == null) continue;

                GameObject icono = Instantiate(iconoBotonPrefab, contenedor);
                Image imagen = icono.GetComponent<Image>();

                if (imagen != null)
                {
                    imagen.sprite = sprite;
                    imagen.preserveAspect = true;
                }
                icono.transform.localScale = Vector3.one;
            }
        }
    }

    public void SetIsActive(bool active, Vector3 activeScale, float hoverHeight) { }
    public void SetTargetTransform(Vector3 pos, float speed, Vector3 targetScaleVal) { }

   
    public void MostrarTextosPagina(bool mostrar)
    {
        tokenFade++; 

        if (!mostrar)
        {
            if (videoPlayerIzquierda != null) videoPlayerIzquierda.Stop();
            if (videoPlayerDerecha != null) videoPlayerDerecha.Stop();
            FijarAlpha(0f);
            if (panelInfoPaginas != null) panelInfoPaginas.SetActive(false);
            return;
        }

        if (reiniciarPaginaAlAbrir) paginaActual = 0;

        if (panelInfoPaginas != null) panelInfoPaginas.SetActive(true);
        FijarAlpha(0f);
        ConfigurarContenidoPagina();

        if (grupoCanvasInfo != null && isActiveAndEnabled)
            StartCoroutine(FadeUI(1f, 0.2f));
        else
            FijarAlpha(1f);
    }
}
