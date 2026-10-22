using UnityEngine;

public class BookItem3D : MonoBehaviour
{
    private Vector3 baseTargetPos;
    private Vector3 baseTargetScale = Vector3.one;
    private float currentTransitionSpeed = 8f;
    private bool isActiveBook = false;

    private Renderer[] bookRenderers;

    void Awake()
    {
        // Buscamos todos los renderers de los hijos (Izquierda y Derecha)
        bookRenderers = GetComponentsInChildren<Renderer>();

        baseTargetPos = transform.position;
        baseTargetScale = transform.localScale;
    }

    public void SetupCoverTexture(Texture2D coverTex)
    {
        if (coverTex == null) return;

        // Si no se inicializó en Awake por orden de ejecución, los buscamos de nuevo
        if (bookRenderers == null || bookRenderers.Length == 0)
        {
            bookRenderers = GetComponentsInChildren<Renderer>();
        }

        foreach (Renderer rend in bookRenderers)
        {
            if (rend != null)
            {
                // Usamos MaterialPropertyBlock para inyectar la textura en el Element 1 
                // sin romper las instancias de los materiales URP
                MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

                // Obtenemos los materiales compartidos para comprobar el índice
                Material[] sharedMats = rend.sharedMaterials;

                for (int i = 0; i < sharedMats.Length; i++)
                {
                    if (sharedMats[i] != null && sharedMats[i].name.Contains("cover"))
                    {
                        rend.GetPropertyBlock(propBlock, i);
                        propBlock.SetTexture("_BaseMap", coverTex);
                        propBlock.SetTexture("_MainTex", coverTex);
                        rend.SetPropertyBlock(propBlock, i);
                    }
                }
            }
        }
    }

    public void SetIsActive(bool active)
    {
        isActiveBook = active;
    }

    public void SetTargetTransform(Vector3 pos, float speed, Vector3 targetScale)
    {
        baseTargetPos = pos;
        baseTargetScale = targetScale;
        currentTransitionSpeed = speed;
    }

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, baseTargetPos, Time.deltaTime * currentTransitionSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, baseTargetScale, Time.deltaTime * currentTransitionSpeed);
    }
}