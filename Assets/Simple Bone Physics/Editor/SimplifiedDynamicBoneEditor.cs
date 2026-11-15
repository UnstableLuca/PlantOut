#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace AtivusStudio.SimplifiedDynamicBone.Editor
{
    [CustomEditor(typeof(AtivusStudio.SimplifiedDynamicBone.SimplifiedDynamicBone))]
    public class SimplifiedDynamicBoneEditor : UnityEditor.Editor
    {
        private bool showAdvanced;

        private SerializedProperty tail1;
        private SerializedProperty tail2;
        private SerializedProperty tail3;

        private SerializedProperty autoFindChildren;
        private SerializedProperty preset;

        private SerializedProperty inertia;
        private SerializedProperty stiffness;
        private SerializedProperty stretch;
        private SerializedProperty velocity;

        private SerializedProperty useConeLimit;
        private SerializedProperty tail1ConeOffset;
        private SerializedProperty tail2ConeOffset;
        private SerializedProperty tail1ConeAngle;
        private SerializedProperty tail2ConeAngle;

        private SerializedProperty liveUpdateValues;
        private SerializedProperty drawGizmos;

        private void OnEnable()
        {
            tail1 = serializedObject.FindProperty("tail1");
            tail2 = serializedObject.FindProperty("tail2");
            tail3 = serializedObject.FindProperty("tail3");

            autoFindChildren = serializedObject.FindProperty("autoFindChildren");
            preset = serializedObject.FindProperty("preset");

            inertia = serializedObject.FindProperty("inertia");
            stiffness = serializedObject.FindProperty("stiffness");
            stretch = serializedObject.FindProperty("stretch");
            velocity = serializedObject.FindProperty("velocity");

            useConeLimit = serializedObject.FindProperty("useConeLimit");
            tail1ConeOffset = serializedObject.FindProperty("tail1ConeOffset");
            tail2ConeOffset = serializedObject.FindProperty("tail2ConeOffset");
            tail1ConeAngle = serializedObject.FindProperty("tail1ConeAngle");
            tail2ConeAngle = serializedObject.FindProperty("tail2ConeAngle");

            liveUpdateValues = serializedObject.FindProperty("liveUpdateValues");
            drawGizmos = serializedObject.FindProperty("drawGizmos");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (autoFindChildren != null)
                autoFindChildren.boolValue = true;

            DrawTail1();

            EditorGUILayout.Space(6);

            DrawPreset();

            EditorGUILayout.Space(6);

            DrawControls();

            EditorGUILayout.Space(6);

            DrawAdvanced();

            AutoFindTailChildren();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTail1()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Tail 1", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tail1, GUIContent.none);

            EditorGUILayout.EndVertical();
        }

        private void DrawPreset()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(
                preset,
                new GUIContent("Preset")
            );

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                SimplifiedDynamicBone script = (SimplifiedDynamicBone)target;
                script.ApplyPreset(script.preset);

                EditorUtility.SetDirty(script);
                serializedObject.Update();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(inertia, new GUIContent("Inertia"));
            EditorGUILayout.PropertyField(stiffness, new GUIContent("Stiffness"));
            EditorGUILayout.PropertyField(stretch, new GUIContent("Stretch"));
            EditorGUILayout.PropertyField(velocity, new GUIContent("Velocity"));

            EditorGUILayout.EndVertical();
        }

        private void DrawAdvanced()
        {
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);

            if (!showAdvanced)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(
                useConeLimit,
                new GUIContent("Cone Limit")
            );

            if (useConeLimit.boolValue)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(
                    tail1ConeOffset,
                    new GUIContent("Tail 1 Offset")
                );

                EditorGUILayout.PropertyField(
                    tail1ConeAngle,
                    new GUIContent("Tail 1 Angle")
                );

                EditorGUILayout.PropertyField(
                    tail2ConeOffset,
                    new GUIContent("Tail 2 Offset")
                );

                EditorGUILayout.PropertyField(
                    tail2ConeAngle,
                    new GUIContent("Tail 2 Angle")
                );

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(6);

            EditorGUILayout.PropertyField(
                liveUpdateValues,
                new GUIContent("Live Update")
            );

            EditorGUILayout.PropertyField(
                drawGizmos,
                new GUIContent("Draw Gizmos")
            );

            EditorGUILayout.EndVertical();
        }

        private void AutoFindTailChildren()
        {
            if (tail1.objectReferenceValue == null)
                return;

            Transform root = tail1.objectReferenceValue as Transform;

            if (root == null)
                return;

            if (root.childCount > 0)
            {
                Transform child = root.GetChild(0);

                tail2.objectReferenceValue = child;

                if (child.childCount > 0)
                    tail3.objectReferenceValue = child.GetChild(0);
            }
        }
    }
}
#endif