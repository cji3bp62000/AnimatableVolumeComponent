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
            DrawMissingVolumeComponentInOriginalVolumeProfile();
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

            // check if there are any missing animatable components
            tempVCList.Clear();
            foreach (var vc in targetVolume.sharedProfile.components) {
                var vcType = vc.GetType();
                var sameTypeFound = false;
                foreach (var avc in animatableVolumeComponentList) {
                    if (avc.TargetType == vcType) {
                        sameTypeFound = true;
                        break;
                    }
                }
                if (!sameTypeFound) {
                    tempVCList.Add(vcType);
                }
            }

            if (tempVCList.Count == 0) return;

            // draw message
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("▶ Animatable Components Missing:", EditorStyles.largeLabel);
            var log = string.Join(
                "\n",
                tempVCList.Select(vcType => $" - {vcType.Name}")
            );
            var guiBackgroundOriginColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            EditorGUILayout.HelpBox(log, MessageType.None);
            GUI.backgroundColor = guiBackgroundOriginColor;

            // draw button
            EditorGUILayout.Space(5);
            if (!GUILayout.Button("Add Corresponding Animatable Component")) return;
            {
                tempVCList.Reverse();
                for (var i = tempVCList.Count - 1; i >= 0; i--) {
                    var vcType = tempVCList[i];
                    if (!AnimatableVolumeComponentMapping.Map.TryGetValue(vcType, out var acType)) continue;

                    Undo.AddComponent(volumeHelper.gameObject, acType);
                }
            }
        }

        /// <summary>
        /// Draw missing volume components that only exist on runtime (instantiated) profile, and show a button to add them back to original profile.
        /// </summary>
        private void DrawMissingVolumeComponentInOriginalVolumeProfile()
        {
            if (!targetVolume.HasInstantiatedProfile() || !targetVolume.sharedProfile) return;

            var runtimeProfile = targetVolume.profile;
            var originalProfile = targetVolume.sharedProfile;

            // check if there are any missing components
            tempVCList.Clear();
            foreach (var vc in runtimeProfile.components) {
                var vcType = vc.GetType();
                var sameTypeFound = false;
                foreach (var originalVc in originalProfile.components) {
                    if (vcType == originalVc.GetType()) {
                        sameTypeFound = true;
                        break;
                    }
                }
                if (!sameTypeFound) {
                    tempVCList.Add(vcType);
                }
            }

            if (tempVCList.Count == 0) return;

            // draw message
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("▶ Volume Components Missing in Original Profile:", EditorStyles.largeLabel);
            var log = string.Join(
                "\n",
                tempVCList.Select(vcType => $" - {vcType.Name}")
            );
            var guiBackgroundOriginColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            EditorGUILayout.HelpBox(log, MessageType.None);
            GUI.backgroundColor = guiBackgroundOriginColor;

            // draw button
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Add Missing Volume Component")) {
                tempVCList.Reverse();
                for (var i = tempVCList.Count - 1; i >= 0; i--) {
                    // add to original profile
                    var vcType = tempVCList[i];
                    originalProfile.Add(vcType);

                    // also check if the corresponding animatable component exists; if no, add it
                    var sameTypeFound = false;
                    foreach (var avc in animatableVolumeComponentList) {
                        if (avc.TargetType == vcType) {
                            sameTypeFound = true;
                            break;
                        }
                    }
                    if (sameTypeFound) continue;
                    if (!AnimatableVolumeComponentMapping.Map.TryGetValue(vcType, out var acType)) continue;

                    Undo.AddComponent(volumeHelper.gameObject, acType);
                }
                EditorUtility.SetDirty(originalProfile);
            }
        }
    }
}
