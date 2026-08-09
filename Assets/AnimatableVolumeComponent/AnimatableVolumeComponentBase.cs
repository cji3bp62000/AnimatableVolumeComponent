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
        /// VolumeComponent that the non-public VolumeParameter cache was resolved from. <br/>
        /// This is not serialized, so it also becomes null after a domain reload.
        /// </summary>
        private VolumeComponent cachedParameterSource;

        /// <summary>
        /// Resolve the non-public VolumeParameter cache, if the source VolumeComponent has changed. <br/>
        /// This must be called at the head of the generated WriteToVolumeComponent(T) / ReadFromVolumeComponent(T),
        /// not in the public wrappers: Reset() passes the VolumeComponent of the <b>shared</b> profile asset, and
        /// caching that one would make later writes modify the project asset itself.
        /// </summary>
        /// <param name="volumeComponent">VolumeComponent that is going to be written / read</param>
        protected void RefreshParameterCache(VolumeComponent volumeComponent)
        {
            if (ReferenceEquals(cachedParameterSource, volumeComponent)) return;

            cachedParameterSource = volumeComponent;
            CacheNonPublicParameters(volumeComponent);
        }

        /// <summary>
        /// Resolve the non-public VolumeParameter cache with the current runtime VolumeComponent.
        /// </summary>
        public void RefreshParameterCache()
        {
            if (!volumeHelper.TryGet(TargetType, out var volumeComponent)) return;

            RefreshParameterCache(volumeComponent);
        }

        /// <summary>
        /// Clear the non-public VolumeParameter cache. The next write / read resolves it again.
        /// </summary>
        public void ClearParameterCache()
        {
            cachedParameterSource = null;
            CacheNonPublicParameters(null);
        }

        /// <summary>
        /// Resolve non-public VolumeParameters by reflection. Overridden by generated code, and only when the
        /// VolumeComponent actually has a non-public VolumeParameter.
        /// </summary>
        /// <param name="volumeComponent">VolumeComponent to resolve from; null clears the cache</param>
        protected virtual void CacheNonPublicParameters(VolumeComponent volumeComponent) { }

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
