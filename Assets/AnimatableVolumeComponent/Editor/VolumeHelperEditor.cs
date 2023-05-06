using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    [CustomEditor(typeof(VolumeHelper))]
    public class VolumeHelperEditor : Editor
    {
        private VolumeHelper volumeHelper;
        private Volume targetVolume;
        private List<AnimatableVolumeComponentBase> animatableVolumeComponentList = new();

        private List<Type> tempVCList = new();

        private void Awake()
        {
            volumeHelper = target as VolumeHelper;
            targetVolume = volumeHelper.GetComponent<Volume>();
            volumeHelper.GetComponents(animatableVolumeComponentList);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawRuntimeProfileSettings();
            DrawMissingAnimatableComponent();
        }

        private void DrawRuntimeProfileSettings()
        {
            var hasRuntimeProfile = targetVolume.HasInstantiatedProfile();

            EditorGUILayout.LabelField("Running Profile:", hasRuntimeProfile ? "Runtime" : "Asset");

            using var disabledScope = new EditorGUI.DisabledScope(!hasRuntimeProfile);
            if (hasRuntimeProfile) GUI.color = new Color(0.5f, 0.9f, 0.25f);
            if (GUILayout.Button("Clear Runtime Profile")) {
                var runtimeProfile = targetVolume.profile;
                targetVolume.profile = null;
                DestroyImmediate(runtimeProfile);
            }

            GUI.color = Color.white;
        }

        private void DrawMissingAnimatableComponent()
        {
            if (!targetVolume.sharedProfile) return;

            tempVCList.Clear();
            foreach (var vc in targetVolume.sharedProfile.components) {
                var vcType = vc.GetType();
                if (animatableVolumeComponentList.FindIndex(avc => avc.TargetType == vcType) < 0) {
                    tempVCList.Add(vcType);
                }
            }

            if (tempVCList.Count > 0) {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Animatable Component Missing:");
                var log = string.Join(
                    "\n",
                    tempVCList.Select(vcType => $" - {vcType.Name}")
                );
                EditorGUILayout.HelpBox(log, MessageType.None);
                EditorGUILayout.Space(5);
                if (GUILayout.Button("Add Corresponding Animatable Component")) {
                    tempVCList.Reverse();
                    for (var i = tempVCList.Count - 1; i >= 0; i--) {
                        var vcType = tempVCList[i];
                        if (!AnimatableVolumeComponentMapping.Map.TryGetValue(vcType, out var acType)) continue;

                        Undo.AddComponent(volumeHelper.gameObject, acType);
                    }
                }
            }
        }
    }
}
