using UnityEngine;
using System.Collections.Generic;

public class StatusEffectUI : MonoBehaviour
{
    [System.Serializable]
    public class EffectVisual
    {
        public Fighter.StatusEffect effect;
        public Sprite colorIcon;
        public Sprite greyIcon;
    }

    [SerializeField] Fighter fighter;
    [Tooltip("Prefab del circulo (con StatusIconView)")]
    [SerializeField] StatusIconView iconPrefab;
    [Tooltip("Donde se crean los circulos. Vacio = este objeto")]
    [SerializeField] Transform container;
    [Tooltip("Orden en que salen de izquierda a derecha")]
    [SerializeField] EffectVisual[] effects;

    readonly Dictionary<Fighter.StatusEffect, StatusIconView> active =
        new Dictionary<Fighter.StatusEffect, StatusIconView>();

    void Awake()
    {
        if (container == null) container = transform;
    }

    void LateUpdate()
    {
        if (fighter == null || iconPrefab == null || effects == null) return;

        int index = 0;
        foreach (EffectVisual e in effects)
        {
            active.TryGetValue(e.effect, out StatusIconView view);

            if (fighter.TryGetStatus(e.effect, out float remaining, out float duration))
            {
                if (view == null)
                {
                    view = Instantiate(iconPrefab, container);
                    view.Setup(e.colorIcon, e.greyIcon);
                    active[e.effect] = view;
                }

                if (view.transform.GetSiblingIndex() != index) view.transform.SetSiblingIndex(index);
                index++;

                view.SetProgress(1f - Mathf.Clamp01(remaining / duration), fighter.GetStatusLevel(e.effect));
            }
            else if (view != null)
            {
                Destroy(view.gameObject);
                active.Remove(e.effect);
            }
        }
    }
}