using UnityEngine;
using UnityEngine.InputSystem;

public class DebugMenu : MonoBehaviour
{
    [Header("Jugadores")]
    [SerializeField] Fighter playerLeft;
    [SerializeField] Fighter playerRight;

    [Header("Sistemas Opcionales")]
    [SerializeField] ScoreSystem scoreSystem;
    [SerializeField] BugSpawner bugSpawner;

    [Header("Activación")]
    [SerializeField] Key toggleKey = Key.F1;
    [SerializeField] bool visible = false;

    private Rect windowRect = new Rect(20, 20, 320, 420);

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            visible = !visible;
        }
    }

    void OnGUI()
    {
        if (!visible) return;
        windowRect = GUI.Window(90210, windowRect, DrawWindow, "Debug Combate (F1)");
    }

    private void DrawWindow(int id)
    {
        GUILayout.Space(5);

        DrawFighterSection("Jugador Izquierda", playerLeft);
        GUILayout.Space(10);
        DrawFighterSection("Jugador Derecha", playerRight);

        GUILayout.Space(12);
        GUILayout.Label("<b>Escena</b>", new GUIStyle(GUI.skin.label) { richText = true });

        if (scoreSystem != null && GUILayout.Button("Resetear Puntuación"))
        {
            scoreSystem.ResetScores();
        }

        if (bugSpawner != null && GUILayout.Button("Invocar 2 Bichos por Lado"))
        {
            if (playerLeft != null) bugSpawner.SummonBugs(playerLeft, 2, 0.5f);
            if (playerRight != null) bugSpawner.SummonBugs(playerRight, 2, 0.5f);
        }

        GUI.DragWindow();
    }

    private void DrawFighterSection(string title, Fighter f)
    {
        if (f == null)
        {
            GUILayout.Label($"{title}: No asignado");
            return;
        }

        GUILayout.Label($"<b>{title}</b> ({f.State})", new GUIStyle(GUI.skin.label) { richText = true });
        string puntos = scoreSystem != null
            ? (f.isLeftPlayer ? scoreSystem.ScoreLeft : scoreSystem.ScoreRight).ToString("F0")
            : "-";
        GUILayout.Label($"Puntos: {puntos} | Barra: {f.SkillBar:F2}/{f.MaxSkillUses}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+1 Barra"))
        {
            f.AddBarPassive(1f);
        }
        if (GUILayout.Button("Barra Max"))
        {
            f.AddBarPassive(f.MaxSkillUses);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Melee"))
        {
            f.RequestAttack(AttackKind.Melee);
        }
        if (GUILayout.Button("Ranged"))
        {
            f.RequestAttack(AttackKind.Ranged);
        }
        if (GUILayout.Button("Skill"))
        {
            f.RequestSkillPress();
            f.RequestSkillRelease();
        }
        GUILayout.EndHorizontal();
    }
}