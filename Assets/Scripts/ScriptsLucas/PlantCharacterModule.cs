using UnityEngine;

public class PlantCharacterModule : MonoBehaviour
{
    [Header("Acciones básicas")]
    public PlayerAction meleeAsset;
    public PlayerAction rangedAsset;
    public PlayerAction blockAsset;

    [Header("Habilidades de Planta")]
    public SkillController skillController;

    [Header("Referencias del Prefab")]
    public Animator modelAnimator;
    [Tooltip("Punto donde impactan los proyectiles rivales en esta planta")]
    public Transform aimPoint;
    [Tooltip("Punto desde donde salen los proyectiles de esta planta")]
    public Transform projectileOrigin;
    [Tooltip("Malla del tallo separada para PlantHeight (opcional)")]
    public Transform stemTransform;

    public Fighter ParentFighter {  get; private set; }

    private void Awake()
    {
        if (modelAnimator == null)
            modelAnimator = GetComponentInChildren<Animator>();

        if (skillController == null)
            skillController = GetComponentInChildren<SkillController>();

        ParentFighter = GetComponentInParent<Fighter>();
        if (ParentFighter != null)
        {
            ParentFighter.RegisterCharacter(
                meleeAsset,
                rangedAsset,
                blockAsset,
                modelAnimator,
                aimPoint,
                projectileOrigin,
                stemTransform,
                skillController
            );

            // Voltear visualmente si es el jugador de la derecha
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
