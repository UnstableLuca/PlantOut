
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class BookItem3D : MonoBehaviour
{
    private Renderer[] bookRenderers;

    [Header("Planta")]
    public PlantData3D plantDataAssigned;

    [Header("UI")]
    public GameObject panelInfoPaginas;
    public TextMeshProUGUI textoNombre;
    public TextMeshProUGUI textoDescripcion;
    public TextMeshProUGUI textoEstadisticas;

    [Header("Botones de combos")]
    public Transform contenedorCombosUI;
    public GameObject iconoBotonPrefab;

    public bool forzarModoMando = false;

    private Vector3 shelfPosition;
    private Vector3 shelfScale;
    private Vector3 targetPosition;
    private Vector3 targetScale;

    private float transitionSpeed = 8f;
    private bool isActiveBook = false;

    private bool modoMandoActual = false;
    private bool modoControlRecibidoDelManager = false;

    void Awake()
    {
        bookRenderers = GetComponentsInChildren<Renderer>();

        shelfPosition = transform.position;
        shelfScale = transform.localScale;

        targetPosition = shelfPosition;
        targetScale = shelfScale;

        if (panelInfoPaginas != null)
            panelInfoPaginas.SetActive(false);
    }

    void Start()
    {
        if (plantDataAssigned != null)
        {
            
            ConfigurarContenidoPagina();
        }
    }

    public void SetupBookTextures(
        Texture2D coverTex,
        Texture2D spineTex,
        Texture2D pagesTex)
    {
        if (bookRenderers == null || bookRenderers.Length == 0)
            bookRenderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in bookRenderers)
        {
            if (rend == null)
                continue;

            Material[] mats = rend.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null)
                    continue;

                Material uniqueMat = new Material(mats[i]);
                string matName = mats[i].name.ToLower();

                if ((matName.Contains("cover") ||
                     matName.Contains("cubierta")) && coverTex != null)
                {
                    uniqueMat.mainTexture = coverTex;
                    uniqueMat.SetTexture("_BaseMap", coverTex);
                }
                else if ((matName.Contains("binding") ||
                          matName.Contains("lomo")) && spineTex != null)
                {
                    uniqueMat.mainTexture = spineTex;
                    uniqueMat.SetTexture("_BaseMap", spineTex);
                }
                else if ((matName.Contains("page") ||
                          matName.Contains("pages") ||
                          matName.Contains("interior")) && pagesTex != null)
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
        if (plantDataAssigned == null)
        {
            Debug.LogWarning(
                $"[{name}] No tiene PlantData3D asignado.", this);
            return;
        }

        if (textoNombre != null)
            textoNombre.text = plantDataAssigned.plantName;

        if (textoDescripcion != null)
            textoDescripcion.text = plantDataAssigned.description;

        if (textoEstadisticas != null)
            textoEstadisticas.text = plantDataAssigned.estadisticas;

       
        if (!modoControlRecibidoDelManager)
        {
            modoMandoActual =
                Gamepad.current != null || forzarModoMando;
        }

        GenerarIconosCombos(modoMandoActual);
    }


    public void ActualizarIconosControl(bool usandoMando)
    {
        modoControlRecibidoDelManager = true;
        modoMandoActual = usandoMando;

        GenerarIconosCombos(usandoMando);
    }

    private void GenerarIconosCombos(bool usandoMando)
    {
       

        ComboVisual[] combos = plantDataAssigned.combosVisuales;

        

        System.Collections.Generic.List<Sprite> spritesNuevos =
            new System.Collections.Generic.List<Sprite>();

        foreach (ComboVisual combo in combos)
        {
            if (combo == null)
                continue;

            Sprite[] sprites = usandoMando
                ? combo.botonesMando
                : combo.botonesTeclado;

            if (sprites == null)
                continue;

            foreach (Sprite sprite in sprites)
            {
                if (sprite != null)
                    spritesNuevos.Add(sprite);
            }
        }

        if (spritesNuevos.Count == 0)
        {
          
            return;
        }

        System.Collections.Generic.List<GameObject> nuevosIconos =
            new System.Collections.Generic.List<GameObject>();

        foreach (Sprite sprite in spritesNuevos)
        {
            GameObject icono = Instantiate(
                iconoBotonPrefab,
                contenedorCombosUI
            );

            Image imagen = icono.GetComponent<Image>();

            if (imagen == null)
            {
               

                Destroy(icono);
                continue;
            }

            imagen.sprite = sprite;
            imagen.preserveAspect = true;
            icono.transform.localScale = Vector3.one;

            nuevosIconos.Add(icono);
        }

        if (nuevosIconos.Count == 0)
            return;

        foreach (Transform child in contenedorCombosUI)
        {
            if (!nuevosIconos.Contains(child.gameObject))
                Destroy(child.gameObject);
        }

        
    }



    public void SetIsActive(
        bool active,
        Vector3 activeScale,
        float hoverHeight)
    {
        isActiveBook = active;

        if (active)
        {
            targetPosition =
                shelfPosition + new Vector3(0f, hoverHeight, 0f);

            targetScale = activeScale;
        }
        else
        {
            targetPosition = shelfPosition;
            targetScale = shelfScale;
        }
    }

    public void SetTargetTransform(
        Vector3 pos,
        float speed,
        Vector3 targetScaleVal)
    {
        targetPosition = pos;
        targetScale = targetScaleVal;
        transitionSpeed = speed;
    }

    public void MostrarTextosPagina(bool mostrar)
    {
        if (!mostrar)
        {
            if (panelInfoPaginas != null)
                panelInfoPaginas.SetActive(false);

            return;
        }

       

        ConfigurarContenidoPagina();

        if (panelInfoPaginas != null)
            panelInfoPaginas.SetActive(true);
        
    }

    void Update()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * transitionSpeed
        );

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * transitionSpeed
        );
    }
}
