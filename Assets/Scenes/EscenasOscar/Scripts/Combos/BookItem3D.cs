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
    [TextArea(2, 3)] public string estadisticasCaraIzquierda;
    public ComboVisual[] combosCaraIzquierda;

    [Header("Soporte de Vídeo - Cara Izquierda")]
    public bool mostrarVideoIzquierda;
    public VideoClip clipVideoIzquierda;

    [Header("Cara Derecha")]
    public string tituloCaraDerecha;
    [TextArea(2, 4)] public string descripcionCaraDerecha;
    [TextArea(2, 3)] public string estadisticasCaraDerecha;
    public ComboVisual[] combosCaraDerecha;

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
    public TextMeshProUGUI textoEstadisticasIzquierda;
    public Transform contenedorCombosUIIzquierda;
    public VideoPlayer videoPlayerIzquierda;
    public GameObject rawImageVideoIzquierda;

    [Header("UI - Derecha (Cara 2, 4, 6)")]
    public TextMeshProUGUI textoNombreDerecha;
    public TextMeshProUGUI textoDescripcionDerecha;
    public TextMeshProUGUI textoEstadisticasDerecha;
    public Transform contenedorCombosUIDerecha;
    public VideoPlayer videoPlayerDerecha;
    public GameObject rawImageVideoDerecha;
    public Button botonPasarPaginaDerecha;

    [Header("Referencias Globales")]
    public GameObject panelInfoPaginas;
    public GameObject iconoBotonPrefab;
    public bool forzarModoMando = false;

    private BookShelfManager shelfManagerRef;

    private bool isActiveBook = false;
    private bool modoMandoActual = false;
    private bool modoControlRecibidoDelManager = false;

    void Awake()
    {
        bookRenderers = GetComponentsInChildren<Renderer>();

        if (panelInfoPaginas != null)
            panelInfoPaginas.SetActive(false);

        shelfManagerRef = Object.FindAnyObjectByType<BookShelfManager>();

        if (botonPasarPaginaDerecha != null)
        {
            botonPasarPaginaDerecha.onClick.AddListener(OnBotonPasarPaginaClicked);
        }
    }

    void Start()
    {
        if (paginas != null && paginas.Length > 0)
        {
            paginaActual = 0;
            ConfigurarContenidoPagina();
        }
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

        // --- Lado Izquierdo ---
        if (textoNombreIzquierda != null) textoNombreIzquierda.text = pData.tituloCaraIzquierda;
        if (textoDescripcionIzquierda != null) textoDescripcionIzquierda.text = pData.descripcionCaraIzquierda;
        if (textoEstadisticasIzquierda != null) textoEstadisticasIzquierda.text = pData.estadisticasCaraIzquierda;

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

        // --- Lado Derecho ---
        if (textoNombreDerecha != null) textoNombreDerecha.text = pData.tituloCaraDerecha;
        if (textoDescripcionDerecha != null) textoDescripcionDerecha.text = pData.descripcionCaraDerecha;
        if (textoEstadisticasDerecha != null) textoEstadisticasDerecha.text = pData.estadisticasCaraDerecha;

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
        {
            modoMandoActual = Gamepad.current != null || forzarModoMando;
        }

        GenerarIconosCombos(modoMandoActual, pData.combosCaraIzquierda, contenedorCombosUIIzquierda);
        GenerarIconosCombos(modoMandoActual, pData.combosCaraDerecha, contenedorCombosUIDerecha);
    }

    public bool CambiarPagina(int direccion)
    {
        if (paginas == null || paginas.Length <= 1) return false;

        int nuevaPagina = paginaActual + direccion;

        if (nuevaPagina >= 0 && nuevaPagina < paginas.Length)
        {
            paginaActual = nuevaPagina;
            ConfigurarContenidoPagina();
            return true;
        }

        return false;
    }

    private void OnBotonPasarPaginaClicked()
    {
        if (shelfManagerRef != null)
        {
            shelfManagerRef.IntentarAvanzarPaginaDesdeUI();
        }
    }

    public void ActualizarIconosControl(bool usandoMando)
    {
        modoControlRecibidoDelManager = true;
        modoMandoActual = usandoMando;

        if (paginas != null && paginas.Length > 0)
        {
            PaginaLibro pData = paginas[paginaActual];
            GenerarIconosCombos(usandoMando, pData.combosCaraIzquierda, contenedorCombosUIIzquierda);
            GenerarIconosCombos(usandoMando, pData.combosCaraDerecha, contenedorCombosUIDerecha);
        }
    }

    private void GenerarIconosCombos(bool usandoMando, ComboVisual[] combosPagina, Transform contenedor)
    {
        if (contenedor == null) return;

        foreach (Transform child in contenedor)
        {
            Destroy(child.gameObject);
        }

        if (combosPagina == null) return;

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

    public void SetIsActive(bool active, Vector3 activeScale, float hoverHeight)
    {
        isActiveBook = active;
    }

    public void SetTargetTransform(Vector3 pos, float speed, Vector3 targetScaleVal) { }

    public void MostrarTextosPagina(bool mostrar)
    {
        if (!mostrar)
        {
            if (videoPlayerIzquierda != null) videoPlayerIzquierda.Stop();
            if (videoPlayerDerecha != null) videoPlayerDerecha.Stop();
            if (panelInfoPaginas != null) panelInfoPaginas.SetActive(false);
            return;
        }

        paginaActual = 0;
        ConfigurarContenidoPagina();

        if (panelInfoPaginas != null) panelInfoPaginas.SetActive(true);
    }


}
