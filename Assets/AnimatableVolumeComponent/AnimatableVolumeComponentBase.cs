using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    [ExecuteAlways]
    [RequireComponent(typeof(Volume)), RequireComponent(typeof(AnimatableVolumeHelper))]
    public abstract class AnimatableVolumeComponentBase : MonoBehaviour
    {
        public abstract Type TargetType { get; }

        public bool active;

        protected AnimatableVolumeHelper volumeHelper
        {
            get {
                if (!_cachedVolumeHelper) _cachedVolumeHelper = GetComponent<AnimatableVolumeHelper>();
                return _cachedVolumeHelper;
            }
        }
        private AnimatableVolumeHelper _cachedVolumeHelper;

        protected virtual void LateUpdate()
        {
#if UNITY_EDITOR
            // don't auto write in editor mode (because we may want to directly adjust profile)
            if (!Application.isPlaying) return;
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
