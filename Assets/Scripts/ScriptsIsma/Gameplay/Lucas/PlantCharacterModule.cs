using UnityEngine;

public class PlantCharacterModule : MonoBehaviour
{
    [Header("Acciones básicas")]
    public PlayerAction meleeAsset;
    public PlayerAction rangedAsset;
    public PlayerAction blockAsset;

    [Header("Configuración")]
    [Tooltip("Desmarcar solo en Belladona.")]
    public bool canGainStandardEnergy = true;

    [Header("Referencias")]
    public Animator modelAnimator;
    public Transform firePoint;
    public Transform stemTransform;

    public Fighter ParentFighter {  get; private set; }

    private void Start()
    {
        ParentFighter = GetComponentInParent<Fighter>();
        if (ParentFighter != null)
        {
            ParentFighter.RegisterCharacter(
                meleeAsset,
                rangedAsset,
                blockAsset,
                modelAnimator,
                firePoint,
                stemTransform,
                canGainStandardEnergy
                );

            if (!ParentFighter.isLeftPlayer)
            {
                Vector3 scale = transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }
        else
        {
            Debug.LogError($"[{name}] No se encontró un componente Fighter en el objeto padre.");
        }
    }
}
