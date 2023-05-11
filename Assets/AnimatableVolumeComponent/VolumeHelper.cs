using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    [RequireComponent(typeof(Volume))]
    public partial class VolumeHelper : MonoBehaviour
    {
        private Volume volume
        {
            get {
                if (!_cachedVolume) _cachedVolume = GetComponent<Volume>();
                return _cachedVolume;
            }
        }
        private Volume _cachedVolume;

        private VolumeProfile runtimeProfile;
        private Dictionary<Type, VolumeComponent> runtimeVolumeComponentDic = new();

        public bool TryGet<T>(out T volumeComponent) where T : VolumeComponent
        {
            var hasValue = runtimeVolumeComponentDic.TryGetValue(typeof(T), out var vc);
            volumeComponent = vc as T;
            return hasValue;
        }

        private void Start()
        {
            if (!Application.isPlaying) return;

            ClearRuntimeProfile();
            CreateRuntimeProfile();
        }

        public void CreateRuntimeProfile()
        {
            if (volume.HasInstantiatedProfile() && runtimeProfile == volume.profile) return;

            // in editor mode, sometimes runtimeProfile will hold last profile after code compiling
            if (runtimeProfile) {
                DestroyImmediate(runtimeProfile);
            }

            runtimeProfile = volume.profile;
            runtimeVolumeComponentDic.Clear();
            foreach (var volumeComponent in runtimeProfile.components) {
                runtimeVolumeComponentDic[volumeComponent.GetType()] = volumeComponent;
            }
        }

        public void ClearRuntimeProfile()
        {
            if (!volume.HasInstantiatedProfile()) return;

            volume.profile = null;
            DestroyImmediate(runtimeProfile);
            runtimeVolumeComponentDic.Clear();
            runtimeProfile = null;
        }

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

        private void OnDestroy()
        {
            ClearRuntimeProfile();
        }
    }


    /// <summary>
    /// parts only for inspector editing
    /// </summary>
#if UNITY_EDITOR
    public partial class VolumeHelper
    {
        [NonSerialized] public bool editorSyncProfileToAnimatable;
    }
#endif

}
