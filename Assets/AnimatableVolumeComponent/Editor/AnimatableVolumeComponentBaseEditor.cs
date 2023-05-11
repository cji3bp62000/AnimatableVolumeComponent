using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    [CustomEditor(typeof(AnimatableVolumeComponentBase), true)]
    public class AnimatableVolumeComponentBaseEditor : Editor
    {
        private static readonly GUIContent EmptyLabel = new GUIContent();
        private static readonly GUILayoutOption[] CheckboxOptions = { GUILayout.MaxWidth(16) };

        private HashSet<string> showedProperties = new();

        private AnimatableVolumeComponentBase avc;
        private Volume volume;
        private VolumeHelper volumeHelper;

        private void OnEnable()
        {
            avc = target as AnimatableVolumeComponentBase;
            volume = avc.GetComponent<Volume>();
            volumeHelper = avc.GetComponent<VolumeHelper>();
        }

        public override void OnInspectorGUI()
        {
            ShowProperties();
            SyncVolumeComponentValues();
        }

        private void ShowProperties()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = serializedObject.GetIterator();
            // a VolumeComponent-like editor
            // this editor maybe not be good for customizing...
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false) {
                if (iterator.propertyPath == "m_Script") {
                    using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath)) {
                        EditorGUILayout.PropertyField(iterator, true);
                    }

                    string status = "";
                    if (!volume.HasInstantiatedProfile()) {
                        status = "No Runtime Profile";
                    }
                    else {
                        status = volumeHelper.editorSyncProfileToAnimatable ? "Reading from Profile" : "Writing to Profile";
                    }
                    EditorGUILayout.LabelField($"Status:  {status}");
                    EditorGUILayout.Space(5);
                }
                else if (iterator.propertyPath.StartsWith("override_")
                    && iterator.propertyType == SerializedPropertyType.Boolean) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(iterator, EmptyLabel, CheckboxOptions);
                    var fieldName = iterator.propertyPath.Substring("override_".Length);
                    var fieldSP = serializedObject.FindProperty(fieldName);
                    // there's a chance that Parameter is overridable but not animatable
                    if (fieldSP != null) {
                        if (fieldSP.hasVisibleChildren) {
                            EditorGUILayout.Space(8f, expand:false);
                        }
                        EditorGUILayout.PropertyField(fieldSP, true);
                    }
                    else {
                        var niceFieldName = ObjectNames.NicifyVariableName(fieldName);
                        EditorGUILayout.LabelField(niceFieldName, "(value is not animatable)");
                    }
                    EditorGUILayout.EndHorizontal();

                    showedProperties.Add(fieldName);
                }
                else if (iterator.propertyPath == "active") {
                    EditorGUILayout.LabelField("Volume Parameters", EditorStyles.boldLabel);
                    iterator.boolValue = EditorGUILayout.ToggleLeft(iterator.displayName, iterator.boolValue);
                    EditorGUILayout.Space(5);
                }

                else if (!showedProperties.Contains(iterator.propertyPath)) {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
            }

            showedProperties.Clear();
        }

        private void SyncVolumeComponentValues()
        {
            if (!volumeHelper.editorSyncProfileToAnimatable) return;
            if (!avc) return;

            Undo.RecordObject(target, "");
            avc.ReadFromVolumeComponent();
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
