using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    /// <summary>
    /// Custom editor of AnimatableVolumeComponent. Has a VolumeComponent-like inspector.
    /// </summary>
    [CustomEditor(typeof(AnimatableVolumeComponentBase), true)]
    public class AnimatableVolumeComponentBaseEditor : Editor
    {
        private static readonly GUIContent EmptyLabel = new GUIContent();
        private static readonly GUILayoutOption[] CheckboxOptions = { GUILayout.MaxWidth(16) };

        private HashSet<string> showedProperties = new();

        private AnimatableVolumeComponentBase avc;
        private Volume volume;
        private AnimatableVolumeHelper volumeHelper;

        private int dirtyCount = 0;

        private void OnEnable()
        {
            avc = target as AnimatableVolumeComponentBase;
            volume = avc.GetComponent<Volume>();
            volumeHelper = avc.GetComponent<AnimatableVolumeHelper>();
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
                        status = Application.isPlaying ? "Writing to Profile" : "Reading / Writing to Profile";
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

        /// <summary>
        /// sync values from VolumeComponent to AnimatableVolumeComponentBase;
        /// by this, user can key animation by directly change VolumeComponent parameter values.
        /// </summary>
        private void SyncVolumeComponentValues()
        {
            if (!avc) return;

            if (!volumeHelper.TryGet(avc.TargetType, out var targetVolumeComponent)) {
                return;
            }

            // only record when volume component is modified
            var oldDirtyCount = dirtyCount;
            dirtyCount = EditorUtility.GetDirtyCount(targetVolumeComponent);
            if (dirtyCount <= oldDirtyCount) return;

            Undo.RecordObject(target, "");
            avc.ReadFromVolumeComponent();
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
