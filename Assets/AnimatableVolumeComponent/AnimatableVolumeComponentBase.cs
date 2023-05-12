using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    [ExecuteAlways]
    [RequireComponent(typeof(Volume)), RequireComponent(typeof(VolumeHelper))]
    public abstract class AnimatableVolumeComponentBase : MonoBehaviour
    {
        public abstract Type TargetType { get; }

        public bool active;

        protected VolumeHelper volumeHelper
        {
            get {
                if (!_cachedVolumeHelper) _cachedVolumeHelper = GetComponent<VolumeHelper>();
                return _cachedVolumeHelper;
            }
        }
        private VolumeHelper _cachedVolumeHelper;

        protected virtual void LateUpdate()
        {
#if UNITY_EDITOR
            // stop writing, only when reading from profile
            if (volumeHelper.editorSyncProfileToAnimatable) return;
#endif

            WriteToVolumeComponent();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Animation applied/previewed callback
        /// </summary>
        private void OnDidApplyAnimationProperties()
        {
            if (Application.isPlaying) return;

            // update profile when previewing animation
            WriteToVolumeComponent();
        }
#endif

        private void OnValidate()
        {
            // if no runtime profile, create one
            volumeHelper.CreateRuntimeProfile();
            WriteToVolumeComponent();
            // some Parameter has min/max value, so when keying / modifying in editor,
            // we want to also clamp to Parameter's range
            ReadFromVolumeComponent();
        }

        public abstract void WriteToVolumeComponent();
        public abstract void ReadFromVolumeComponent();
    }
}
