using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace TsukimiNeko.AnimatableVolumeComponent
{
    /// <summary>
    /// Base class for AnimatableVolumeComponent. Provides interface for writing/reading to/from VolumeComponent.
    /// </summary>
    /// [ExecuteAlways] is for OnDidApplyAnimationProperties() callback: previewing animation purpose.
    [ExecuteAlways]
    [RequireComponent(typeof(Volume)), RequireComponent(typeof(AnimatableVolumeHelper))]
    public abstract class AnimatableVolumeComponentBase : MonoBehaviour
    {
        public abstract Type TargetType { get; }

        /// <summary> Is this VolumeComponent active? </summary>
        public bool active;

        /// <summary> cached AnimatableVolumeHelper </summary>
        protected AnimatableVolumeHelper volumeHelper
        {
            get {
                if (!_cachedVolumeHelper) _cachedVolumeHelper = GetComponent<AnimatableVolumeHelper>();
                return _cachedVolumeHelper;
            }
        }
        private AnimatableVolumeHelper _cachedVolumeHelper;

        /// <summary>
        /// Write values to VolumeComponent in LateUpdate.
        /// </summary>
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

        /// <summary>
        /// Create runtime profile and write values to VolumeComponent.
        /// </summary>
        private void OnValidate()
        {
            // if no runtime profile, create one
            volumeHelper.CreateRuntimeProfile();
            WriteToVolumeComponent();
            // some Parameter has min/max value, so when keying / modifying in editor,
            // we want to also clamp to Parameter's range
            ReadFromVolumeComponent();
        }

        /// <summary>
        /// Write values to VolumeComponent.
        /// </summary>
        public abstract void WriteToVolumeComponent();
        /// <summary>
        /// Read values from VolumeComponent.
        /// </summary>
        public abstract void ReadFromVolumeComponent();
    }
}
