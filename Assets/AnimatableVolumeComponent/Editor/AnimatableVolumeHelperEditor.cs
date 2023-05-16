using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    /// <summary>
    /// Editor for AnimatableVolumeHelper. Provides features for animation-keying VolumeComponents.
    /// </summary>
    [CustomEditor(typeof(AnimatableVolumeHelper))]
    public class AnimatableVolumeHelperEditor : Editor
    {
        private AnimatableVolumeHelper volumeHelper;
        private Volume targetVolume;
        private List<AnimatableVolumeComponentBase> animatableVolumeComponentList = new();

        private List<Type> tempVCList = new();

        private static readonly Color RuntimeProfileColor = new Color(0.5f, 0.9f, 0.25f);
        private static GUIContent SyncButtonContentOn;
        private static GUIContent SyncButtonContentOff;

        private void Awake()
        {
            volumeHelper = target as AnimatableVolumeHelper;
            targetVolume = volumeHelper.GetComponent<Volume>();
            volumeHelper.GetComponents(animatableVolumeComponentList);
        }

        private void OnEnable()
        {
            volumeHelper.EditorForceRefreshRuntimeVolumeComponentDic();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawRuntimeProfileSettings();
            DrawMissingAnimatableComponent();
        }

        /// <summary>
        /// Display the current running profile status.
        /// </summary>
        private void DrawRuntimeProfileSettings()
        {
            var hasRuntimeProfile = targetVolume.HasInstantiatedProfile();

            EditorGUILayout.LabelField("Running Profile:", hasRuntimeProfile ? "Runtime" : "Asset");

            string status = null;
            if (!hasRuntimeProfile) {
                status = "No Runtime Profile";
            }
            else {
                status = Application.isPlaying ? "Writing to Profile" : "Reading / Writing to Profile";
            }
            GUILayout.Label($"Status:  {status}");

            if (Application.isPlaying) return;

            if (hasRuntimeProfile) {
                GUI.color = RuntimeProfileColor;
                if (GUILayout.Button("Clear Runtime Profile")) {
                    volumeHelper.ClearRuntimeProfile();

                }
                GUI.color = Color.white;
            }
            else {
                if (GUILayout.Button("Create Runtime Profile  (for editing)")) {
                    volumeHelper.CreateRuntimeProfile();
                }
            }

            GUI.color = Color.white;
        }

        /// <summary>
        /// Draw missing animatable component list, and show a button to add them.
        /// </summary>
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
