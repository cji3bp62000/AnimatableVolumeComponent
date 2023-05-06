using System;
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
#pragma warning disable 618
                if (!_cachedVolume) _cachedVolume = GetComponent<Volume>();
                return _cachedVolume;
#pragma warning restore 618
            }
        }
        [Obsolete("use volume.")] private Volume _cachedVolume;

        public bool TryGet<T>(out T volumeComponent) where T : VolumeComponent
        {
            volumeComponent = default;
            if (!volume || !volume.profile || !volume.profile.TryGet<T>(out volumeComponent)) return false;

            return true;
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
            if (volume && volume.HasInstantiatedProfile()) {
                var runtimeProfile = volume.profile;
                volume.profile = null;
                DestroyImmediate(runtimeProfile);
            }
        }
    }
}
