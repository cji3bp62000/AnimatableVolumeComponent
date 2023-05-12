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

        private Dictionary<Type, VolumeComponent> runtimeVolumeComponentDic = new();

        public bool TryGet<T>(out T volumeComponent) where T : VolumeComponent
        {
            var hasValue = runtimeVolumeComponentDic.TryGetValue(typeof(T), out var vc);
            volumeComponent = vc as T;
            return hasValue;
        }

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

        public void CreateRuntimeProfile()
        {
            if (volume.HasInstantiatedProfile()) return;

            var runtimeProfile = volume.profile;
            runtimeVolumeComponentDic.Clear();
            foreach (var volumeComponent in runtimeProfile.components) {
                runtimeVolumeComponentDic[volumeComponent.GetType()] = volumeComponent;
            }
        }

        public void ClearRuntimeProfile()
        {
            if (!volume.HasInstantiatedProfile()) return;

            var runtimeProfile = volume.profile;
            volume.profile = null;
            DestroyImmediate(runtimeProfile);
            runtimeVolumeComponentDic.Clear();
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
    /// Things only for inspector editing. These should not be used in runtime code
    /// </summary>
#if UNITY_EDITOR
    public partial class VolumeHelper
    {
        [NonSerialized] public bool editorSyncProfileToAnimatable;

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
