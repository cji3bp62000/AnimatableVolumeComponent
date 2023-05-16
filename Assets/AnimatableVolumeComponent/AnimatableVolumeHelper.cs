using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    [RequireComponent(typeof(Volume))]
    public partial class AnimatableVolumeHelper : MonoBehaviour
    {
        /// <summary> cached Volume </summary>
        private Volume volume
        {
            get {
                if (!_cachedVolume) _cachedVolume = GetComponent<Volume>();
                return _cachedVolume;
            }
        }
        private Volume _cachedVolume;

        /// <summary> cached VolumeComponent in VolumeProfile </summary>
        private Dictionary<Type, VolumeComponent> runtimeVolumeComponentDic = new();

        /// <summary>
        /// Get VolumeComponent.
        /// </summary>
        /// <param name="volumeComponent">target VolumeComponent</param>
        /// <typeparam name="T">target VolumeComponent type</typeparam>
        /// <returns>success or not</returns>
        public bool TryGet<T>(out T volumeComponent) where T : VolumeComponent
        {
            var hasValue = runtimeVolumeComponentDic.TryGetValue(typeof(T), out var vc);
            volumeComponent = vc as T;
            return hasValue;
        }

        /// <summary>
        /// Get VolumeComponent.
        /// </summary>
        /// <param name="volumeComponentType">target VolumeComponent type</param>
        /// <param name="volumeComponent">target VolumeComponent</param>
        /// <returns>success or not</returns>
        public bool TryGet(Type volumeComponentType, out VolumeComponent volumeComponent)
        {
            return runtimeVolumeComponentDic.TryGetValue(volumeComponentType, out volumeComponent);
        }

        private void Start()
        {
            if (!Application.isPlaying) return;

            ClearRuntimeProfile();
            CreateRuntimeProfile();
        }

        /// <summary>
        /// Create runtime profile and cache VolumeComponents.
        /// </summary>
        public void CreateRuntimeProfile()
        {
            if (volume.HasInstantiatedProfile()) return;

            var runtimeProfile = volume.profile;
            runtimeVolumeComponentDic.Clear();
            foreach (var volumeComponent in runtimeProfile.components) {
                runtimeVolumeComponentDic[volumeComponent.GetType()] = volumeComponent;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying) {
                // in editor mode, we don't auto write to volume component at LateUpdate, so manually initialize runtime profile
                // (other inspector operations will write to volume component by OnValidate())
                foreach (var avc in GetComponents<AnimatableVolumeComponentBase>()) {
                    avc.WriteToVolumeComponent();
                }
            }
#endif
        }

        /// <summary>
        /// Clear runtime profile and cached components.
        /// </summary>
        public void ClearRuntimeProfile()
        {
            if (!volume.HasInstantiatedProfile()) return;

            var runtimeProfile = volume.profile;
            volume.profile = null;
            DestroyImmediate(runtimeProfile);
            runtimeVolumeComponentDic.Clear();
        }

        /// <summary>
        /// Add AnimatableVolumeComponent to this GameObject during playing.
        /// </summary>
        public void AddCorrespondingComponent()
        {
            var avcList = GetComponents<AnimatableVolumeComponentBase>();
            foreach (var vc in volume.sharedProfile.components) {
                var vcType = vc.GetType();
                if (Array.FindIndex(avcList, avc => avc.TargetType == vcType) >= 0) continue;
                if (!AnimatableVolumeComponentMapping.Map.TryGetValue(vcType, out var acType)) continue;

                gameObject.AddComponent(acType);
            }
        }

        /// <summary>
        /// Clear runtime profile on destroy.
        /// </summary>
        private void OnDestroy()
        {
            ClearRuntimeProfile();
        }
    }


    /// <summary>
    /// Things only for inspector editing. These should not be used in runtime code
    /// </summary>
#if UNITY_EDITOR
    public partial class AnimatableVolumeHelper
    {
        public void EditorForceRefreshRuntimeVolumeComponentDic()
        {
            runtimeVolumeComponentDic.Clear();
            if (!volume.HasInstantiatedProfile()) return;

            var runtimeProfile = volume.profile;
            foreach (var volumeComponent in runtimeProfile.components) {
                runtimeVolumeComponentDic[volumeComponent.GetType()] = volumeComponent;
            }
        }
    }
#endif

}
