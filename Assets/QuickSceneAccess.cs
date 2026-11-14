using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

[InitializeOnLoad]
public class QuickSceneSwitcher
{
    private static readonly List<EditorBuildSettingsScene> _scenes = new();
    private static Vector2 _scrollPos;
    private static GUIStyle _buttonStyle;
    private static GUIStyle _activeButtonStyle;

    private const float ButtonWidth = 150f;
    private const float ButtonHeight = 24f;
    private const float MaxPanelHeight = 220f;
    private const float Padding = 10f;

    static QuickSceneSwitcher()
    {
        EditorBuildSettings.sceneListChanged += RefreshSceneList;
        SceneView.duringSceneGui += OnSceneGUI;
        RefreshSceneList();
    }

    private static void RefreshSceneList()
    {
        _scenes.Clear();
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.enabled)
                _scenes.Add(scene);
    }

    private static void EnsureStyles()
    {
        if (_buttonStyle != null) return;

        _buttonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            alignment = TextAnchor.MiddleLeft,
            fixedHeight = ButtonHeight,
            fontSize = 11,
            margin = new RectOffset(1, 1, 1, 1)
        };

        _activeButtonStyle = new GUIStyle(_buttonStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.5f, 1f, 0.5f) },
            hover = { textColor = new Color(0.5f, 1f, 0.5f) }
        };
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (_scenes.Count == 0) return;

        EnsureStyles();
        Handles.BeginGUI();

        bool isPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;
        bool isBusy = EditorApplication.isUpdating || EditorApplication.isCompiling;

        float containerWidth = ButtonWidth + 18f;
        float contentHeight = _scenes.Count * (ButtonHeight + 2f);
        float panelHeight = Mathf.Min(contentHeight + 12f, MaxPanelHeight);

        var rect = new Rect(
            sceneView.position.width - containerWidth - Padding,
            sceneView.position.height - panelHeight - 30f,
            containerWidth,
            panelHeight
        );

        GUILayout.BeginArea(rect);
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUIStyle.none, GUI.skin.verticalScrollbar);

        string activePath = EditorSceneManager.GetActiveScene().path;

        foreach (var scene in _scenes)
        {
            string sceneName = " " + Path.GetFileNameWithoutExtension(scene.path);
            bool isActive = scene.path == activePath;

            GUIStyle style = isActive ? _activeButtonStyle : _buttonStyle;
            GUIContent content = EditorGUIUtility.TrTextContentWithIcon(sceneName, "SceneAsset Icon");

            EditorGUI.BeginDisabledGroup(isActive || isPlayMode || isBusy);

            if (GUILayout.Button(content, style, GUILayout.Width(ButtonWidth)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scene.path);
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
        Handles.EndGUI();
    }
}