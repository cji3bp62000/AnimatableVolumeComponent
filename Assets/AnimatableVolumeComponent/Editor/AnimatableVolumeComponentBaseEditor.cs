using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    [CustomEditor(typeof(AnimatableVolumeComponentBase), true)]
    public class AnimatableVolumeComponentBaseEditor : Editor
    {
        private static readonly GUIContent EmptyLabel = new GUIContent();
        private static readonly GUILayoutOption[] CheckboxOptions = { GUILayout.MaxWidth(16) };

        private HashSet<string> showedProperties = new();

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = serializedObject.GetIterator();
            // a VolumeComponent-like editor
            // this editor maybe not be good for customizing...
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false) {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath)) {
                    if (iterator.propertyPath.StartsWith("override_") &&
                        iterator.propertyType == SerializedPropertyType.Boolean) {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(iterator, EmptyLabel, CheckboxOptions);
                        var fieldName = iterator.propertyPath.Substring("override_".Length);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(fieldName), true);
                        EditorGUILayout.EndHorizontal();

                        showedProperties.Add(fieldName);
                    }
                    else if (iterator.propertyPath == "active") {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Volume Component Properties", EditorStyles.boldLabel);
                        iterator.boolValue = EditorGUILayout.ToggleLeft(iterator.displayName, iterator.boolValue);
                        EditorGUILayout.Space(5);
                    }
                    else if (iterator.propertyPath == "writeValuesOnLateUpdate") {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                        iterator.boolValue = EditorGUILayout.ToggleLeft(iterator.displayName, iterator.boolValue);
                    }
                    else if (!showedProperties.Contains(iterator.propertyPath)) {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space(16, false);
                        EditorGUILayout.PropertyField(iterator, true);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    }
                }
            }

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
            }

            showedProperties.Clear();
        }
    }
}
