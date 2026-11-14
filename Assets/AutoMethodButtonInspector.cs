using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Text.RegularExpressions;

[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class AutoMethodButtonInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (target is not MonoBehaviour targetScript) return;

        var methods = targetScript.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        bool drewHeader = false;

        foreach (var method in methods)
        {
            if (method.IsSpecialName || method.GetParameters().Length > 0) continue;

            if (!drewHeader)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Public Actions", EditorStyles.boldLabel);
                drewHeader = true;
            }

            if (GUILayout.Button(Regex.Replace(method.Name, "(\\B[A-Z])", " $1")))
            {
                method.Invoke(targetScript, null);
            }
        }
    }
}