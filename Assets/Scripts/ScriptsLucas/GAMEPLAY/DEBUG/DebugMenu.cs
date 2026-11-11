using UnityEngine;
using UnityEngine.InputSystem;

public class DebugMenu : MonoBehaviour
{
    [SerializeField] Fighter[] fighters;
    [SerializeField] Key toggleKey = Key.F1;
    [Tooltip("Escala de la interfaz (sube si se ve pequena)")]
    [SerializeField, Range(1f, 3f)] float uiScale = 1.5f;

    bool visible;

    void Awake()
    {
        if (!Application.isEditor && !Debug.isDebugBuild) enabled = false;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame) visible = !visible;
    }

    void OnGUI()
    {
        if (!visible || fighters == null) return;

        GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));

        GUILayout.BeginArea(new Rect(10, 10, 300, 600), GUI.skin.box);
        GUILayout.Label("DEBUG  (" + toggleKey + " para cerrar)");

        foreach (var f in fighters)
        {
            if (f == null) continue;

            GUILayout.Space(6);
            GUILayout.Label($"{f.name}   estado: {f.State}");
            GUILayout.Label($"Barra: {f.SkillBar:0.00} / {f.MaxSkillUses}");

            float current = f.SkillBar;
            float slider = GUILayout.HorizontalSlider(current, 0f, f.MaxSkillUses);
            if (!Mathf.Approximately(slider, current)) SetBar(f, slider);

            GUILayout.BeginHorizontal();
            for (int i = 0; i <= f.MaxSkillUses; i++)
                if (GUILayout.Button(i.ToString())) SetBar(f, i);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Llenar todas")) SetAll(true);
        if (GUILayout.Button("Vaciar todas")) SetAll(false);
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    void SetAll(bool full)
    {
        foreach (var f in fighters)
            if (f != null) SetBar(f, full ? f.MaxSkillUses : 0f);
    }

    static void SetBar(Fighter f, float value) => f.AddBarPassive(value - f.SkillBar);
}